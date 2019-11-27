using MathNet.Numerics.Distributions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Utils
{
    public class Simulation
    {
        public static double ConditionedSamplingFrequency = 0.1;

        public static double MinConcurrentSamplingTimeDelta = 0.00001;
        static void FixAndSortSamplingTimes(List<double> samplingTimes, double branchLength)
        {
            samplingTimes.Sort();
            samplingTimes[samplingTimes.Count - 1] = branchLength;

            for (int i = 1; i < samplingTimes.Count; i++)
            {
                if (samplingTimes[i] - samplingTimes[i - 1] < MinConcurrentSamplingTimeDelta)
                {
                    if (i == 1 && samplingTimes.Count > 2)
                    {
                        samplingTimes.RemoveAt(i);
                        i--;
                    }
                    else if (samplingTimes.Count == 2 || i > 1)
                    {
                        samplingTimes.RemoveAt(i - 1);
                        i--;
                    }
                }
            }
        }

        public static (BranchState[][][], int) SimulateJointHistory(CharacterDependency[] dependency, LikelihoodModel tree, int[][] allPossibleStates, string[][] states, double[][] posteriors, double[][,] rateMatrix, int[][] dependencies, Dictionary<string, double>[] conditionedProbabilities, Random randomSource, List<(int dependency, int state)> invariantStates)
        {
            bool retryTree = false;

            int treeAttempts = 0;

            do
            {
                if (retryTree)
                {
                    treeAttempts++;
                    retryTree = false;
                }

                int[][] nodeRealization = new int[tree.Parents.Length][]; //[node][character]

                //If the rate matrices define any invariant states (lines filled with zeroes), make sure that the history is not impossible; otherwise, sample skipping this check
                if (invariantStates.Count > 0)
                {
                    for (int i = 0; i < tree.Children.Length; i++)
                    {
                        if (tree.Children[i].Length == 0)
                        {
                            nodeRealization[i] = allPossibleStates[Categorical.Sample(randomSource, posteriors[i])];
                        }
                        else
                        {
                            bool redraw = true;

                            while (redraw)
                            {
                                redraw = false;
                                nodeRealization[i] = allPossibleStates[Categorical.Sample(randomSource, posteriors[i])];
                                for (int j = 0; j < invariantStates.Count && !redraw; j++)
                                {
                                    if (nodeRealization[i][invariantStates[j].dependency] == invariantStates[j].state)
                                    {
                                        for (int k = 0; k < tree.Children[i].Length; k++)
                                        {
                                            if (nodeRealization[tree.Children[i][k]][invariantStates[j].dependency] != nodeRealization[i][invariantStates[j].dependency])
                                            {
                                                redraw = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < tree.Children.Length; i++)
                    {
                        nodeRealization[i] = allPossibleStates[Categorical.Sample(randomSource, posteriors[i])];
                    }
                }



                BranchState[][][] branches = new BranchState[tree.Parents.Length][][]; //[node][character][]

                for (int i = 0; i < tree.Parents.Length; i++)
                {
                    if (tree.Parents[i] >= 0)
                    {
                        int[] targetState = nodeRealization[i];
                        BranchState[][] jointBranch = new BranchState[dependency.Length][];

                        int[] currState = new int[dependency.Length];

                        bool retry = false;

                        do
                        {
                            retry = false;

                            for (int d = 0; d < dependency.Length; d++)
                            {
                                if (dependency[d].Type == CharacterDependency.Types.Independent)
                                {
                                    List<BranchState> branch;
                                    double remainingLength;

                                    do
                                    {
                                        branch = new List<BranchState>();
                                        remainingLength = tree.BranchLengths[i];
                                        currState[d] = nodeRealization[tree.Parents[i]][d];

                                        while (remainingLength > 0)
                                        {
                                            double currTime = Exponential.Sample(randomSource, -rateMatrix[d][currState[d], currState[d]]);

                                            while (currTime > remainingLength && branch.Count == 0 && currState[d] != targetState[d])
                                            {
                                                currTime = Exponential.Sample(randomSource, -rateMatrix[d][currState[d], currState[d]]);
                                            }

                                            if (currTime > remainingLength)
                                            {
                                                branch.Add(new BranchState(states[d][currState[d]], remainingLength));
                                                remainingLength = 0;
                                            }
                                            else
                                            {
                                                branch.Add(new BranchState(states[d][currState[d]], currTime));
                                                remainingLength -= currTime;
                                                currState[d] = Categorical.Sample(randomSource, (from el in Utils.Range(0, rateMatrix[d].GetLength(0)) select el == currState[d] ? 0 : rateMatrix[d][currState[d], el]).ToArray());
                                            }
                                        }
                                    } while (currState[d] != targetState[d]); //Independent characters can be simulated independently on each other - one character simulation being rejected does not affect other characters

                                    jointBranch[d] = branch.ToArray();
                                }
                            }

                            if (!retry)
                            {
                                for (int d = 0; d < dependency.Length; d++)
                                {
                                    if (dependency[d].Type == CharacterDependency.Types.Conditioned)
                                    {
                                        List<double> samplingTimes = new List<double>();

                                        for (int j = 0; j < dependency[d].Dependencies.Length; j++)
                                        {
                                            double len = 0;
                                            for (int k = 0; k < jointBranch[dependency[d].Dependencies[j]].Length; k++)
                                            {
                                                double time = len + jointBranch[dependency[d].Dependencies[j]][k].Length;
                                                if (!samplingTimes.Contains(time))
                                                {
                                                    samplingTimes.Add(time);
                                                }
                                                len += jointBranch[dependency[d].Dependencies[j]][k].Length;
                                            }
                                        }

                                        FixAndSortSamplingTimes(samplingTimes, Math.Min(samplingTimes.Max(), tree.BranchLengths[i]));

                                        List<BranchState> branch = new List<BranchState>();

                                        branch.Add(new BranchState(states[d][nodeRealization[tree.Parents[i]][d]], samplingTimes[0])); //First state must be the same as the parent

                                        //If the last state is not the target state, let's not bother with sampling intermediate states.
                                        if (samplingTimes.Count > 1)
                                        {
                                            string lastStates = Utils.StringifyArray((from el in dependencies[d] select Utils.GetStateLeft(jointBranch[el], samplingTimes.Last())));
                                            double[] probs = (from el in states[d] select conditionedProbabilities[d][lastStates + ">" + el]).ToArray();

                                            if (Categorical.Sample(randomSource, probs) != targetState[d])
                                            {
                                                retry = true;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (branch[0].State != states[d][targetState[d]])
                                            {
                                                retry = true;
                                                break;
                                            }
                                        }

                                        double currLen = samplingTimes[0];

                                        for (int j = 1; j < samplingTimes.Count - 1; j++)
                                        {
                                            string currStates = Utils.StringifyArray((from el in dependencies[d] select Utils.GetStateLeft(jointBranch[el], samplingTimes[j])));
                                            double[] probs = (from el in states[d] select conditionedProbabilities[d][currStates + ">" + el]).ToArray();

                                            string state = states[d][Categorical.Sample(randomSource, probs)];

                                            if (state != branch.Last().State)
                                            {
                                                branch.Add(new BranchState(state, samplingTimes[j] - currLen));
                                            }
                                            else
                                            {
                                                branch[branch.Count - 1] = new BranchState(state, branch[branch.Count - 1].Length + samplingTimes[j] - currLen);
                                            }

                                            currLen = samplingTimes[j];
                                        }

                                        //We have already rejected with an appropriate probability, so the last state must be the same as the target state
                                        if (states[d][targetState[d]] != branch.Last().State)
                                        {
                                            branch.Add(new BranchState(states[d][nodeRealization[i][d]], tree.BranchLengths[i] - currLen));
                                        }
                                        else
                                        {
                                            branch[branch.Count - 1] = new BranchState(states[d][nodeRealization[i][d]], branch[branch.Count - 1].Length + tree.BranchLengths[i] - currLen);
                                        }

                                        jointBranch[d] = branch.ToArray();

                                        currState[d] = states[d].IndexOf(branch.Last().State);
                                    }
                                }
                            }

                        } while (retry || !currState.SequenceEqual(targetState));

                        if (retryTree)
                        {
                            break;
                        }

                        branches[i] = jointBranch;
                    }
                }

                if (!retryTree)
                {
                    return (branches, treeAttempts);
                }
            } while (retryTree);

            throw new Exception("Unexpected code path!");
        }

        public static BranchState[][][] SimulateJointPriorHistory(CharacterDependency[] dependency, LikelihoodModel tree, string[][] states, double[][,] rateMatrix, int[][] dependencies, Dictionary<string, double>[] conditionedProbabilities, Dictionary<string, double>[] pis, Random randomSource)
        {
            BranchState[][][] branches = new BranchState[tree.Parents.Length][][]; //[node][character][]

            for (int i = tree.Parents.Length - 1; i >= 0; i--)
            {
                if (tree.Parents[i] >= 0)
                {
                    BranchState[][] jointBranch = new BranchState[dependency.Length][];

                    int[] currState = new int[dependency.Length];

                    for (int d = 0; d < dependency.Length; d++)
                    {
                        if (dependency[d].Type == CharacterDependency.Types.Independent)
                        {
                            List<BranchState> branch;
                            double remainingLength;
                            branch = new List<BranchState>();
                            remainingLength = tree.BranchLengths[i];
                            currState[d] = states[d].IndexOf(branches[tree.Parents[i]][d].Last().State);

                            while (remainingLength > 0)
                            {
                                double currTime = Exponential.Sample(randomSource, -rateMatrix[d][currState[d], currState[d]]);

                                if (currTime > remainingLength)
                                {
                                    branch.Add(new BranchState(states[d][currState[d]], remainingLength));
                                    remainingLength = 0;
                                }
                                else
                                {
                                    branch.Add(new BranchState(states[d][currState[d]], currTime));
                                    remainingLength -= currTime;
                                    currState[d] = Categorical.Sample(randomSource, (from el in Utils.Range(0, rateMatrix[d].GetLength(0)) select el == currState[d] ? 0 : rateMatrix[d][currState[d], el]).ToArray());
                                }
                            }
                            jointBranch[d] = branch.ToArray();
                        }
                    }

                    for (int d = 0; d < dependency.Length; d++)
                    {
                        if (dependency[d].Type == CharacterDependency.Types.Conditioned)
                        {
                            List<double> samplingTimes = new List<double>();

                            for (int j = 0; j < dependency[d].Dependencies.Length; j++)
                            {
                                double len = 0;
                                for (int k = 0; k < jointBranch[dependency[d].Dependencies[j]].Length; k++)
                                {
                                    double time = len + jointBranch[dependency[d].Dependencies[j]][k].Length;
                                    if (!samplingTimes.Contains(time))
                                    {
                                        samplingTimes.Add(time);
                                    }
                                    len += jointBranch[dependency[d].Dependencies[j]][k].Length;
                                }
                            }

                            FixAndSortSamplingTimes(samplingTimes, Math.Min(samplingTimes.Max(), tree.BranchLengths[i]));

                            List<BranchState> branch = new List<BranchState>();

                            branch.Add(new BranchState(branches[tree.Parents[i]][d].Last().State, samplingTimes[0])); //First state must be the same as the parent

                            double currLen = samplingTimes[0];

                            for (int j = 1; j < samplingTimes.Count; j++)
                            {
                                string currStates = Utils.StringifyArray((from el in dependencies[d] select Utils.GetStateLeft(jointBranch[el], samplingTimes[j])));
                                double[] probs = (from el in states[d] select conditionedProbabilities[d][currStates + ">" + el]).ToArray();

                                string state = states[d][Categorical.Sample(randomSource, probs)];

                                if (state != branch.Last().State)
                                {
                                    branch.Add(new BranchState(state, samplingTimes[j] - currLen));
                                }
                                else
                                {
                                    branch[branch.Count - 1] = new BranchState(state, branch[branch.Count - 1].Length + samplingTimes[j] - currLen);
                                }

                                currLen = samplingTimes[j];
                            }

                            jointBranch[d] = branch.ToArray();

                            currState[d] = states[d].IndexOf(branch.Last().State);
                        }
                    }
                    branches[i] = jointBranch;
                }
                else
                {
                    BranchState[][] jointBranch = new BranchState[dependency.Length][];

                    int[] currState = new int[dependency.Length];


                    for (int d = 0; d < dependency.Length; d++)
                    {
                        if (dependency[d].Type == CharacterDependency.Types.Independent)
                        {
                            List<BranchState> branch = new List<BranchState>();
                            currState[d] = Categorical.Sample(randomSource, (from el in states[d] select pis[d][el]).ToArray());

                            branch.Add(new BranchState(states[d][currState[d]], 0));

                            jointBranch[d] = branch.ToArray();
                        }
                    }

                    for (int d = 0; d < dependency.Length; d++)
                    {
                        if (dependency[d].Type == CharacterDependency.Types.Conditioned)
                        {
                            List<BranchState> branch = new List<BranchState>();

                            string currStates = Utils.StringifyArray((from el in dependencies[d] select Utils.GetState(jointBranch[el], 0)));
                            double[] probs = (from el in states[d] select conditionedProbabilities[d][currStates + ">" + el]).ToArray();
                            string state = states[d][Categorical.Sample(randomSource, probs)];
                            branch.Add(new BranchState(state, 0));

                            jointBranch[d] = branch.ToArray();

                            currState[d] = states[d].IndexOf(branch.Last().State);
                        }
                    }
                    branches[i] = jointBranch;
                }
            }
            return branches;
        }


        static double getRate(int[][] dependencies, CharacterDependency[] dependency, int d, double branchLength, double remainingLength, string[][] states, double[][,] rateMatrix, BranchState[][] jointBranch, int[] nodeRealizationI)
        {
            double rate = 0;
            int dividers = 0;
            for (int k = 0; k < dependencies[d].Length; k++)
            {
                if (dependency[dependencies[d][k]].Type == CharacterDependency.Types.Independent)
                {
                    if (branchLength - remainingLength > 0)
                    {
                        int state = states[dependencies[d][k]].IndexOf(Utils.GetStateLeft(jointBranch[dependencies[d][k]], branchLength - remainingLength));
                        rate -= rateMatrix[dependencies[d][k]][state, state];
                        dividers++;
                    }
                    else
                    {
                        rate -= rateMatrix[dependencies[d][k]][nodeRealizationI[dependencies[d][k]], nodeRealizationI[dependencies[d][k]]];
                        dividers++;
                    }
                }
                else
                {
                    rate += getRate(dependencies, dependency, dependencies[d][k], branchLength, remainingLength, states, rateMatrix, jointBranch, nodeRealizationI);
                    dividers++;
                }
            }

            return rate / dividers;

        }

        public static TaggedHistory[] SimulateJointHistories(LikelihoodModel[] likModels, int[] treeSamples, CharacterDependency[] dependency, int dependencyIndex, string[][] states, double[][][] posteriors, double[][][] parameters, Dictionary<string, Parameter>[] rates, Dictionary<string, Parameter>[] pi, Random randomSource, int count, int maxThreads, double scale = 1, double delta = 0)
        {
            ConcurrentBag<(int, BranchState[][][])> characterHistories = new ConcurrentBag<(int, BranchState[][][])>();

            string[][] allPossibleStates = Utils.GetAllPossibleStatesStrings(dependency, states);

            List<Parameter> ratesToEstimate = Utils.RatesToEstimateList(Utils.GetParametersToEstimate(dependency, rates, pi));
            List<Parameter> pisToEstimate = Utils.PisToEstimateList(Utils.GetParametersToEstimate(dependency, rates, pi));

            Dictionary<string, int>[] parameterKeys = new Dictionary<string, int>[dependency.Length];
            int[][][] correspStates = new int[dependency.Length][][];

            for (int i = 0; i < dependency.Length; i++)
            {
                if (dependency[i].Type == CharacterDependency.Types.Independent)
                {
                    parameterKeys[i] = new Dictionary<string, int>();

                    for (int j = 0; j < ratesToEstimate.Count; j++)
                    {
                        string key = rates[dependency[i].Index].GetKey(ratesToEstimate[j]);
                        if (!string.IsNullOrEmpty(key))
                        {
                            parameterKeys[i].Add(key, j);
                        }
                    }

                    correspStates[i] = new int[states[dependency[i].Index].Length][];

                    for (int l = 0; l < states[dependency[i].Index].Length; l++)
                    {
                        List<int> currStates = new List<int>();
                        for (int m = 0; m < allPossibleStates.Length; m++)
                        {
                            if (allPossibleStates[m][i] == states[dependency[i].Index][l])
                            {
                                currStates.Add(m);
                            }
                        }

                        correspStates[i][l] = currStates.ToArray();
                    }
                }
                else
                {
                    parameterKeys[i] = new Dictionary<string, int>();

                    for (int j = 0; j < pisToEstimate.Count; j++)
                    {
                        string key = dependency[i].ConditionedProbabilities.GetKey(pisToEstimate[j]);
                        if (!string.IsNullOrEmpty(key))
                        {
                            parameterKeys[i].Add(key, ratesToEstimate.Count + j);
                        }
                    }

                    correspStates[i] = new int[states[dependency[i].Index].Length][];

                    for (int l = 0; l < states[dependency[i].Index].Length; l++)
                    {
                        List<int> currStates = new List<int>();
                        for (int m = 0; m < allPossibleStates.Length; m++)
                        {
                            if (allPossibleStates[m][i] == states[dependency[i].Index][l])
                            {
                                currStates.Add(m);
                            }
                        }

                        correspStates[i][l] = currStates.ToArray();
                    }
                }
            }

            string[][] currDepStates = new string[dependency.Length][];

            for (int i = 0; i < dependency.Length; i++)
            {
                currDepStates[i] = states[dependency[i].Index];
            }


            int cursorPos = ConsoleWrapper.CursorLeft;


            EventWaitHandle notifyProgressThread = new EventWaitHandle(false, EventResetMode.ManualReset);
            EventWaitHandle abortProgressThread = new EventWaitHandle(false, EventResetMode.ManualReset);

            object progressLock = new object();

            int progress = 0;

            Thread progressThread = new Thread(() =>
            {
                while (!abortProgressThread.WaitOne(0))
                {
                    int ind = EventWaitHandle.WaitAny(new WaitHandle[] { notifyProgressThread, abortProgressThread });
                    if (ind == 0)
                    {
                        lock (progressLock)
                        {
                            notifyProgressThread.Reset();
                            progress++;
                        }

                        if (progress % Math.Max(1, (count / 100)) == 0)
                        {
                            ConsoleWrapper.CursorLeft = cursorPos;
                            ConsoleWrapper.Write("{0}   ", ((double)progress / count * scale + delta).ToString("0%", System.Globalization.CultureInfo.InvariantCulture));
                            ConsoleWrapper.CursorLeft = ConsoleWrapper.CursorLeft - 3;

                            if (Utils.RunningGui)
                            {
                                Utils.Trigger("SimulationsProgress", new object[] { (double)progress / count * scale + delta });
                            }
                        }
                    }
                }
            });

            progressThread.Start();

            int[][] allPossibleStatesInt = Utils.GetAllPossibleStatesInts(dependency, states);

            int totalRejectedCount = 0;

            Parallel.For(0, count, new ParallelOptions() { MaxDegreeOfParallelism = maxThreads }, j =>
            {
                double[][][] marginalPosterior = new double[dependency.Length][][];

                double[][,] rateMatrices = new double[dependency.Length][,];

                int[][] dependencies = new int[dependency.Length][];

                Dictionary<string, double>[] conditionedProbabilities = new Dictionary<string, double>[dependency.Length];

                List<(int dependency, int state)> invariantStates = new List<(int, int)>();

                for (int i = 0; i < dependency.Length; i++)
                {
                    marginalPosterior[i] = new double[posteriors[j].Length][];

                    for (int n = 0; n < posteriors[j].Length; n++)
                    {
                        marginalPosterior[i][n] = new double[states[dependency[i].Index].Length];

                        for (int l = 0; l < states[dependency[i].Index].Length; l++)
                        {
                            for (int m = 0; m < correspStates[i][l].Length; m++)
                            {
                                marginalPosterior[i][n][l] += posteriors[j][n][correspStates[i][l][m]];
                            }
                        }
                    }

                    if (dependency[i].Type == CharacterDependency.Types.Independent)
                    {
                        rateMatrices[i] = GetRateMatrix(states[dependency[i].Index], parameters[j][dependencyIndex], parameterKeys[i], rates[dependency[i].Index]);
                        for (int k = 0; k < states[dependency[i].Index].Length; k++)
                        {
                            if (rateMatrices[i][k, k] == 0)
                            {
                                invariantStates.Add((i, k));
                            }
                        }
                    }
                    else if (dependency[i].Type == CharacterDependency.Types.Conditioned)
                    {
                        Dictionary<string, double> template = new Dictionary<string, double>();
                        dependencies[i] = (from el in dependency[i].Dependencies select dependency.FindDependencyWithIndex(el)).ToArray();
                        conditionedProbabilities[i] = GetConditionedProbabilities(parameterKeys[i], parameters[j][dependencyIndex], dependency[i].ConditionedProbabilities, template);
                    }
                }

                (BranchState[][][], int) simulatedHistory = SimulateJointHistory(dependency, likModels[treeSamples[j]], allPossibleStatesInt, currDepStates, posteriors[j], rateMatrices, dependencies, conditionedProbabilities, randomSource, invariantStates);

                characterHistories.Add((j, simulatedHistory.Item1));

                totalRejectedCount += simulatedHistory.Item2;

                lock (progressLock)
                {
                    notifyProgressThread.Set();
                }
            });


            abortProgressThread.Set();
            progressThread.Join();

            TaggedHistory[] tbr = new TaggedHistory[count];

            foreach ((int, BranchState[][][]) characterHistory in characterHistories)
            {
                tbr[characterHistory.Item1] = new TaggedHistory(characterHistory.Item1, new BranchState[likModels[treeSamples[characterHistory.Item1]].Parents.Length][]);

                for (int j = 0; j < likModels[treeSamples[characterHistory.Item1]].Parents.Length; j++)
                {
                    if (likModels[treeSamples[characterHistory.Item1]].Parents[j] >= 0)
                    {
                        List<double> samplingTimes = new List<double>();

                        for (int k = 0; k < dependency.Length; k++)
                        {
                            double len = 0;

                            for (int l = 0; l < characterHistory.Item2[j][k].Length; l++)
                            {
                                double time = len + characterHistory.Item2[j][k][l].Length;
                                if (!samplingTimes.Contains(time))
                                {
                                    samplingTimes.Add(time);
                                }
                                len += characterHistory.Item2[j][k][l].Length;
                            }
                        }

                        if (samplingTimes.Count > 0)
                        {
                            FixAndSortSamplingTimes(samplingTimes, Math.Min(samplingTimes.Max(), likModels[treeSamples[characterHistory.Item1]].BranchLengths[j]));
                        }

                        List<BranchState> branch = new List<BranchState>();

                        double currLen = 0;

                        for (int l = 0; l < samplingTimes.Count; l++)
                        {
                            string state = Utils.StringifyArray(from el in Utils.Range(0, dependency.Length) select Utils.GetStateLeft(characterHistory.Item2[j][el], samplingTimes[l]));

                            if (branch.Count == 0 || branch.Last().State != state)
                            {
                                branch.Add(new BranchState(state, samplingTimes[l] - currLen));
                            }
                            else
                            {
                                branch[branch.Count - 1] = new BranchState(state, branch[branch.Count - 1].Length + samplingTimes[l] - currLen);
                            }

                            currLen = samplingTimes[l];
                        }

                        tbr[characterHistory.Item1].History[j] = branch.ToArray();
                    }
                }
            }

            ConsoleWrapper.CursorLeft = cursorPos;

            if (scale == 1)
            {
                ConsoleWrapper.WriteLine("Done.  ");
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("Tree draw rejection rate: {0}", ((double)totalRejectedCount / (count + totalRejectedCount)).ToString("0.##%"));
            }

            return tbr;
        }

        public static Dictionary<string, double> GetConditionedProbabilities(Dictionary<string, int> parameterKeys, double[] parameters, Dictionary<string, Parameter> conditionedProbabilities, Dictionary<string, double> template)
        {
            if (template.Count == 0)
            {
                foreach (KeyValuePair<string, Parameter> kvp in conditionedProbabilities)
                {
                    if (parameterKeys.TryGetValue(kvp.Key, out int ind))
                    {
                        template.Add(kvp.Key, parameters[ind]);
                    }
                    else
                    {
                        if (kvp.Value.Action == Parameter.ParameterAction.Equal)
                        {
                            string key = conditionedProbabilities.GetKey(kvp.Value.EqualParameter);
                            if (parameterKeys.TryGetValue(key, out int ind2))
                            {
                                template.Add(kvp.Key, parameters[ind2]);
                            }
                            else
                            {
                                template.Add(kvp.Key, kvp.Value.EqualParameter.Value);
                            }
                        }
                        else
                        {
                            template.Add(kvp.Key, kvp.Value.Value);
                        }
                    }
                }

                return template;
            }
            else
            {
                foreach (KeyValuePair<string, Parameter> kvp in conditionedProbabilities)
                {
                    if (parameterKeys.TryGetValue(kvp.Key, out int ind))
                    {
                        template[kvp.Key] = parameters[ind];
                    }
                    else
                    {
                        if (kvp.Value.Action == Parameter.ParameterAction.Equal)
                        {
                            string key = conditionedProbabilities.GetKey(kvp.Value.EqualParameter);
                            if (parameterKeys.TryGetValue(key, out int ind2))
                            {
                                template[kvp.Key] = parameters[ind2];
                            }
                            else
                            {
                                template[kvp.Key] = kvp.Value.EqualParameter.Value;
                            }
                        }
                        else
                        {
                            template[kvp.Key] = kvp.Value.Value;
                        }
                    }
                }

                return template;
            }
        }

        public static double[,] GetRateMatrix(string[] states, double[] parameters, Dictionary<string, int> parameterKeys, Dictionary<string, Parameter> rates)
        {
            double[,] rateMatrix = new double[states.Length, states.Length];
            for (int i = 0; i < states.Length; i++)
            {
                double currRate = 0;
                for (int j = 0; j < states.Length; j++)
                {
                    if (i != j)
                    {
                        string trans = states[i] + ">" + states[j];
                        if (parameterKeys.TryGetValue(trans, out int keyInd))
                        {
                            rateMatrix[i, j] = parameters[keyInd];
                        }
                        else
                        {
                            if (rates[trans].Action == Parameter.ParameterAction.Equal)
                            {
                                string key = rates.GetKey(rates[trans].EqualParameter);
                                if (parameterKeys.TryGetValue(key, out int keyInd2))
                                {
                                    rateMatrix[i, j] = parameters[keyInd2];
                                }
                                else
                                {
                                    rateMatrix[i, j] = rates[trans].EqualParameter.Value;
                                }
                            }
                            else
                            {
                                rateMatrix[i, j] = rates[trans].Value;
                            }
                        }

                        currRate += rateMatrix[i, j];
                    }
                }
                rateMatrix[i, i] = -currRate;
            }

            return rateMatrix;
        }

        public static Dictionary<string, double> GetPis(string[] states, double[] parameters, Dictionary<string, int> parameterKeys, Dictionary<string, Parameter> pi)
        {
            Dictionary<string, double> pis = new Dictionary<string, double>();
            for (int i = 0; i < states.Length; i++)
            {
                if (parameterKeys.TryGetValue(states[i], out int keyInd))
                {
                    pis[states[i]] = parameters[keyInd];
                }
                else
                {
                    if (pi[states[i]].Action == Parameter.ParameterAction.Equal)
                    {
                        string key = pi.GetKey(pi[states[i]].EqualParameter);
                        if (parameterKeys.TryGetValue(key, out int keyInd2))
                        {
                            pis[states[i]] = parameters[keyInd2];
                        }
                        else
                        {
                            pis[states[i]] = pi[states[i]].EqualParameter.Value;
                        }
                    }
                    else
                    {
                        pis[states[i]] = pi[states[i]].Value;
                    }
                }
            }

            return pis;
        }

        public static BranchState[][] GetMarginalHistory(BranchState[][] history, int[] invCorrespStates, string[] marginalStates, Dictionary<string, int> states)
        {
            BranchState[][] tbr = new BranchState[history.Length][];

            for (int i = 0; i < history.Length; i++)
            {
                if (history[i] != null)
                {
                    List<BranchState> branch = new List<BranchState>();
                    for (int j = 0; j < history[i].Length; j++)
                    {
                        if (branch.Count == 0 || branch.Last().State != marginalStates[invCorrespStates[states[history[i][j].State]]])
                        {
                            branch.Add(new BranchState(marginalStates[invCorrespStates[states[history[i][j].State]]], history[i][j].Length));
                        }
                        else
                        {
                            branch[branch.Count - 1] = new BranchState(branch.Last().State, branch.Last().Length + history[i][j].Length);
                        }
                    }

                    tbr[i] = branch.ToArray();
                }
            }

            return tbr;
        }

        public static TaggedHistory[] GetMarginalHistory(SerializedRun Run, int activeChar)
        {
            List<string[]> activeStates = new List<string[]>();
            List<int> activeChars = new List<int>();

            List<string> st = new List<string>();

            for (int j = 0; j < Run.States.Length; j++)
            {
                if (!st.Contains(Run.States[j].Split(',')[activeChar]))
                {
                    st.Add(Run.States[j].Split(',')[activeChar]);
                }
            }

            activeStates.Add(st.ToArray());

            activeChars.Add(activeChar);


            string[][] stateCombinations = Utils.GetCombinations(activeStates.ToArray());

            int[] invCorrespStates = new int[Run.States.Length];

            Dictionary<string, int> statesInds = new Dictionary<string, int>();

            for (int i = 0; i < Run.States.Length; i++)
            {
                statesInds.Add(Run.States[i], i);

                string[] splitState = Run.States[i].Split(',');

                for (int j = 0; j < stateCombinations.Length; j++)
                {
                    bool foundDiff = false;
                    for (int k = 0; k < stateCombinations[j].Length; k++)
                    {
                        if (stateCombinations[j][k] != splitState[activeChars[k]])
                        {
                            foundDiff = true;
                            break;
                        }
                    }
                    if (!foundDiff)
                    {
                        invCorrespStates[i] = j;
                        break;
                    }
                }
            }

            TaggedHistory[] marginalHistories = new TaggedHistory[Run.Histories.Length];

            for (int l = 0; l < Run.Histories.Length; l++)
            {
                marginalHistories[l] = new TaggedHistory(Run.Histories[l].Tag, GetMarginalHistory(Run.Histories[l].History, invCorrespStates, (from el in stateCombinations select Utils.StringifyArray(el)).ToArray(), statesInds));
            }

            return marginalHistories;
        }

        public static TaggedHistory[] GetMarginalPriorHistory(SerializedRun Run, int activeChar, int simulationInd)
        {
            List<string[]> activeStates = new List<string[]>();
            List<int> activeChars = new List<int>();

            List<string> st = new List<string>();

            for (int j = 0; j < Run.States.Length; j++)
            {
                if (!st.Contains(Run.States[j].Split(',')[activeChar]))
                {
                    st.Add(Run.States[j].Split(',')[activeChar]);
                }
            }

            activeStates.Add(st.ToArray());

            activeChars.Add(activeChar);


            string[][] stateCombinations = Utils.GetCombinations(activeStates.ToArray());

            int[] invCorrespStates = new int[Run.States.Length];

            Dictionary<string, int> statesInds = new Dictionary<string, int>();

            for (int i = 0; i < Run.States.Length; i++)
            {
                statesInds.Add(Run.States[i], i);

                string[] splitState = Run.States[i].Split(',');

                for (int j = 0; j < stateCombinations.Length; j++)
                {
                    bool foundDiff = false;
                    for (int k = 0; k < stateCombinations[j].Length; k++)
                    {
                        if (stateCombinations[j][k] != splitState[activeChars[k]])
                        {
                            foundDiff = true;
                            break;
                        }
                    }
                    if (!foundDiff)
                    {
                        invCorrespStates[i] = j;
                        break;
                    }
                }
            }

            TaggedHistory[] marginalHistories = new TaggedHistory[Run.PriorMultiplicity];
            TaggedHistory[] currHistories = Run.GetPriorHistories(simulationInd);

            for (int l = 0; l < currHistories.Length; l++)
            {
                marginalHistories[l] = new TaggedHistory(currHistories[l].Tag, GetMarginalHistory(currHistories[l].History, invCorrespStates, (from el in stateCombinations select Utils.StringifyArray(el)).ToArray(), statesInds));
            }
            return marginalHistories;
        }


        public static TaggedHistory MergeHistories(TaggedHistory[] histories, LikelihoodModel likModel)
        {
            BranchState[][] resultHistory = new BranchState[histories[0].History.Length][];

            for (int i = 0; i < histories[0].History.Length; i++)
            {
                if (histories[0].History[i] != null)
                {
                    List<double> samplingTimes = new List<double>();

                    for (int k = 0; k < histories.Length; k++)
                    {
                        double lastTime = 0;

                        for (int j = 0; j < histories[k].History[i].Length; j++)
                        {
                            lastTime += histories[k].History[i][j].Length;

                            if (!samplingTimes.Contains(lastTime))
                            {
                                samplingTimes.Add(lastTime);
                            }
                        }
                    }

                    FixAndSortSamplingTimes(samplingTimes, likModel.BranchLengths[i]);

                    List<BranchState> states = new List<BranchState>();

                    for (int j = 0; j < samplingTimes.Count; j++)
                    {
                        string[] state = new string[histories.Length];
                        for (int k = 0; k < histories.Length; k++)
                        {
                            state[k] = Utils.GetStateLeft(histories[k].History[i], samplingTimes[j]);
                        }

                        states.Add(new BranchState(Utils.StringifyArray(state), j > 0 ? (samplingTimes[j] - samplingTimes[j - 1]) : samplingTimes[j]));
                    }

                    resultHistory[i] = states.ToArray();
                }
            }

            return new TaggedHistory(-1, resultHistory);
        }
    }
}