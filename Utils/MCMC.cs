using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearRegression;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Utils
{
    public class MCMC
    {
        public abstract class MCMCVariable
        {
            public double StepSize { get; set; }
            public enum VariableType { Uni, Multi }
            public abstract VariableType Type { get; }
            public abstract int Length { get; }
        }

        public class MCMCUniVariable : MCMCVariable
        {
            public enum ProposalTypes
            {
                NormalSlide
            }

            public override VariableType Type { get { return VariableType.Uni; } }

            public override int Length
            {
                get
                {
                    return 1;
                }
            }

            public ProposalTypes ProposalType { get; set; }
            public IContinuousDistribution PriorDistribution { get; set; }
            public List<double> Samples { get; }

            public MCMCUniVariable()
            {
                Samples = new List<double>();
            }
        }

        public class MCMCMultiVariable : MCMCVariable
        {
            public enum ProposalTypes
            {
                Dirichlet, Multinomial
            }

            public override VariableType Type { get { return VariableType.Multi; } }
            public override int Length
            {
                get
                {
                    return PriorDistribution.Dimension;
                }
            }

            public double Scale { get; set; }
            public ProposalTypes ProposalType { get; set; }
            public MultivariateDistribution PriorDistribution { get; set; }
            public List<double[]> Samples { get; }

            public MCMCMultiVariable()
            {
                Samples = new List<double[]>();
            }
        }

        public enum WatchdogActions
        {
            Nothing,
            Converge,
            Restart
        }

        public static double magicAcceptanceRate = 0.37;

        public static bool estimateStepSize = true;

        public static int initialBurnin = 1000;
        public static int tuningSteps = 1000;
        public static int tuningAttempts = 10;

        public static int diagnosticFrequency = 1000;
        public static int sampleFrequency = 10;
        public static int swapFrequency = 10;
        public static int minSamples = 1000;
        public static int maxSamples = 200000;
        public static WatchdogActions watchdogAction = WatchdogActions.Restart;
        public static int watchdogInitialTimeout = 20000;

        public static double temperatureIncrement = 0.5;
        public static double globalStepSizeMultiplier = 1;
        public static double convergenceCoVThreshold = 0.01;
        public static double convergenceESSThreshold = 200;
        public static double convergenceRHatThreshold = 1.01;

        public static double globalTreeSwapProbability = 0.37;

        public static ConcurrentDictionary<string, ConsoleCancelEventHandler> cancelEventHandlers = new ConcurrentDictionary<string, ConsoleCancelEventHandler>();

        public static ((int, double[])[], List<List<Parameter>>[], List<(double remainingPi, List<Parameter> pis)>[], (int, double[][])[], CharacterDependency[][][], Dictionary<string, Parameter>[][], Dictionary<string, Parameter>[][], double[], double) SamplePosterior(CharacterDependency[][] dependencies, LikelihoodModel[] likModels, LikelihoodModel meanLikModel, Dictionary<string, Parameter>[] rates, Dictionary<string, Parameter>[] pi, int dependencyIndex, Func<double[][], int, List<List<Parameter>>[], List<(double remainingPi, List<Parameter> pis)>[], Dictionary<string, Parameter>[][], Dictionary<string, Parameter>[][], CharacterDependency[][][], LikelihoodModel, double> logLikelihoodFunction, Random randomSource, string parameterNameFile, string logFile, int numRuns, int numChains, int sampleCount, bool runUnderPrior, double[] currentStepSizeMultipliers, out bool mcmcSucceeded)
        {
            CharacterDependency[][][] dependenciesByChain = new CharacterDependency[numChains * numRuns][][];
            Dictionary<string, Parameter>[][] pisByChain = new Dictionary<string, Parameter>[numChains * numRuns][];
            Dictionary<string, Parameter>[][] ratesByChain = new Dictionary<string, Parameter>[numChains * numRuns][];
            List<MCMCVariable>[] variablesByChain = new List<MCMCVariable>[numChains * numRuns];
            List<List<Parameter>>[] ratesToEstimateByChain = new List<List<Parameter>>[numChains * numRuns];
            List<(double remainingPi, List<Parameter> pis)>[] pisToEstimateByChain = new List<(double, List<Parameter>)>[numChains * numRuns];

            for (int chainId = 0; chainId < numChains * numRuns; chainId++)
            {
                dependenciesByChain[chainId] = new CharacterDependency[1][];

                dependenciesByChain[chainId][0] = new CharacterDependency[dependencies[dependencyIndex].Length];
                for (int k = 0; k < dependencies[dependencyIndex].Length; k++)
                {
                    switch (dependencies[dependencyIndex][k].Type)
                    {
                        case CharacterDependency.Types.Independent:
                            dependenciesByChain[chainId][0][k] = new CharacterDependency(dependencies[dependencyIndex][k].Index);
                            break;
                        case CharacterDependency.Types.Dependent:
                        case CharacterDependency.Types.Conditioned:
                            dependenciesByChain[chainId][0][k] = new CharacterDependency(dependencies[dependencyIndex][k].Index, dependencies[dependencyIndex][k].Type, (int[])dependencies[dependencyIndex][k].Dependencies.Clone(), Parameter.CloneParameterDictionary(dependencies[dependencyIndex][k].ConditionedProbabilities));
                            break;
                    }
                }

                pisByChain[chainId] = new Dictionary<string, Parameter>[pi.Length];

                for (int j = 0; j < pi.Length; j++)
                {
                    pisByChain[chainId][j] = Parameter.CloneParameterDictionary(pi[j]);
                }

                ratesByChain[chainId] = new Dictionary<string, Parameter>[rates.Length];

                for (int j = 0; j < rates.Length; j++)
                {
                    ratesByChain[chainId][j] = Parameter.CloneParameterDictionary(rates[j]);
                }

                variablesByChain[chainId] = new List<MCMC.MCMCVariable>();
                ratesToEstimateByChain[chainId] = new List<List<Parameter>>();
                pisToEstimateByChain[chainId] = new List<(double, List<Parameter>)>();

                for (int j = 0; j < dependenciesByChain[chainId][0].Length; j++)
                {
                    if (dependenciesByChain[chainId][0][j].Type == CharacterDependency.Types.Independent)
                    {
                        int ind = dependenciesByChain[chainId][0][j].Index;

                        List<Parameter> currentRates = new List<Parameter>();

                        foreach (KeyValuePair<string, Parameter> kvp in ratesByChain[chainId][ind])
                        {
                            if (kvp.Value.Action == Parameter.ParameterAction.Bayes)
                            {
                                currentRates.Add(kvp.Value);
                            }
                        }

                        if (currentRates.Count > 0)
                        {
                            ratesToEstimateByChain[chainId].Add(currentRates);
                        }

                        double remainingPi = 1;
                        List<Parameter> currentPis = new List<Parameter>();

                        foreach (KeyValuePair<string, Parameter> kvp in pisByChain[chainId][ind])
                        {
                            switch (kvp.Value.Action)
                            {
                                case Parameter.ParameterAction.Fix:
                                    remainingPi -= kvp.Value.Value;
                                    break;
                                case Parameter.ParameterAction.Equal:
                                    if (kvp.Value.EqualParameter.Action == Parameter.ParameterAction.Fix)
                                    {
                                        kvp.Value.Action = Parameter.ParameterAction.Fix;
                                        kvp.Value.Value = kvp.Value.EqualParameter.Value;
                                        remainingPi -= kvp.Value.Value;
                                    }
                                    else if (kvp.Value.EqualParameter.Action == Parameter.ParameterAction.ML)
                                    {
                                        remainingPi -= kvp.Value.Value;
                                    }
                                    break;
                                case Parameter.ParameterAction.ML:
                                    remainingPi -= kvp.Value.Value;
                                    break;
                                case Parameter.ParameterAction.Dirichlet:
                                    currentPis.Add(kvp.Value);
                                    break;
                            }
                        }

                        if (currentPis.Count > 0)
                        {
                            pisToEstimateByChain[chainId].Add((remainingPi, currentPis));
                        }
                    }
                    else if (dependenciesByChain[chainId][0][j].Type == CharacterDependency.Types.Dependent)
                    {
                        throw new NotImplementedException();
                    }
                    else if (dependenciesByChain[chainId][0][j].Type == CharacterDependency.Types.Conditioned)
                    {
                        Dictionary<string, List<Parameter>> currentPis = new Dictionary<string, List<Parameter>>();

                        int ind = dependenciesByChain[chainId][0][j].Index;

                        Dictionary<string, double> remainingPi = new Dictionary<string, double>();

                        Dictionary<string, bool> dirichletFound = new Dictionary<string, bool>();
                        Dictionary<string, bool> mlFound = new Dictionary<string, bool>();

                        foreach (KeyValuePair<string, Parameter> kvp in dependenciesByChain[chainId][0][j].ConditionedProbabilities)
                        {
                            string stateName = kvp.Key.Substring(0, kvp.Key.IndexOf(">"));
                            if (!remainingPi.ContainsKey(stateName))
                            {
                                remainingPi.Add(stateName, 1);
                                currentPis.Add(stateName, new List<Parameter>());
                                dirichletFound.Add(stateName, false);
                                mlFound.Add(stateName, false);
                            }

                            switch (kvp.Value.Action)
                            {
                                case Parameter.ParameterAction.Fix:
                                    remainingPi[stateName] -= kvp.Value.Value;
                                    break;
                                case Parameter.ParameterAction.Equal:
                                    if (kvp.Value.EqualParameter.Action == Parameter.ParameterAction.Fix)
                                    {
                                        kvp.Value.Action = Parameter.ParameterAction.Fix;
                                        kvp.Value.Value = kvp.Value.EqualParameter.Value;
                                        remainingPi[stateName] -= kvp.Value.Value;
                                    }
                                    else
                                    {
                                        currentPis[stateName].Add(kvp.Value);
                                    }
                                    break;
                                case Parameter.ParameterAction.ML:
                                    mlFound[stateName] = true;
                                    currentPis[stateName].Add(kvp.Value);
                                    break;
                                case Parameter.ParameterAction.Dirichlet:
                                    dirichletFound[stateName] = true;
                                    currentPis[stateName].Add(kvp.Value);
                                    break;
                                case Parameter.ParameterAction.Multinomial:
                                    dirichletFound[stateName] = true;
                                    currentPis[stateName].Add(kvp.Value);
                                    break;
                            }
                        }

                        foreach (KeyValuePair<string, List<Parameter>> kvp in currentPis)
                        {
                            if (kvp.Value.Count > 0)
                            {
                                int[] currEqualCounts = new int[kvp.Value.Count];

                                for (int i = 0; i < currEqualCounts.Length; i++)
                                {
                                    currEqualCounts[i] = 0;
                                }

                                for (int i = 0; i < kvp.Value.Count; i++)
                                {
                                    if (kvp.Value[i].Action == Parameter.ParameterAction.Equal)
                                    {
                                        currEqualCounts[kvp.Value.IndexOf(kvp.Value[i].EqualParameter)]++;
                                    }
                                }

                                pisToEstimateByChain[chainId].Add((remainingPi[kvp.Key], kvp.Value));
                            }
                        }
                    }
                }

                for (int j = 0; j < ratesToEstimateByChain[chainId].Count; j++)
                {
                    for (int k = 0; k < ratesToEstimateByChain[chainId][j].Count; k++)
                    {
                        MCMC.MCMCUniVariable var = new MCMC.MCMCUniVariable
                        {
                            PriorDistribution = ratesToEstimateByChain[chainId][j][k].PriorDistribution,
                            StepSize = 0.4 * globalStepSizeMultiplier,
                            ProposalType = MCMC.MCMCUniVariable.ProposalTypes.NormalSlide
                        };

                        variablesByChain[chainId].Add(var);
                    }
                }

                for (int j = 0; j < pisToEstimateByChain[chainId].Count; j++)
                {
                    if (pisToEstimateByChain[chainId][j].pis.Count > 0)
                    {
                        if (pisToEstimateByChain[chainId][j].pis[0].Action == Parameter.ParameterAction.Dirichlet)
                        {
                            MCMC.MCMCMultiVariable var = new MCMC.MCMCMultiVariable
                            {
                                StepSize = 0.2 * globalStepSizeMultiplier,
                                ProposalType = MCMC.MCMCMultiVariable.ProposalTypes.Dirichlet,
                                PriorDistribution = new MultivariateDistribution(new Dirichlet((from el in pisToEstimateByChain[chainId][j].pis select el.DistributionParameter).ToArray(), randomSource)),
                                Scale = pisToEstimateByChain[chainId][j].remainingPi
                            };

                            variablesByChain[chainId].Add(var);
                        }
                        else if (pisToEstimateByChain[chainId][j].pis[0].Action == Parameter.ParameterAction.Multinomial)
                        {
                            MCMC.MCMCMultiVariable var = new MCMC.MCMCMultiVariable
                            {
                                StepSize = 0.2 * globalStepSizeMultiplier,
                                ProposalType = MCMC.MCMCMultiVariable.ProposalTypes.Multinomial,
                                PriorDistribution = new MultivariateDistribution(new Multinomial((from el in pisToEstimateByChain[chainId][j].pis select el.DistributionParameter).ToArray(), 1, randomSource)),
                                Scale = pisToEstimateByChain[chainId][j].remainingPi
                            };

                            variablesByChain[chainId].Add(var);
                        }
                    }
                }
            }

            MCMCVariable[][] variables = (from el in variablesByChain select el.ToArray()).ToArray();

            List<string> realParameterNames = new List<string>();

            if (!string.IsNullOrEmpty(parameterNameFile))
            {

                using (StreamWriter sw = new StreamWriter(parameterNameFile, true))
                {
                    sw.Write("T\t");
                    realParameterNames.Add("T");

                    for (int i = 0; i < variables[0].Length; i++)
                    {
                        switch (variables[0][i].Type)
                        {
                            case MCMCVariable.VariableType.Uni:
                                sw.Write("U" + i.ToString() + (i < variables[0].Length - 1 ? "\t" : ""));
                                realParameterNames.Add("U" + i.ToString());
                                break;
                            case MCMCVariable.VariableType.Multi:
                                for (int j = 0; j < ((MCMCMultiVariable)variables[0][i]).PriorDistribution.Alpha.Length; j++)
                                {
                                    sw.Write("M" + i.ToString() + "{" + j.ToString() + "}" + ((j < ((MCMCMultiVariable)variables[0][i]).PriorDistribution.Alpha.Length - 1 || i < variables[0].Length - 1) ? "\t" : ""));
                                    realParameterNames.Add("M" + i.ToString() + "{" + j.ToString() + "}");
                                }
                                break;
                        }
                    }

                    sw.WriteLine();
                }
            }

            EventWaitHandle[] diagnosticHandles = new EventWaitHandle[numRuns];
            EventWaitHandle[] proceedHandles = new EventWaitHandle[numRuns];
            EventWaitHandle[] stopHandles = new EventWaitHandle[numRuns];
            EventWaitHandle[] stepSignals = new EventWaitHandle[numRuns];
            EventWaitHandle[] finishedStepSignals = new EventWaitHandle[numRuns];
            Thread[] runThreads = new Thread[numRuns];
            double[] stepProgresses = new double[numRuns];
            object[] stepLocks = new object[numRuns];
            double[][] stepSizeMultipliers = new double[numRuns][];
            double[] stepDurations = new double[numRuns];

            for (int i = 0; i < numRuns; i++)
            {
                if (currentStepSizeMultipliers.Length == variables[0].Length)
                {
                    stepSizeMultipliers[i] = (double[])currentStepSizeMultipliers.Clone();
                }
                else if (currentStepSizeMultipliers.Length == 1)
                {
                    stepSizeMultipliers[i] = (from el in variables[0] select currentStepSizeMultipliers[0]).ToArray();
                }
                else
                {
                    stepSizeMultipliers[i] = (from el in variables[0] select 1.0).ToArray();
                }
            }

            List<(int, double[][])>[] variableSamples = new List<(int, double[][])>[numRuns];

            MCMCChain[] chains = new MCMCChain[numRuns * numChains];
            MCMCRun[] runs = new MCMCRun[numRuns];

            for (int i = 0; i < numRuns; i++)
            {
                diagnosticHandles[i] = new EventWaitHandle(false, EventResetMode.ManualReset);
                proceedHandles[i] = new EventWaitHandle(false, EventResetMode.ManualReset);
                stopHandles[i] = new EventWaitHandle(false, EventResetMode.ManualReset);
                stepSignals[i] = new EventWaitHandle(false, EventResetMode.ManualReset);
                finishedStepSignals[i] = new EventWaitHandle(false, EventResetMode.ManualReset);
                stepLocks[i] = new object();
                runs[i] = new MCMCRun();

                variableSamples[i] = new List<(int, double[][])>();

                int j = i;

                runThreads[i] = new Thread(() =>
                {
                    RunRun(likModels, meanLikModel, j, numChains, chains, variables, (tree, vars, chainId) => logLikelihoodFunction(vars, chainId, ratesToEstimateByChain, pisToEstimateByChain, ratesByChain, pisByChain, dependenciesByChain, tree), randomSource, logFile + ".run" + (j + 1).ToString() + ".log", variableSamples[j], diagnosticHandles[j], proceedHandles[j], stopHandles[j], ref stepProgresses[j], ref stepSizeMultipliers[j], ref stepDurations[j], stepLocks[j], stepSignals[j], finishedStepSignals[j], runs[j], runUnderPrior);
                });

                bool threadStarted = false;

                while (!threadStarted)
                {
                    try
                    {
                        runThreads[i].Start();
                        threadStarted = true;
                    }
                    catch (Exception e)
                    {
                        EventWaitHandle retryThreadHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

                        ConsoleCancelEventHandler retryThreadEvent = (sender, ev) =>
                        {
                            retryThreadHandle.Set();
                            if (ev != null)
                            {
                                ev.Cancel = true;
                            }
                        };

                        string evGuid = Guid.NewGuid().ToString();

                        Console.CancelKeyPress += retryThreadEvent;
                        cancelEventHandlers.TryAdd(evGuid, retryThreadEvent);

                        ConsoleWrapper.WriteLine("Run thread {0} start error: {1}", i, e.Message);
                        ConsoleWrapper.WriteLine("There are {0} currently running OS threads", Process.GetCurrentProcess().Threads.Count);
                        ConsoleWrapper.WriteLine("Press CTRL+C to retry");

                        retryThreadHandle.WaitOne();

                        Console.CancelKeyPress -= retryThreadEvent;
                        cancelEventHandlers.TryRemove(evGuid, out ConsoleCancelEventHandler evIgnored);
                    }
                }
            }

            int diagnosticCount = 0;

            if (Utils.RunningGui)
            {
                Utils.Trigger("BurnInStarted", new object[] { estimateStepSize, realParameterNames });
            }

            if (estimateStepSize)
            {
                ConsoleWrapper.Write("Performing initial burnin and determining step sizes: 0%");
            }
            else
            {
                ConsoleWrapper.Write("Performing initial burnin: 0%");
            }

            while (!EventWaitHandle.WaitAll(finishedStepSignals, 0))
            {
                EventWaitHandle.WaitAny(stepSignals);
                double progress = 0;

                for (int i = 0; i < stepProgresses.Length; i++)
                {
                    lock (stepLocks[i])
                    {
                        progress += stepProgresses[i];
                    }
                }

                for (int i = 0; i < stepSignals.Length; i++)
                {
                    stepSignals[i].Reset();
                }

                progress /= stepProgresses.Length;

                string progressString = progress.ToString("0%", System.Globalization.CultureInfo.InvariantCulture);
                ConsoleWrapper.SetCursorPosition(0, ConsoleWrapper.CursorTop);

                if (estimateStepSize)
                {
                    ConsoleWrapper.Write("Performing initial burnin and determining step sizes: {0}", progressString.Pad(4, Extensions.PadType.Left));
                    ConsoleWrapper.SetCursorPosition(progressString.Length + 53, ConsoleWrapper.CursorTop);
                }
                else
                {
                    ConsoleWrapper.Write("Performing initial burnin: {0}", progressString.Pad(4, Extensions.PadType.Left));
                }

                if (Utils.RunningGui)
                {
                    Utils.Trigger("BurnInProgress", new object[] { progress });
                }
            }

            ConsoleWrapper.SetCursorPosition(0, ConsoleWrapper.CursorTop);

            double[] averages = new double[variables[0].Length];


            if (estimateStepSize)
            {
                ConsoleWrapper.WriteLine("Performing initial burnin and determining step sizes: Done   ");
                ConsoleWrapper.WriteLine("Average estimated step size multipliers: ");

                for (int i = 0; i < variables[0].Length; i++)
                {
                    averages[i] = (from el in stepSizeMultipliers select el[i]).Average() * globalStepSizeMultiplier;
                    ConsoleWrapper.WriteLine("\t{0}", averages[i].ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
            }
            else
            {
                ConsoleWrapper.WriteLine("Performing initial burnin: Done   ");
                ConsoleWrapper.WriteLine("Fixed step size multipliers: ");

                for (int i = 0; i < variables[0].Length; i++)
                {
                    averages[i] = (from el in stepSizeMultipliers select el[i]).Average() * globalStepSizeMultiplier;
                    ConsoleWrapper.WriteLine("\t{0}", averages[i].ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
            }

            long averageStepDuration = (long)Math.Round(stepDurations.Average());

            string timedLikelihood = "";

            if (averageStepDuration < 1000)
            {
                timedLikelihood = averageStepDuration.ToString() + "ms";
            }
            else if (averageStepDuration < 60000)
            {
                timedLikelihood = ((double)averageStepDuration / 1000).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "s";
            }
            else
            {
                timedLikelihood = ((double)averageStepDuration / 60000).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "min";
            }

            ConsoleWrapper.WriteLine();
            ConsoleWrapper.WriteLine("A single MCMC step takes about {0}", timedLikelihood);

            if (Utils.RunningGui)
            {
                Utils.Trigger("BurnInFinished", new object[] { averages });
            }

            ConsoleWrapper.WriteLine();

            bool oldConvergence = MCMC.convergenceCoVThreshold < 1e5;
            bool newConvergence = MCMC.convergenceRHatThreshold < 1e5;

            if (oldConvergence && !newConvergence)
            {
                ConsoleWrapper.WriteLine("   Sample   │ Acceptance │   Swap AR  │  MCoV Mean │   MCoV SD  │   Min ESS  ");
            }
            else if (oldConvergence && newConvergence)
            {
                ConsoleWrapper.WriteLine("   Sample   │ Acceptance │   Swap AR  │  Max  CoV  │  Max Rhat  │   Min ESS  ");
            }
            else
            {
                ConsoleWrapper.WriteLine("   Sample   │ Acceptance │   Swap AR  │  Max Rhat  │  Bulk ESS  │  Tail ESS  ");
            }

            ConsoleWrapper.WriteLine("────────────┼────────────┼────────────┼────────────┼────────────┼────────────");

            bool converged = false;

            bool cancelled = false;

            ConsoleCancelEventHandler ctrlCHandler = (sender, e) =>
            {
                if (!cancelled)
                {
                    for (int i = 0; i < numRuns; i++)
                    {
                        stopHandles[i].Set();
                        proceedHandles[i].Set();
                    }

                    cancelled = true;

                    if (e != null)
                    {
                        e.Cancel = true;
                    }
                }
            };

            string guid = Guid.NewGuid().ToString();

            Console.CancelKeyPress += ctrlCHandler;
            cancelEventHandlers.TryAdd(guid, ctrlCHandler);

            EventWaitHandle watchdogHandle1 = new EventWaitHandle(false, EventResetMode.ManualReset);
            EventWaitHandle watchdogHandle1Proceed = new EventWaitHandle(false, EventResetMode.ManualReset);

            EventWaitHandle watchdogHandle2 = new EventWaitHandle(false, EventResetMode.ManualReset);
            EventWaitHandle watchdogHandle2Proceed = new EventWaitHandle(false, EventResetMode.ManualReset);

            EventWaitHandle watchdogExit = new EventWaitHandle(false, EventResetMode.ManualReset);

            bool localMCMCSucceeded = true;

            Thread watchdogThread = new Thread(() =>
            {
                EventWaitHandle[] handles = new EventWaitHandle[] { watchdogHandle1, watchdogExit };

                long lastDiagnosticInterval = (watchdogInitialTimeout + averageStepDuration * diagnosticFrequency) / 2 - 500;
                long lastDiagnosticFinished = Environment.TickCount64;

                int lastCollectedSamples = -1;

                while (true)
                {
                    int handleResult = EventWaitHandle.WaitAny(handles, (int)(2 * lastDiagnosticInterval + 1000));

                    if (handleResult == 0)
                    {
                        watchdogHandle1.Reset();
                        lastDiagnosticInterval = Environment.TickCount64 - lastDiagnosticFinished;
                        watchdogHandle1Proceed.Set();

                        watchdogHandle2.WaitOne();
                        watchdogHandle2.Reset();

                        lastDiagnosticFinished = Environment.TickCount64;
                        watchdogHandle2Proceed.Set();
                    }
                    else if (handleResult == 1)
                    {
                        ConsoleWrapper.WriteLine("");
                        ConsoleWrapper.WriteLine("Terminating watchdog thread");
                        ConsoleWrapper.WriteLine("");
                        break;
                    }
                    else if (handleResult == EventWaitHandle.WaitTimeout)
                    {
                        if (lastCollectedSamples < 0)
                        {
                            lastCollectedSamples = variableSamples.Select(x => x.Count).Min();
                            Thread.Sleep((int)(2 * lastDiagnosticInterval + 1000));
                        }

                        if (variableSamples.Select(x => x.Count).Min() <= lastCollectedSamples)
                        {
                            lastCollectedSamples = -1;

                            ConsoleWrapper.WriteLine("");
                            ConsoleWrapper.WriteLine("Deadlock detected (" + ((int)(2 * lastDiagnosticInterval + 1000)).ToString() + "ms)!");

                            if (watchdogAction == WatchdogActions.Restart)
                            {
                                ConsoleWrapper.WriteLine("Stopping and restarting all chains...");

                                localMCMCSucceeded = false;

                                foreach (KeyValuePair<string, ConsoleCancelEventHandler> h in MCMC.cancelEventHandlers)
                                {
                                    h.Value.Invoke(h.Value.Target, null);
                                }
                            }
                            else if (watchdogAction == WatchdogActions.Converge)
                            {
                                ConsoleWrapper.WriteLine("Assuming convergence has been reached...");
                                foreach (KeyValuePair<string, ConsoleCancelEventHandler> h in MCMC.cancelEventHandlers)
                                {
                                    h.Value.Invoke(h.Value.Target, null);
                                }
                            }

                            ConsoleWrapper.WriteLine("");
                        }
                        else
                        {
                            lastCollectedSamples = -1;

                            ConsoleWrapper.WriteLine("");
                            ConsoleWrapper.WriteLine("Potential deadlock detected (" + ((int)(2 * lastDiagnosticInterval + 1000)).ToString() + "ms), but sampling continues...");
                            ConsoleWrapper.WriteLine("");
                        }
                    }
                }
            });

            watchdogThread.Start();

            while (!converged && !cancelled)
            {
                EventWaitHandle.WaitAll(diagnosticHandles);

                for (int i = 0; i < numRuns; i++)
                {
                    diagnosticHandles[i].Reset();
                }

                diagnosticCount++;

                EventWaitHandle.SignalAndWait(watchdogHandle1, watchdogHandle1Proceed);

                double maxCoVMean = double.NaN;
                double maxCoVStdDev = double.NaN;
                double minESS = double.NaN;

                double maxRhat = double.NaN;
                double minBulkESS = double.NaN;
                double minTailESS = double.NaN;

                if (oldConvergence)
                {
                    (maxCoVMean, maxCoVStdDev, minESS, _) = Convergence.ComputeConvergenceStats(numRuns, variables, variableSamples, diagnosticCount, false);
                }

                if (newConvergence)
                {
                    (maxRhat, minBulkESS, minTailESS, _) = Convergence.ComputeConvergenceStats(numRuns, variableSamples, false);
                }

                double avgAR = 0;
                double avgSAR = 0;

                for (int i = 0; i < numRuns; i++)
                {
                    avgAR += runs[i].AvgAcceptanceRate;
                    avgSAR += runs[i].SwapAcceptanceRate;
                }

                avgAR /= numRuns;
                avgSAR /= numRuns;

                for (int i = 0; i < numRuns; i++)
                {
                    proceedHandles[i].Set();

                }

                if (oldConvergence && !newConvergence)
                {
                    ConsoleWrapper.WriteLine("{0}│{1}│{2}│{3}│{4}│{5}", (variableSamples[0].Count).ToString().Pad(12, Extensions.PadType.Center), avgAR.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture).Pad(12, Extensions.PadType.Center), avgSAR.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture).Pad(12, Extensions.PadType.Center), maxCoVMean.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture).Pad(12, Extensions.PadType.Center), maxCoVStdDev.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture).Pad(12, Extensions.PadType.Center), (minESS >= 0 ? minESS.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) : "NC").Pad(12, Extensions.PadType.Center));
                }
                else if (newConvergence && oldConvergence)
                {
                    ConsoleWrapper.WriteLine("{0}│{1}│{2}│{3}│{4}│{5}", (variableSamples[0].Count).ToString().Pad(12, Extensions.PadType.Center), avgAR.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture).Pad(12, Extensions.PadType.Center), avgSAR.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture).Pad(12, Extensions.PadType.Center), Math.Max(maxCoVMean, maxCoVStdDev).ToString("0.########", System.Globalization.CultureInfo.InvariantCulture).Pad(12, Extensions.PadType.Center), maxRhat.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture).Pad(12, Extensions.PadType.Center), (Math.Min(minESS, Math.Min(minBulkESS, minTailESS)) >= 0 ? Math.Min(minESS, Math.Min(minBulkESS, minTailESS)).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) : "NC").Pad(12, Extensions.PadType.Center));
                }
                else
                {
                    ConsoleWrapper.WriteLine("{0}│{1}│{2}│{3}│{4}│{5}", (variableSamples[0].Count).ToString().Pad(12, Extensions.PadType.Center), avgAR.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture).Pad(12, Extensions.PadType.Center), avgSAR.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture).Pad(12, Extensions.PadType.Center), maxRhat.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture).Pad(12, Extensions.PadType.Center), (minBulkESS >= 0 ? minBulkESS.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) : "NC").Pad(12, Extensions.PadType.Center), (minTailESS >= 0 ? minTailESS.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) : "NC").Pad(12, Extensions.PadType.Center));
                }

                bool oldConverged = (!oldConvergence || (maxCoVMean < convergenceCoVThreshold && maxCoVStdDev < convergenceCoVThreshold && minESS > convergenceESSThreshold)) && (diagnosticCount - diagnosticCount / 10) * diagnosticFrequency >= minSamples * sampleFrequency;
                bool newConverged = (!newConvergence || (maxRhat < convergenceRHatThreshold && minBulkESS > convergenceESSThreshold && minTailESS > convergenceESSThreshold)) && (diagnosticCount - diagnosticCount / 10) * diagnosticFrequency >= minSamples * sampleFrequency;

                converged = (oldConverged && newConverged) || (maxSamples > 0 && (diagnosticCount - diagnosticCount / 10) * diagnosticFrequency >= maxSamples * sampleFrequency);

                EventWaitHandle.SignalAndWait(watchdogHandle2, watchdogHandle2Proceed);
            }

            for (int i = 0; i < numRuns; i++)
            {
                stopHandles[i].Set();
                proceedHandles[i].Set();
            }

            watchdogExit.Set();

            ConsoleWrapper.WriteLine();
            ConsoleWrapper.WriteLine("Converged!");

            Console.CancelKeyPress -= ctrlCHandler;
            cancelEventHandlers.TryRemove(guid, out ConsoleCancelEventHandler ignored);

            double[] minEffSizes;

            if (oldConvergence && !newConvergence)
            {
                (_, _, _, minEffSizes) = Convergence.ComputeConvergenceStats(numRuns, variables, variableSamples, diagnosticCount, true);
            }
            else
            {
                (_, _, _, minEffSizes) = Convergence.ComputeConvergenceStats(numRuns, variableSamples, true);
            }

            (int, double[][])[] samples = new (int, double[][])[sampleCount];

            double totalESS = 0;
            for (int j = 0; j < numRuns; j++)
            {
                totalESS += minEffSizes[j];
            }

            int[] samplesFromRuns = Utils.RoundInts((from el in minEffSizes select el / totalESS * sampleCount).ToArray());
            int ind2 = 0;

            for (int i = 0; i < numRuns; i++)
            {
                for (int j = 0; j < samplesFromRuns[i]; j++)
                {
                    samples[ind2] = variableSamples[i][randomSource.Next(variableSamples[i].Count / 10, variableSamples[i].Count)];
                    ind2++;
                }
            }

            double averageSamples = (from el in variableSamples select (double)el.Count).Average();

            bool[] finished = new bool[numRuns];

            while (!finished.All(a => a))
            {
                for (int i = 0; i < numRuns; i++)
                {
                    finished[i] = runThreads[i].Join(100);
                    stopHandles[i].Set();
                    proceedHandles[i].Set();
                }
            }

            (int bayesParameterCount, int mlParameterCount, List<List<Parameter>> ratesToEstimate, List<(double remainingPi, List<Parameter> pis, int[] equalCounts)> pisToEstimate) parametersToEstimate = Utils.GetParametersToEstimate(dependencies[dependencyIndex], rates, pi);

            int bayesParameterCount = parametersToEstimate.bayesParameterCount;
            int mlParameterCount = parametersToEstimate.mlParameterCount;
            List<List<Parameter>> ratesToEstimate = parametersToEstimate.ratesToEstimate;
            List<(double remainingPi, List<Parameter> pis, int[] equalCounts)> pisToEstimate = parametersToEstimate.pisToEstimate;


            (int, double[])[] tbr = new (int, double[])[sampleCount];

            for (int sampleId = 0; sampleId < sampleCount; sampleId++)
            {
                tbr[sampleId].Item1 = samples[sampleId].Item1;
                tbr[sampleId].Item2 = new double[bayesParameterCount + mlParameterCount + pisToEstimate.Count];
                int valInd = 0;
                int bayesValInd = 0;

                for (int i = 0; i < ratesToEstimate.Count; i++)
                {
                    for (int j = 0; j < ratesToEstimate[i].Count; j++)
                    {
                        if (ratesToEstimate[i][j].Action == Parameter.ParameterAction.ML)
                        {
                            tbr[sampleId].Item2[valInd] = double.NaN;
                            valInd++;
                        }
                        else if (ratesToEstimate[i][j].Action == Parameter.ParameterAction.Bayes)
                        {
                            tbr[sampleId].Item2[valInd] = samples[sampleId].Item2[bayesValInd][0];
                            valInd++;
                            bayesValInd++;
                        }
                    }
                }

                for (int i = 0; i < pisToEstimate.Count; i++)
                {
                    bool bayesFound = false;
                    for (int j = 0; j < pisToEstimate[i].pis.Count; j++)
                    {
                        if (pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.ML)
                        {
                            tbr[sampleId].Item2[valInd] = double.NaN;
                            valInd++;
                        }
                        else if (pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.Dirichlet || pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.Multinomial)
                        {
                            tbr[sampleId].Item2[valInd] = samples[sampleId].Item2[bayesValInd][j];
                            bayesFound = true;
                            valInd++;
                        }
                    }

                    if (bayesFound)
                    {
                        bayesValInd++;
                    }
                }

            }

            double[] stepSizeMultipliersAverages = new double[variables[0].Length];
            for (int i = 0; i < stepSizeMultipliersAverages.Length; i++)
            {
                stepSizeMultipliersAverages[i] = (from el in stepSizeMultipliers select el[i]).Average();
            }

            mcmcSucceeded = localMCMCSucceeded;

            return (tbr, ratesToEstimateByChain, pisToEstimateByChain, samples, dependenciesByChain, pisByChain, ratesByChain, stepSizeMultipliersAverages, averageSamples);
        }

        private class MCMCChain
        {
            public int ChainId { get; set; }
            public double Temperature { get; set; }
            public int Step { get; set; }
            public double CurrentLogLikelihood { get; set; }
            public double CurrentLogPrior { get; set; }
            public double AcceptanceRate { get; set; }
        }

        private class MCMCRun
        {
            public double AvgAcceptanceRate { get; set; }
            public double SwapAcceptanceRate { get; set; }
        }

        private static void RunRun(LikelihoodModel[] likModels, LikelihoodModel meanLikModel, int runId, int chainCount, MCMCChain[] chains, MCMCVariable[][] variablesByChain, Func<LikelihoodModel, double[][], int, double> logLikelihoodFunction, Random randomSource, string logFile, List<(int, double[][])> coldSamples, EventWaitHandle diagnosticHandle, EventWaitHandle proceedHandle, EventWaitHandle stopHandle, ref double stepProgress, ref double[] stepSizeMultipliers, ref double stepDuration, object stepLock, EventWaitHandle stepSignal, EventWaitHandle finishedStepSignal, MCMCRun run, bool runUnderPrior)
        {
            using (StreamWriter sw = new StreamWriter(logFile))
            {
                double[][] startVariableValues;
                int startTreeIndex;
                double startLogLikelihood;
                double startLogPrior;

                if (estimateStepSize)
                {
                    MCMCVariable[] variables = variablesByChain[runId * chainCount];

                    sw.Write("step".Pad(10, Extensions.PadType.Center) + "\t");
                    sw.Write("logL".Pad(10, Extensions.PadType.Center) + "\t");
                    sw.Write("logPrior".Pad(10, Extensions.PadType.Center) + "\t");
                    sw.Write(("T").Pad(10, Extensions.PadType.Center) + "\t");

                    for (int i = 0; i < variables.Length; i++)
                    {
                        switch (variables[i].Type)
                        {
                            case MCMCVariable.VariableType.Uni:
                                sw.Write(("U" + i.ToString()).Pad(10, Extensions.PadType.Center) + (i < variables.Length - 1 ? "\t" : ""));
                                break;
                            case MCMCVariable.VariableType.Multi:
                                for (int j = 0; j < ((MCMCMultiVariable)variables[i]).PriorDistribution.Alpha.Length; j++)
                                {
                                    sw.Write(("M" + i.ToString() + "{" + j.ToString() + "}").Pad(10, Extensions.PadType.Center) + ((j < ((MCMCMultiVariable)variables[i]).PriorDistribution.Alpha.Length - 1 || i < variables.Length - 1) ? "\t" : ""));
                                }
                                break;
                        }
                    }

                    sw.WriteLine();

                    double[][] currentVariableValues = new double[variables.Length][];

                    double currentLogPrior = 0;

                    double currentLogLikelihood = 0;

                    currentLogPrior = 0;

                    lock (stepLock)
                    {
                        stepProgress = 0;
                    }

                    for (int i = 0; i < variables.Length; i++)
                    {
                        switch (variables[i].Type)
                        {
                            case MCMCVariable.VariableType.Uni:
                                currentVariableValues[i] = new double[] { ((MCMCUniVariable)variables[i]).PriorDistribution.Mean };
                                currentLogPrior += ((MCMCUniVariable)variables[i]).PriorDistribution.DensityLn(currentVariableValues[i][0]);
                                break;
                            case MCMCVariable.VariableType.Multi:
                                currentVariableValues[i] = (from el in ((MCMCMultiVariable)variables[i]).PriorDistribution.Mean select el * ((MCMCMultiVariable)variables[i]).Scale).ToArray();
                                currentLogPrior += ((MCMCMultiVariable)variables[i]).PriorDistribution.DensityLn(currentVariableValues[i]);
                                break;
                        }
                    }

                    int currentTreeIndex = randomSource.Next(likModels.Length);

                    if (!runUnderPrior)
                    {
                        currentLogLikelihood = logLikelihoodFunction(likModels[currentTreeIndex], currentVariableValues, runId * chainCount);
                    }
                    else
                    {
                        currentLogLikelihood = 0;
                    }

                    Stopwatch stopWatch = Stopwatch.StartNew();

                    for (int step = 0; step < initialBurnin; step++)
                    {
                        currentVariableValues = MCMCStep(1, likModels, meanLikModel, ref currentTreeIndex, variables, currentVariableValues, ref currentLogLikelihood, ref currentLogPrior, logLikelihoodFunction, randomSource, runId * chainCount, out bool accepted, runUnderPrior);
                    }

                    stopWatch.Stop();

                    stepDuration = (double)stopWatch.ElapsedMilliseconds / initialBurnin;


                    double[] stepSizes = (from el in variables select el.StepSize).ToArray();
                    double[] originalStepSizes = (double[])stepSizes.Clone();

                    double stepSizeMultiplier = EstimateStepSizeMultiplier(0, (double)(1 + tuningAttempts) / (tuningAttempts * (variables.Length + 2) + 1), stepLock, ref stepProgress, stepSignal, variables, stepSizes, ref currentVariableValues, likModels, meanLikModel, ref currentTreeIndex, ref currentLogLikelihood, ref currentLogPrior, logLikelihoodFunction, randomSource, runId, chainCount, runUnderPrior);

                    for (int j = 0; j < variables.Length; j++)
                    {
                        variables[j].StepSize = stepSizes[j] * stepSizeMultiplier;
                        stepSizes[j] = variables[j].StepSize;
                    }

                    stepSizeMultipliers = EstimateStepSizeVariableMultipliers((double)(1 + tuningAttempts) / (tuningAttempts * (variables.Length + 2) + 1), (double)(1 + tuningAttempts * (variables.Length + 1)) / (tuningAttempts * (variables.Length + 2) + 1), stepLock, ref stepProgress, stepSignal, variables, stepSizes, ref currentVariableValues, likModels, meanLikModel, ref currentTreeIndex, ref currentLogLikelihood, ref currentLogPrior, logLikelihoodFunction, randomSource, runId, chainCount, runUnderPrior);

                    for (int j = 0; j < variables.Length; j++)
                    {
                        variables[j].StepSize = stepSizes[j] * stepSizeMultipliers[j];
                        stepSizes[j] = variables[j].StepSize;
                    }

                    stepSizeMultiplier = EstimateStepSizeMultiplier((double)(1 + tuningAttempts * (variables.Length + 1)) / (tuningAttempts * (variables.Length + 2) + 1), 1, stepLock, ref stepProgress, stepSignal, variables, stepSizes, ref currentVariableValues, likModels, meanLikModel, ref currentTreeIndex, ref currentLogLikelihood, ref currentLogPrior, logLikelihoodFunction, randomSource, runId, chainCount, runUnderPrior);

                    for (int j = 0; j < variables.Length; j++)
                    {
                        variables[j].StepSize = stepSizes[j] * stepSizeMultiplier;
                        stepSizes[j] = variables[j].StepSize;
                    }

                    for (int j = 0; j < variables.Length; j++)
                    {
                        stepSizeMultipliers[j] = variables[j].StepSize / originalStepSizes[j];
                    }

                    lock (stepLock)
                    {
                        stepProgress = 1;
                    }

                    stepSignal.Set();

                    startVariableValues = currentVariableValues;
                    startLogLikelihood = currentLogLikelihood;
                    startLogPrior = currentLogPrior;
                    startTreeIndex = currentTreeIndex;
                }
                else
                {
                    MCMCVariable[] variables = variablesByChain[runId * chainCount];

                    sw.Write("step".Pad(10, Extensions.PadType.Center) + "\t");
                    sw.Write("logL".Pad(10, Extensions.PadType.Center) + "\t");
                    sw.Write("logPrior".Pad(10, Extensions.PadType.Center) + "\t");
                    sw.Write(("T").Pad(10, Extensions.PadType.Center) + "\t");

                    for (int i = 0; i < variables.Length; i++)
                    {
                        switch (variables[i].Type)
                        {
                            case MCMCVariable.VariableType.Uni:
                                sw.Write(("U" + i.ToString()).Pad(10, Extensions.PadType.Center) + (i < variables.Length - 1 ? "\t" : ""));
                                break;
                            case MCMCVariable.VariableType.Multi:
                                for (int j = 0; j < ((MCMCMultiVariable)variables[i]).PriorDistribution.Alpha.Length; j++)
                                {
                                    sw.Write(("M" + i.ToString() + "{" + j.ToString() + "}").Pad(10, Extensions.PadType.Center) + ((j < ((MCMCMultiVariable)variables[i]).PriorDistribution.Alpha.Length - 1 || i < variables.Length - 1) ? "\t" : ""));
                                }
                                break;
                        }
                    }

                    sw.WriteLine();

                    double[][] currentVariableValues = new double[variables.Length][];

                    double currentLogPrior = 0;

                    double currentLogLikelihood;

                    lock (stepLock)
                    {
                        stepProgress = 0;
                    }

                    for (int i = 0; i < variables.Length; i++)
                    {
                        switch (variables[i].Type)
                        {
                            case MCMCVariable.VariableType.Uni:
                                currentVariableValues[i] = new double[] { ((MCMCUniVariable)variables[i]).PriorDistribution.Mean };
                                currentLogPrior += ((MCMCUniVariable)variables[i]).PriorDistribution.DensityLn(currentVariableValues[i][0]);
                                break;
                            case MCMCVariable.VariableType.Multi:
                                currentVariableValues[i] = (from el in ((MCMCMultiVariable)variables[i]).PriorDistribution.Mean select el * ((MCMCMultiVariable)variables[i]).Scale).ToArray();
                                currentLogPrior += ((MCMCMultiVariable)variables[i]).PriorDistribution.DensityLn(currentVariableValues[i]);
                                break;
                        }
                    }

                    int currentTreeIndex = randomSource.Next(likModels.Length);

                    if (!runUnderPrior)
                    {
                        currentLogLikelihood = logLikelihoodFunction(likModels[currentTreeIndex], currentVariableValues, runId * chainCount);
                    }
                    else
                    {
                        currentLogLikelihood = 0;
                    }

                    Stopwatch stopWatch = Stopwatch.StartNew();

                    for (int step = 0; step < initialBurnin; step++)
                    {
                        currentVariableValues = MCMCStep(1, likModels, meanLikModel, ref currentTreeIndex, variables, currentVariableValues, ref currentLogLikelihood, ref currentLogPrior, logLikelihoodFunction, randomSource, runId, out bool accepted, runUnderPrior);
                    }

                    stopWatch.Stop();

                    stepDuration = (double)stopWatch.ElapsedMilliseconds / initialBurnin;

                    for (int j = 0; j < variables.Length; j++)
                    {
                        variables[j].StepSize *= stepSizeMultipliers[j];
                    }

                    startVariableValues = currentVariableValues;
                    startLogLikelihood = currentLogLikelihood;
                    startLogPrior = currentLogPrior;
                    startTreeIndex = currentTreeIndex;
                }

                finishedStepSignal.Set();
                stepSignal.Set();

                if (Utils.RunningGui)
                {
                    Utils.Trigger("MCMCSamplingStarted", new object[] { runId });
                }

                for (int i = 1; i < chainCount; i++)
                {
                    for (int j = 0; j < variablesByChain[runId * chainCount].Length; j++)
                    {
                        variablesByChain[runId * chainCount + i][j].StepSize = variablesByChain[runId * chainCount][j].StepSize;
                    }
                }

                int[] treeIndicesByChain = new int[chainCount];
                for (int i = 0; i < chainCount; i++)
                {
                    treeIndicesByChain[i] = startTreeIndex;
                }

                Thread[] chainThreads = new Thread[chainCount];
                EventWaitHandle[] sampleEvents = new EventWaitHandle[chainCount];
                EventWaitHandle[] sampleEventsIncludingAbort = new EventWaitHandle[chainCount + 1];
                EventWaitHandle[] proceedEvents = new EventWaitHandle[chainCount];

                EventWaitHandle[] waitForDiagnosticEvents = new EventWaitHandle[chainCount];
                EventWaitHandle[] diagnosticEvents = new EventWaitHandle[chainCount];
                EventWaitHandle[] diagnosticEventsIncludingAbort = new EventWaitHandle[chainCount + 1];
                EventWaitHandle[] proceedDiagnosticEvents = new EventWaitHandle[chainCount];

                EventWaitHandle[] waitForSwapEvents = new EventWaitHandle[chainCount];
                EventWaitHandle[] waitingForSwapEvents = new EventWaitHandle[chainCount];
                EventWaitHandle[] proceedAfterSwapEvents = new EventWaitHandle[chainCount];

                EventWaitHandle globalSampleEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
                EventWaitHandle globalDiagnosticEvent = new EventWaitHandle(false, EventResetMode.ManualReset);

                for (int i = 0; i < chainCount; i++)
                {
                    int j = i;
                    chains[runId * chainCount + j] = new MCMCChain();
                    chains[runId * chainCount + j].Temperature = 1 / (1 + temperatureIncrement * j);

                    sampleEvents[j] = new EventWaitHandle(false, EventResetMode.ManualReset);
                    sampleEventsIncludingAbort[j] = sampleEvents[j];
                    proceedEvents[j] = new EventWaitHandle(false, EventResetMode.ManualReset);

                    waitForDiagnosticEvents[j] = new EventWaitHandle(false, EventResetMode.ManualReset);
                    diagnosticEvents[j] = new EventWaitHandle(false, EventResetMode.ManualReset);
                    diagnosticEventsIncludingAbort[j] = diagnosticEvents[j];
                    proceedDiagnosticEvents[j] = new EventWaitHandle(false, EventResetMode.ManualReset);

                    waitForSwapEvents[j] = new EventWaitHandle(false, EventResetMode.ManualReset);
                    waitingForSwapEvents[j] = new EventWaitHandle(false, EventResetMode.ManualReset);
                    proceedAfterSwapEvents[j] = new EventWaitHandle(false, EventResetMode.ManualReset);

                    chainThreads[j] = new Thread(() =>
                    {
                        RunChain(runId * chainCount + j, chains[runId * chainCount + j], likModels, meanLikModel, ref treeIndicesByChain[j], startVariableValues.DeepClone(), startLogLikelihood, startLogPrior, variablesByChain[runId * chainCount + j], logLikelihoodFunction, randomSource, waitForDiagnosticEvents[j], diagnosticEvents[j], proceedDiagnosticEvents[j], stopHandle, sampleEvents[j], proceedEvents[j], waitForSwapEvents[j], waitingForSwapEvents[j], proceedAfterSwapEvents[j], runUnderPrior);
                    });
                }

                EventWaitHandle abortSampleWaiter = new EventWaitHandle(false, EventResetMode.ManualReset);
                sampleEventsIncludingAbort[chainCount] = abortSampleWaiter;

                bool pauseSampling = false;

                Thread sampleWaiter = new Thread(() =>
                {
                    while (!abortSampleWaiter.WaitOne(0))
                    {
                        int ind = EventWaitHandle.WaitAny(sampleEventsIncludingAbort);

                        if (ind < chainCount && !pauseSampling)
                        {
                            sampleEvents[ind].Reset();
                            globalSampleEvent.Set();
                        }
                    }
                });

                bool threadStarted = false;

                while (!threadStarted)
                {
                    try
                    {
                        sampleWaiter.Start();
                        threadStarted = true;
                    }
                    catch (Exception e)
                    {
                        EventWaitHandle retryThreadHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

                        ConsoleCancelEventHandler retryThreadEvent = (sender, ev) =>
                        {
                            retryThreadHandle.Set();
                            if (ev != null)
                            {
                                ev.Cancel = true;
                            }
                        };

                        string guid = Guid.NewGuid().ToString();

                        Console.CancelKeyPress += retryThreadEvent;
                        cancelEventHandlers.TryAdd(guid, retryThreadEvent);

                        ConsoleWrapper.WriteLine("Sampling thread start error: {0}", e.Message);
                        ConsoleWrapper.WriteLine("There are {0} currently running OS threads", Process.GetCurrentProcess().Threads.Count);
                        ConsoleWrapper.WriteLine("Press CTRL+C to retry");

                        retryThreadHandle.WaitOne();

                        Console.CancelKeyPress -= retryThreadEvent;
                        cancelEventHandlers.TryRemove(guid, out ConsoleCancelEventHandler ignored);
                    }
                }

                EventWaitHandle abortDiagnosticWaiter = new EventWaitHandle(false, EventResetMode.ManualReset);
                diagnosticEventsIncludingAbort[chainCount] = abortDiagnosticWaiter;

                Thread diagnosticWaiter = new Thread(() =>
                {
                    while (!abortDiagnosticWaiter.WaitOne(0))
                    {
                        EventWaitHandle.WaitAny(diagnosticEventsIncludingAbort);

                        if (EventWaitHandle.WaitAll(diagnosticEvents, 0))
                        {
                            for (int i = 0; i < chainCount; i++)
                            {
                                diagnosticEvents[i].Reset();
                            }
                            globalDiagnosticEvent.Set();
                        }
                    }
                });


                threadStarted = false;

                while (!threadStarted)
                {
                    try
                    {
                        diagnosticWaiter.Start();
                        threadStarted = true;
                    }
                    catch (Exception e)
                    {
                        EventWaitHandle retryThreadHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

                        ConsoleCancelEventHandler retryThreadEvent = (sender, ev) =>
                        {
                            retryThreadHandle.Set();
                            if (ev != null)
                            {
                                ev.Cancel = true;
                            }
                        };

                        string guid = Guid.NewGuid().ToString();

                        Console.CancelKeyPress += retryThreadEvent;
                        cancelEventHandlers.TryAdd(guid, retryThreadEvent);

                        ConsoleWrapper.WriteLine("Diagnostic thread start error: {0}", e.Message);
                        ConsoleWrapper.WriteLine("There are {0} currently running OS threads", Process.GetCurrentProcess().Threads.Count);
                        ConsoleWrapper.WriteLine("Press CTRL+C to retry");

                        retryThreadHandle.WaitOne();

                        Console.CancelKeyPress -= retryThreadEvent;
                        cancelEventHandlers.TryRemove(guid, out ConsoleCancelEventHandler ignored);
                    }
                }

                for (int i = 0; i < chainCount; i++)
                {
                    threadStarted = false;

                    while (!threadStarted)
                    {
                        try
                        {
                            chainThreads[i].Start();
                            threadStarted = true;
                        }
                        catch (Exception e)
                        {
                            EventWaitHandle retryThreadHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

                            ConsoleCancelEventHandler retryThreadEvent = (sender, ev) =>
                            {
                                retryThreadHandle.Set();
                                if (ev != null)
                                {
                                    ev.Cancel = true;
                                }
                            };

                            string guid = Guid.NewGuid().ToString();

                            Console.CancelKeyPress += retryThreadEvent;
                            cancelEventHandlers.TryAdd(guid, retryThreadEvent);

                            ConsoleWrapper.WriteLine("Chain thread {0} start error: {1}", i, e.Message);
                            ConsoleWrapper.WriteLine("There are {0} currently running OS threads", Process.GetCurrentProcess().Threads.Count);
                            ConsoleWrapper.WriteLine("Press CTRL+C to retry");

                            retryThreadHandle.WaitOne();

                            Console.CancelKeyPress -= retryThreadEvent;
                            cancelEventHandlers.TryRemove(guid, out ConsoleCancelEventHandler ignored);
                        }
                    }
                }

                int acceptedSwaps = 0;
                int refusedSwaps = 0;

                while (true)
                {
                    int eventType = EventWaitHandle.WaitAny(new EventWaitHandle[] { globalSampleEvent, globalDiagnosticEvent, stopHandle });

                    if (eventType == 0)
                    {
                        globalSampleEvent.Reset();

                        double coldLogLikelihood = 0;
                        double coldLogPrior = 0;
                        int coldStep = coldSamples.Count;
                        int coldTreeIndex = -1;
                        double[][] coldSample = new double[0][];

                        for (int i = 0; i < chainCount; i++)
                        {
                            if (chains[runId * chainCount + i].Temperature == 1)
                            {
                                coldLogLikelihood = chains[runId * chainCount + i].CurrentLogLikelihood;
                                coldLogPrior = chains[runId * chainCount + i].CurrentLogPrior;
                                coldSample = new double[variablesByChain[runId * chainCount + i].Length][];
                                coldTreeIndex = treeIndicesByChain[i];

                                for (int j = 0; j < variablesByChain[runId * chainCount + i].Length; j++)
                                {
                                    switch (variablesByChain[runId * chainCount + i][j].Type)
                                    {
                                        case MCMCVariable.VariableType.Uni:
                                            coldSample[j] = new double[] { ((MCMCUniVariable)variablesByChain[runId * chainCount + i][j]).Samples.Last() };
                                            break;
                                        case MCMCVariable.VariableType.Multi:
                                            coldSample[j] = (double[])((MCMCMultiVariable)variablesByChain[runId * chainCount + i][j]).Samples.Last().Clone();
                                            break;
                                    }
                                }

                                if (chainCount > 1 && (coldSamples.Count + 1) % (swapFrequency / sampleFrequency) == 0)
                                {
                                    pauseSampling = true;
                                }

                                proceedEvents[i].Set();
                            }
                        }

                        if (chainCount > 1 && (coldSamples.Count + 1) % (swapFrequency / sampleFrequency) == 0)
                        {
                            int swap1 = randomSource.Next(0, chainCount);
                            int swap2 = randomSource.Next(0, chainCount - 1);

                            if (swap2 >= swap1)
                            {
                                swap2++;
                            }

                            //Stop chains swap1 and swap2

                            waitForSwapEvents[swap1].Set();
                            waitForSwapEvents[swap2].Set();

                            //Wait until they have paused

                            bool waiting1 = false;
                            bool waiting2 = false;

                            bool skipSwap = false;

                            while (!waiting1 || !waiting2)
                            {
                                int waitEvent = EventWaitHandle.WaitAny(new EventWaitHandle[] { waitingForSwapEvents[swap1], waitingForSwapEvents[swap2], sampleEvents[swap1], sampleEvents[swap2], globalDiagnosticEvent, stopHandle });

                                switch (waitEvent)
                                {
                                    case 0:
                                        waiting1 = true;
                                        waitingForSwapEvents[swap1].Reset();
                                        break;
                                    case 1:
                                        waiting2 = true;
                                        waitingForSwapEvents[swap2].Reset();
                                        break;
                                    case 2:
                                        sampleEvents[swap1].Reset();
                                        globalSampleEvent.Reset();
                                        proceedEvents[swap1].Set();
                                        break;
                                    case 3:
                                        sampleEvents[swap2].Reset();
                                        globalSampleEvent.Reset();
                                        proceedEvents[swap2].Set();
                                        break;
                                    case 4:
                                    case 5:
                                        waiting1 = true;
                                        waiting2 = true;
                                        skipSwap = true;
                                        waitingForSwapEvents[swap2].Reset();
                                        waitingForSwapEvents[swap1].Reset();
                                        break;
                                }
                            }

                            //Swap temperatures

                            if (!skipSwap)
                            {
                                double temp1 = chains[runId * chainCount + swap1].Temperature;
                                double temp2 = chains[runId * chainCount + swap2].Temperature;
                                double logPost1 = chains[runId * chainCount + swap1].CurrentLogLikelihood + chains[runId * chainCount + swap1].CurrentLogPrior;
                                double logPost2 = chains[runId * chainCount + swap2].CurrentLogLikelihood + chains[runId * chainCount + swap2].CurrentLogPrior;

                                double acceptanceProbability = Math.Exp(temp1 * (logPost2 - logPost1) + temp2 * (logPost1 - logPost2));

                                if (randomSource.NextDouble() < acceptanceProbability)
                                {
                                    double tempTemp = chains[runId * chainCount + swap1].Temperature;
                                    chains[runId * chainCount + swap1].Temperature = chains[runId * chainCount + swap2].Temperature;
                                    chains[runId * chainCount + swap2].Temperature = tempTemp;
                                    acceptedSwaps++;
                                }
                                else
                                {
                                    refusedSwaps++;
                                }
                            }

                            //Resume chains

                            pauseSampling = false;

                            proceedAfterSwapEvents[swap1].Set();
                            proceedAfterSwapEvents[swap2].Set();
                        }

                        if ((coldSamples.Count + 1) % (diagnosticFrequency / sampleFrequency) == 0)
                        {
                            for (int i = 0; i < chainCount; i++)
                            {
                                waitForDiagnosticEvents[i].Set();
                                if (sampleEvents[i].WaitOne(0))
                                {
                                    sampleEvents[i].Reset();
                                    proceedEvents[i].Set();
                                }
                            }
                        }

                        coldSamples.Add((coldTreeIndex, coldSample));

                        if (Utils.RunningGui)
                        {
                            Utils.Trigger("MCMCSample", new object[] { runId, (coldTreeIndex, coldSample, coldLogLikelihood) });
                        }

                        sw.Write(coldStep.ToString().Pad(10, Extensions.PadType.Center) + "\t");
                        sw.Write(coldLogLikelihood.ToString(System.Globalization.CultureInfo.InvariantCulture).Pad(10, Extensions.PadType.Center) + "\t");
                        sw.Write(coldLogPrior.ToString(System.Globalization.CultureInfo.InvariantCulture).Pad(10, Extensions.PadType.Center) + "\t");
                        sw.Write(coldTreeIndex.ToString().Pad(10, Extensions.PadType.Center) + "\t");

                        for (int j = 0; j < coldSample.Length; j++)
                        {
                            for (int k = 0; k < coldSample[j].Length; k++)
                            {
                                sw.Write(coldSample[j][k].ToString("0.00000000", System.Globalization.CultureInfo.InvariantCulture).Pad(10, Extensions.PadType.Center) + ((j < coldSample.Length - 1 || k < coldSample[j].Length - 1) ? "\t" : ""));
                            }
                        }

                        sw.WriteLine();
                    }
                    else if (eventType == 1)
                    {
                        globalDiagnosticEvent.Reset();

                        run.SwapAcceptanceRate = (double)acceptedSwaps / (acceptedSwaps + refusedSwaps);
                        run.AvgAcceptanceRate = 0;
                        for (int i = 0; i < chainCount; i++)
                        {
                            run.AvgAcceptanceRate += chains[runId * chainCount + i].AcceptanceRate;
                        }
                        run.AvgAcceptanceRate /= chainCount;

                        EventWaitHandle.SignalAndWait(diagnosticHandle, proceedHandle);

                        proceedHandle.Reset();
                        for (int i = 0; i < chainCount; i++)
                        {
                            proceedDiagnosticEvents[i].Set();
                        }
                    }
                    else if (eventType == 2)
                    {
                        for (int i = 0; i < chainCount; i++)
                        {
                            proceedEvents[i].Set();
                            proceedDiagnosticEvents[i].Set();
                        }

                        abortSampleWaiter.Set();

                        abortDiagnosticWaiter.Set();

                        diagnosticHandle.Set();
                        return;
                    }
                }
            }
        }

        private static double EstimateStepSizeMultiplier(double minProgress, double maxProgress, object stepLock, ref double stepProgress, EventWaitHandle stepSignal, MCMCVariable[] variables, double[] stepSizes, ref double[][] currentVariableValues, LikelihoodModel[] likModels, LikelihoodModel meanLikModel, ref int currentTreeIndex, ref double currentLogLikelihood, ref double currentLogPrior, Func<LikelihoodModel, double[][], int, double> logLikelihoodFunction, Random randomSource, int runId, int chainCount, bool runUnderPrior)
        {
            double stepSizeMultiplier = 1;

            Dictionary<double, double> testedValues = new Dictionary<double, double>(tuningAttempts);

            for (int tunAtt = 0; tunAtt < tuningAttempts; tunAtt++)
            {
                lock (stepLock)
                {
                    stepProgress = minProgress + (double)(1 + tunAtt) / (tuningAttempts + 1) * (maxProgress - minProgress);
                }

                stepSignal.Set();

                for (int j = 0; j < variables.Length; j++)
                {
                    variables[j].StepSize = stepSizes[j] * stepSizeMultiplier;
                }

                if (!runUnderPrior)
                {
                    currentLogLikelihood = logLikelihoodFunction(meanLikModel, currentVariableValues, runId * chainCount);
                }
                else
                {
                    currentLogLikelihood = 0;
                }

                int acceptedCount = 0;

                int m1 = -1;

                for (int step = 0; step < tuningSteps; step++)
                {
                    currentVariableValues = MCMCStep(1, likModels, meanLikModel, ref m1, variables, currentVariableValues, ref currentLogLikelihood, ref currentLogPrior, logLikelihoodFunction, randomSource, runId * chainCount, out bool accepted, runUnderPrior);
                    if (accepted)
                    {
                        acceptedCount++;
                    }
                }

                double accValue = (double)acceptedCount / tuningSteps;

                if (!testedValues.ContainsKey(stepSizeMultiplier))
                {
                    testedValues.Add(stepSizeMultiplier, accValue);
                }
                else
                {
                    testedValues[stepSizeMultiplier] = accValue;
                }

                if (accValue > magicAcceptanceRate)
                {
                    IEnumerable<KeyValuePair<double, double>> lowerValues = from el in testedValues where el.Value <= magicAcceptanceRate orderby el.Value descending select el;

                    if (lowerValues.Count() == 0)
                    {
                        stepSizeMultiplier *= Math.E;
                    }
                    else
                    {
                        stepSizeMultiplier = Math.Exp((magicAcceptanceRate - lowerValues.ElementAt(0).Value) / (accValue - lowerValues.ElementAt(0).Value) * (Math.Log(stepSizeMultiplier) - Math.Log(lowerValues.ElementAt(0).Key)) + Math.Log(lowerValues.ElementAt(0).Key));
                    }
                }
                else if (accValue < magicAcceptanceRate)
                {
                    IEnumerable<KeyValuePair<double, double>> higherValues = from el in testedValues where el.Value >= magicAcceptanceRate orderby el.Value ascending select el;

                    if (higherValues.Count() == 0)
                    {
                        stepSizeMultiplier /= Math.E;
                    }
                    else
                    {
                        stepSizeMultiplier = Math.Exp(Math.Log(stepSizeMultiplier) + (magicAcceptanceRate - accValue) / (higherValues.ElementAt(0).Value - accValue) * (Math.Log(higherValues.ElementAt(0).Key) - Math.Log(stepSizeMultiplier)));
                    }
                }
                else
                {
                    break;
                }
            }

            IEnumerable<KeyValuePair<double, double>> lowerBound = from el in testedValues where el.Value <= magicAcceptanceRate orderby el.Value descending select el;
            IEnumerable<KeyValuePair<double, double>> upperBound = from el in testedValues where el.Value >= magicAcceptanceRate orderby el.Value ascending select el;

            if (lowerBound.Count() > 0 && upperBound.Count() > 0)
            {
                if (lowerBound.ElementAt(0).Value == upperBound.ElementAt(0).Value)
                {
                    stepSizeMultiplier = lowerBound.ElementAt(0).Key;
                }
                else
                {
                    stepSizeMultiplier = Math.Exp((magicAcceptanceRate - lowerBound.ElementAt(0).Value) / (upperBound.ElementAt(0).Value - lowerBound.ElementAt(0).Value) * (Math.Log(upperBound.ElementAt(0).Key) - Math.Log(lowerBound.ElementAt(0).Key)) + Math.Log(lowerBound.ElementAt(0).Key));
                }
            }
            else if (lowerBound.Count() > 0)
            {
                stepSizeMultiplier = lowerBound.ElementAt(0).Key * Math.E;
            }
            else if (upperBound.Count() > 0)
            {
                stepSizeMultiplier = upperBound.ElementAt(0).Key / Math.E;
            }
            else
            {
                stepSizeMultiplier = 1;
            }

            return stepSizeMultiplier;
        }

        private static double[] EstimateStepSizeVariableMultipliers(double minProgress, double maxProgress, object stepLock, ref double stepProgress, EventWaitHandle stepSignal, MCMCVariable[] variables, double[] stepSizes, ref double[][] currentVariableValues, LikelihoodModel[] likModels, LikelihoodModel meanLikModel, ref int currentTreeIndex, ref double currentLogLikelihood, ref double currentLogPrior, Func<LikelihoodModel, double[][], int, double> logLikelihoodFunction, Random randomSource, int runId, int chainCount, bool runUnderPrior)
        {
            double[] stepSizeMultipliers = (from el in variables select 1.0).ToArray();

            for (int i = 0; i < variables.Length; i++)
            {
                double stepSizeMultiplier = 1;

                Dictionary<double, double> testedValues = new Dictionary<double, double>(tuningAttempts);

                for (int tunAtt = 0; tunAtt < tuningAttempts; tunAtt++)
                {
                    lock (stepLock)
                    {
                        stepProgress = minProgress + (double)(1 + tunAtt + tuningAttempts * i) / (tuningAttempts * variables.Length + 1) * (maxProgress - minProgress);
                    }

                    stepSignal.Set();

                    int acceptedCount = 0;

                    if (!runUnderPrior)
                    {
                        currentLogLikelihood = logLikelihoodFunction(meanLikModel, currentVariableValues, runId * chainCount);
                    }
                    else
                    {
                        currentLogLikelihood = 0;
                    }

                    for (int step = 0; step < tuningSteps; step++)
                    {
                        for (int j = 0; j < variables.Length; j++)
                        {
                            if (j == i)
                            {
                                variables[j].StepSize = stepSizes[j] * stepSizeMultiplier;
                            }
                            else
                            {
                                variables[j].StepSize = 0;
                            }
                        }

                        int m1 = -1;

                        currentVariableValues = MCMCStep(1, likModels, meanLikModel, ref m1, variables, currentVariableValues, ref currentLogLikelihood, ref currentLogPrior, logLikelihoodFunction, randomSource, runId * chainCount, out bool accepted, runUnderPrior);
                        if (accepted)
                        {
                            acceptedCount++;
                        }

                        for (int j = 0; j < variables.Length; j++)
                        {
                            if (j == i)
                            {
                                variables[j].StepSize = stepSizes[j] * stepSizeMultiplier;
                            }
                            else
                            {
                                variables[j].StepSize = stepSizes[j];
                            }
                        }

                        currentVariableValues = MCMCStep(1, likModels, meanLikModel, ref m1, variables, currentVariableValues, ref currentLogLikelihood, ref currentLogPrior, logLikelihoodFunction, randomSource, runId * chainCount, out bool ignored, runUnderPrior);
                    }

                    double accValue = (double)acceptedCount / tuningSteps;

                    if (!testedValues.ContainsKey(stepSizeMultiplier))
                    {
                        testedValues.Add(stepSizeMultiplier, accValue);
                    }
                    else
                    {
                        testedValues[stepSizeMultiplier] = accValue;
                    }

                    if (accValue > magicAcceptanceRate)
                    {
                        IEnumerable<KeyValuePair<double, double>> lowerValues = from el in testedValues where el.Value <= magicAcceptanceRate orderby el.Value descending select el;

                        if (lowerValues.Count() == 0)
                        {
                            stepSizeMultiplier *= Math.E;
                        }
                        else
                        {
                            stepSizeMultiplier = Math.Exp((magicAcceptanceRate - lowerValues.ElementAt(0).Value) / (accValue - lowerValues.ElementAt(0).Value) * (Math.Log(stepSizeMultiplier) - Math.Log(lowerValues.ElementAt(0).Key)) + Math.Log(lowerValues.ElementAt(0).Key));
                        }
                    }
                    else if (accValue < magicAcceptanceRate)
                    {
                        IEnumerable<KeyValuePair<double, double>> higherValues = from el in testedValues where el.Value >= magicAcceptanceRate orderby el.Value ascending select el;

                        if (higherValues.Count() == 0)
                        {
                            stepSizeMultiplier /= Math.E;
                        }
                        else
                        {
                            stepSizeMultiplier = Math.Exp(Math.Log(stepSizeMultiplier) + (magicAcceptanceRate - accValue) / (higherValues.ElementAt(0).Value - accValue) * (Math.Log(higherValues.ElementAt(0).Key) - Math.Log(stepSizeMultiplier)));
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                IEnumerable<KeyValuePair<double, double>> lowerBound = from el in testedValues where el.Value <= magicAcceptanceRate orderby el.Value descending select el;
                IEnumerable<KeyValuePair<double, double>> upperBound = from el in testedValues where el.Value >= magicAcceptanceRate orderby el.Value ascending select el;

                if (lowerBound.Count() > 0 && upperBound.Count() > 0)
                {
                    if (lowerBound.ElementAt(0).Value == upperBound.ElementAt(0).Value)
                    {
                        stepSizeMultiplier = lowerBound.ElementAt(0).Key;
                    }
                    else
                    {
                        stepSizeMultiplier = Math.Exp((magicAcceptanceRate - lowerBound.ElementAt(0).Value) / (upperBound.ElementAt(0).Value - lowerBound.ElementAt(0).Value) * (Math.Log(upperBound.ElementAt(0).Key) - Math.Log(lowerBound.ElementAt(0).Key)) + Math.Log(lowerBound.ElementAt(0).Key));
                    }
                }
                else if (lowerBound.Count() > 0)
                {
                    stepSizeMultiplier = lowerBound.ElementAt(0).Key * Math.E;
                }
                else if (upperBound.Count() > 0)
                {
                    stepSizeMultiplier = upperBound.ElementAt(0).Key / Math.E;
                }
                else
                {
                    stepSizeMultiplier = 1;
                }

                stepSizeMultipliers[i] = stepSizeMultiplier;
            }

            return stepSizeMultipliers;
        }

        private static void RunChain(int chainId, MCMCChain chain, LikelihoodModel[] likModels, LikelihoodModel meanLikModel, ref int currentTreeIndex, double[][] currentVariableValues, double currentLogLikelihood, double currentLogPrior, MCMCVariable[] variables, Func<LikelihoodModel, double[][], int, double> logLikelihoodFunction, Random randomSource, EventWaitHandle waitForDiagnosticHandle, EventWaitHandle diagnosticHandle, EventWaitHandle proceedHandle, EventWaitHandle stopHandle, EventWaitHandle sampleHandle, EventWaitHandle proceedSampleHandle, EventWaitHandle waitForSwapHandle, EventWaitHandle waitingForSwapHandle, EventWaitHandle proceedAfterSwapHandle, bool runUnderPrior)
        {
            int acceptedSteps = 0;

            int mainStep = 0;

            while (true)
            {
                mainStep++;
                if (stopHandle.WaitOne(0))
                {
                    diagnosticHandle.Set();
                    return;
                }

                if (waitForSwapHandle.WaitOne(0))
                {
                    waitForSwapHandle.Reset();
                    EventWaitHandle.SignalAndWait(waitingForSwapHandle, proceedAfterSwapHandle);
                    proceedAfterSwapHandle.Reset();
                }

                currentVariableValues = MCMCStep(chain.Temperature, likModels, meanLikModel, ref currentTreeIndex, variables, currentVariableValues, ref currentLogLikelihood, ref currentLogPrior, logLikelihoodFunction, randomSource, chainId, out bool accepted, runUnderPrior);

                if (accepted)
                {
                    acceptedSteps++;
                }

                if (mainStep % sampleFrequency == 0 && chain.Temperature == 1)
                {
                    for (int i = 0; i < variables.Length; i++)
                    {
                        switch (variables[i].Type)
                        {
                            case MCMCVariable.VariableType.Uni:
                                ((MCMCUniVariable)variables[i]).Samples.Add(currentVariableValues[i][0]);
                                break;
                            case MCMCVariable.VariableType.Multi:
                                ((MCMCMultiVariable)variables[i]).Samples.Add((double[])currentVariableValues[i].Clone());
                                break;
                        }
                    }
                    chain.CurrentLogLikelihood = currentLogLikelihood;
                    chain.CurrentLogPrior = currentLogPrior;
                    chain.Step = mainStep;

                    EventWaitHandle.SignalAndWait(sampleHandle, proceedSampleHandle);
                    proceedSampleHandle.Reset();
                }

                if (waitForDiagnosticHandle.WaitOne(0))
                {
                    waitForDiagnosticHandle.Reset();
                    chain.AcceptanceRate = (double)acceptedSteps / mainStep;
                    EventWaitHandle.SignalAndWait(diagnosticHandle, proceedHandle);
                    proceedHandle.Reset();
                }
            }
        }

        private static double[][] MCMCStep(double temperature, LikelihoodModel[] likModels, LikelihoodModel meanLikModel, ref int currentTreeIndex, MCMCVariable[] variables, double[][] currentVariableValues, ref double currentLogLikelihood, ref double currentLogPrior, Func<LikelihoodModel, double[][], int, double> logLikelihoodFunction, Random randomSource, int chainId, out bool accepted, bool runUnderPrior)
        {
            if (currentTreeIndex >= 0)
            {
                if (likModels.Length > 0 && randomSource.NextDouble() < globalTreeSwapProbability)
                {
                    currentTreeIndex = randomSource.Next(likModels.Length);
                }
            }

            double[][] proposalVariableValues = new double[variables.Length][];

            double proposalLogPrior = 0;
            double logHastingsRatio = 0;

            double multinomialStepProb = randomSource.NextDouble();

            for (int i = 0; i < variables.Length; i++)
            {
                if (variables[i].StepSize > 0)
                {
                    variables[i].StepSize = Math.Min(100, variables[i].StepSize);

                    switch (variables[i].Type)
                    {
                        case MCMCVariable.VariableType.Uni:
                            if (((MCMCUniVariable)variables[i]).ProposalType == MCMCUniVariable.ProposalTypes.NormalSlide)
                            {
                                proposalVariableValues[i] = new double[] { Math.Abs(currentVariableValues[i][0] + Normal.Sample(randomSource, 0, variables[i].StepSize)) };
                            }
                            proposalLogPrior += ((MCMCUniVariable)variables[i]).PriorDistribution.DensityLn(proposalVariableValues[i][0]);
                            break;
                        case MCMCVariable.VariableType.Multi:
                            if (((MCMCMultiVariable)variables[i]).ProposalType == MCMCMultiVariable.ProposalTypes.Dirichlet)
                            {
                                double[] alpha = (from el in currentVariableValues[i] select el / ((MCMCMultiVariable)variables[i]).Scale / variables[i].StepSize).ToArray();
                                double[] sample = Dirichlet.Sample(randomSource, alpha);

                                bool isAnyNan = false;

                                do
                                {
                                    isAnyNan = false;
                                    for (int j = 0; j < sample.Length; j++)
                                    {
                                        if (double.IsNaN(sample[j]))
                                        {
                                            isAnyNan = true;
                                            sample = Dirichlet.Sample(randomSource, alpha);
                                            break;
                                        }
                                    }
                                } while (isAnyNan);

                                proposalVariableValues[i] = (from el in sample select el * ((MCMCMultiVariable)variables[i]).Scale).ToArray();

                                logHastingsRatio += new Dirichlet((from el in proposalVariableValues[i] select el / ((MCMCMultiVariable)variables[i]).Scale / variables[i].StepSize).ToArray(), randomSource).DensityLn((from el in currentVariableValues[i] select el / ((MCMCMultiVariable)variables[i]).Scale).ToArray()) - new Dirichlet(alpha, randomSource).DensityLn((from el in proposalVariableValues[i] select el / ((MCMCMultiVariable)variables[i]).Scale).ToArray());
                            }
                            else if (((MCMCMultiVariable)variables[i]).ProposalType == MCMCMultiVariable.ProposalTypes.Multinomial)
                            {
                                if (multinomialStepProb < ((MCMCMultiVariable)variables[i]).StepSize)
                                {
                                    int sampleInd = randomSource.Next(0, ((MCMCMultiVariable)variables[i]).Length);

                                    double[] sample = new double[((MCMCMultiVariable)variables[i]).Length];

                                    for (int j = 0; j < sample.Length; j++)
                                    {
                                        sample[j] = j == sampleInd ? 1 : 0;
                                    }

                                    proposalVariableValues[i] = (from el in sample select el * ((MCMCMultiVariable)variables[i]).Scale).ToArray();
                                }
                                else
                                {
                                    proposalVariableValues[i] = (double[])currentVariableValues[i].Clone();
                                }
                            }
                            proposalLogPrior += ((MCMCMultiVariable)variables[i]).PriorDistribution.DensityLn(proposalVariableValues[i]);
                            break;
                    }
                }
                else
                {
                    proposalVariableValues[i] = (double[])currentVariableValues[i].Clone();
                    switch (variables[i].Type)
                    {
                        case MCMCVariable.VariableType.Uni:
                            proposalLogPrior += ((MCMCUniVariable)variables[i]).PriorDistribution.DensityLn(proposalVariableValues[i][0]);
                            break;
                        case MCMCVariable.VariableType.Multi:
                            proposalLogPrior += ((MCMCMultiVariable)variables[i]).PriorDistribution.DensityLn(proposalVariableValues[i]);
                            break;
                    }
                }
            }

            double proposalLogLikelihood = 0;

            if (!runUnderPrior)
            {
                if (currentTreeIndex >= 0)
                {
                    proposalLogLikelihood = logLikelihoodFunction(likModels[currentTreeIndex], proposalVariableValues, chainId);
                }
                else
                {
                    proposalLogLikelihood = logLikelihoodFunction(meanLikModel, proposalVariableValues, chainId);
                }
            }
            else
            {
                proposalLogLikelihood = 0;
            }

            if (double.IsInfinity(proposalLogLikelihood))
            {
                accepted = false;
            }
            else
            {
                double acceptanceProbability = Math.Exp((proposalLogLikelihood - currentLogLikelihood + proposalLogPrior - currentLogPrior) * temperature + logHastingsRatio);

                if (randomSource.NextDouble() < acceptanceProbability)
                {
                    accepted = true;
                    currentLogLikelihood = proposalLogLikelihood;
                    currentLogPrior = proposalLogPrior;
                    currentVariableValues = proposalVariableValues;
                }
                else
                {
                    accepted = false;
                }
            }

            return currentVariableValues;
        }
    }
}
