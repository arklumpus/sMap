using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using MatrixExponential;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Utils
{
    public class Likelihoods
    {
        public static bool StoreComputedLikelihoods = false;

        public static ConcurrentBag<string> StoredLikelihoods;

        public static int ParallelMLE = 1;
        public static int MLERounds = 1;

        public static bool UseEstimatedPis = false;

        public static double ComputeAllLikelihoods(LikelihoodModel model, DataMatrix data, CharacterDependency[][] dependencies, Dictionary<string, Parameter>[] pi, Dictionary<string, Parameter>[] rates, bool useEstimatedPis, bool returnEstimatedPis, out Dictionary<string, double>[] estimatedPis)
        {
            if (useEstimatedPis && returnEstimatedPis)
            {
                estimatedPis = new Dictionary<string, double>[dependencies.Length];
            }
            else
            {
                estimatedPis = null;
            }

            double tbr = 0;
            for (int i = 0; i < dependencies.Length; i++)
            {
                if (dependencies[i].Length == 1 && dependencies[i][0].Type == CharacterDependency.Types.Independent)
                {
                    if (useEstimatedPis)
                    {
                        if (!returnEstimatedPis)
                        {
                            tbr += ComputeLikelihoods(model, (data.States[dependencies[i][0].Index], data.Data.Fold(dependencies[i][0].Index)), pi[dependencies[i][0].Index], rates[dependencies[i][0].Index], true, out _);
                        }
                        else
                        {
                            tbr += ComputeLikelihoods(model, (data.States[dependencies[i][0].Index], data.Data.Fold(dependencies[i][0].Index)), pi[dependencies[i][0].Index], rates[dependencies[i][0].Index], true, out double[][] likelihoods);

                            estimatedPis[i] = new Dictionary<string, double>();

                            double denomin = Utils.LogSumExp(likelihoods[likelihoods.Length - 1]);

                            for (int j = 0; j < data.States[dependencies[i][0].Index].Length; j++)
                            {
                                estimatedPis[i][data.States[dependencies[i][0].Index][j]] = Math.Exp(likelihoods[likelihoods.Length - 1][j] - denomin);
                            }
                        }
                    }
                    else
                    {
                        tbr += ComputeLikelihoods(model, (data.States[dependencies[i][0].Index], data.Data.Fold(dependencies[i][0].Index)), pi[dependencies[i][0].Index], rates[dependencies[i][0].Index], false, out _);
                    }
                }
                else
                {
                    double[][] ignore = null;
                    tbr += ComputeJointLikelihoods(model, data, dependencies[i], pi, rates, null, ignore, true);
                }
            }

            return tbr;
        }

        public static double[] EstimateParameters(MaximisationStrategy[] strategies, DataMatrix data, CharacterDependency[] dependencies, Dictionary<string, Parameter>[] rates, Dictionary<string, Parameter>[] pi, LikelihoodModel likModel, Random randomSource, /*bool computeHessian,*/ out bool mcmcRequired, out bool mlPerformed/*, out double[][,] hessianMatrix*/)
        {
            (int mlParameterCount, int bayesParameterCount, List<List<Parameter>> ratesToEstimate, List<(double remainingPi, List<Parameter> pis, int[] equalCounts)> pisToEstimate) parametersToEstimate = Utils.GetParametersToEstimate(dependencies, rates, pi);

            int bayesParameterCount = parametersToEstimate.bayesParameterCount;
            int mlParameterCount = parametersToEstimate.mlParameterCount;
            List<List<Parameter>> ratesToEstimate = parametersToEstimate.ratesToEstimate;
            List<(double remainingPi, List<Parameter> pis, int[] equalCounts)> pisToEstimate = parametersToEstimate.pisToEstimate;

            double[] startValues = new double[bayesParameterCount + mlParameterCount + pisToEstimate.Count];
            List<(Utils.VariableStepType stepType, int[] affectedVariables, double sigma)> stepTypes = new List<(Utils.VariableStepType stepType, int[] affectedVariables, double sigma)>();

            {
                int valInd = 0;
                List<int> normalSlides = new List<int>();

                for (int i = 0; i < ratesToEstimate.Count; i++)
                {
                    for (int j = 0; j < ratesToEstimate[i].Count; j++)
                    {
                        if (ratesToEstimate[i][j].Action == Parameter.ParameterAction.ML || ratesToEstimate[i][j].Action == Parameter.ParameterAction.Bayes)
                        {
                            ratesToEstimate[i][j].Value = Exponential.Sample(randomSource, 1);
                            startValues[valInd] = ratesToEstimate[i][j].Value;
                            normalSlides.Add(valInd);
                            valInd++;
                        }
                    }
                }

                stepTypes.Add((Utils.VariableStepType.NormalSlide, normalSlides.ToArray(), 0.2));

                List<List<int>> dirichlets = new List<List<int>>();
                List<List<int>> multinomials = new List<List<int>>();

                for (int i = 0; i < pisToEstimate.Count; i++)
                {
                    List<int> currDirichlet = new List<int>();
                    List<int> currMultinomials = new List<int>();

                    int countToEstimate = 0;
                    int countToEstimateMultinomial = 0;
                    double[] multinomialParameters = new double[pisToEstimate[i].pis.Count];
                    for (int j = 0; j < pisToEstimate[i].pis.Count; j++)
                    {
                        if (pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.Dirichlet || pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.ML || pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.Multinomial)
                        {
                            countToEstimate++;
                        }
                        if (pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.Multinomial)
                        {
                            countToEstimateMultinomial++;
                            multinomialParameters[j] = pisToEstimate[i].pis[j].DistributionParameter;
                        }
                    }

                    int multinomialInd = -1;

                    if (countToEstimateMultinomial > 0)
                    {
                        multinomialInd = Categorical.Sample(randomSource, multinomialParameters);
                    }

                    for (int j = 0; j < pisToEstimate[i].pis.Count; j++)
                    {
                        if (pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.Dirichlet || pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.ML)
                        {
                            startValues[valInd] = pisToEstimate[i].remainingPi / (double)countToEstimate;
                            pisToEstimate[i].pis[j].Value = startValues[valInd];
                            currDirichlet.Add(valInd);
                            valInd++;
                        }
                        else if (pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.Multinomial)
                        {
                            if (j == multinomialInd)
                            {
                                startValues[valInd] = pisToEstimate[i].remainingPi / (double)countToEstimate * (double)countToEstimateMultinomial;
                                pisToEstimate[i].pis[j].Value = startValues[valInd];
                            }
                            else
                            {
                                startValues[valInd] = 0;
                                pisToEstimate[i].pis[j].Value = startValues[valInd];
                            }
                            currMultinomials.Add(valInd);
                            valInd++;
                        }
                    }

                    if (currDirichlet.Count > 0)
                    {
                        dirichlets.Add(currDirichlet);
                    }


                    if (currMultinomials.Count > 0)
                    {
                        multinomials.Add(currMultinomials);
                    }
                }

                stepTypes.AddRange(from el in dirichlets select (Utils.VariableStepType.Dirichlet, el.ToArray(), 0.01));
                stepTypes.AddRange(from el in multinomials select (Utils.VariableStepType.Multinomial, el.ToArray(), 0.0));
            }

            if (bayesParameterCount > 0)
            {
                mcmcRequired = true;
                ConsoleWrapper.WriteLine("{0} parameter(s) are to be estimated using bayesian inference", bayesParameterCount);
            }
            else
            {
                if (Utils.RunningGui)
                {
                    Utils.Trigger("BayesSkipped", new object[] { });
                }
                mcmcRequired = false;
            }

            if (mlParameterCount > 0)
            {
                ConsoleWrapper.WriteLine("{0} parameter(s) are to be estimated using maximum-likelihood", mlParameterCount);
                mlPerformed = true;
            }
            else
            {
                if (Utils.RunningGui)
                {
                    Utils.Trigger("MLSkipped", new object[] { });
                }

                mlPerformed = false;
                //hessianMatrix = null;
                return startValues;
            }

            ConsoleWrapper.WriteLine();

            ConsoleWrapper.WriteLine("Performing maximum-likelihood optimization...");

            Func<double[], double> threadUnsafeLikelihoodFunc = (vars) =>
            {
                if (vars.Min() < 0)
                {
                    return double.NaN;
                }

                int valInd = 0;

                for (int i = 0; i < ratesToEstimate.Count; i++)
                {
                    for (int j = 0; j < ratesToEstimate[i].Count; j++)
                    {
                        if (ratesToEstimate[i][j].Action == Parameter.ParameterAction.ML || ratesToEstimate[i][j].Action == Parameter.ParameterAction.Bayes)
                        {
                            ratesToEstimate[i][j].Value = vars[valInd];
                            valInd++;
                        }
                    }
                }

                for (int i = 0; i < pisToEstimate.Count; i++)
                {
                    for (int j = 0; j < pisToEstimate[i].pis.Count; j++)
                    {
                        if (pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.Dirichlet || pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.ML || pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.Multinomial)
                        {
                            pisToEstimate[i].pis[j].Value = vars[valInd] / (pisToEstimate[i].equalCounts[j] + 1);
                            valInd++;
                        }
                    }
                }

                if (!StoreComputedLikelihoods)
                {
                    return ComputeAllLikelihoods(likModel, data, new CharacterDependency[][] { dependencies }, pi, rates, UseEstimatedPis, false, out _);
                }
                else
                {
                    double likelihood = ComputeAllLikelihoods(likModel, data, new CharacterDependency[][] { dependencies }, pi, rates, UseEstimatedPis, false, out _);

                    StoredLikelihoods.Add(vars.Aggregate(likelihood.ToString(System.Globalization.CultureInfo.InvariantCulture), (a, b) => a + "\t" + b.ToString(System.Globalization.CultureInfo.InvariantCulture)));

                    return likelihood;
                }
            };

            Dictionary<string, Parameter>[][] pisByThread = new Dictionary<string, Parameter>[Math.Max(Utils.MaxThreads, ParallelMLE)][];
            Dictionary<string, Parameter>[][] ratesByThread = new Dictionary<string, Parameter>[Math.Max(Utils.MaxThreads, ParallelMLE)][];
            CharacterDependency[][] dependenciesByThread = new CharacterDependency[Math.Max(Utils.MaxThreads, ParallelMLE)][];

            for (int j = 0; j < Math.Max(Utils.MaxThreads, ParallelMLE); j++)
            {
                dependenciesByThread[j] = new CharacterDependency[dependencies.Length];

                for (int k = 0; k < dependencies.Length; k++)
                {
                    switch (dependencies[k].Type)
                    {
                        case CharacterDependency.Types.Independent:
                            dependenciesByThread[j][k] = new CharacterDependency(dependencies[k].Index);
                            break;
                        case CharacterDependency.Types.Dependent:
                        case CharacterDependency.Types.Conditioned:
                            dependenciesByThread[j][k] = new CharacterDependency(dependencies[k].Index, dependencies[k].Type, (int[])dependencies[k].Dependencies.Clone(), Parameter.CloneParameterDictionary(dependencies[k].ConditionedProbabilities));
                            break;
                    }
                }

                pisByThread[j] = new Dictionary<string, Parameter>[pi.Length];

                for (int k = 0; k < pi.Length; k++)
                {
                    pisByThread[j][k] = Parameter.CloneParameterDictionary(pi[k]);
                }

                ratesByThread[j] = new Dictionary<string, Parameter>[rates.Length];

                for (int k = 0; k < rates.Length; k++)
                {
                    ratesByThread[j][k] = Parameter.CloneParameterDictionary(rates[k]);
                }
            }

            List<List<Parameter>>[] ratesToEstimateByThread = new List<List<Parameter>>[Math.Max(Utils.MaxThreads, ParallelMLE)];
            List<(double remainingPi, List<Parameter> pis, int[] equalCounts)>[] pisToEstimateByThread = new List<(double remainingPi, List<Parameter> pis, int[] equalCounts)>[Math.Max(Utils.MaxThreads, ParallelMLE)];

            for (int i = 0; i < Math.Max(Utils.MaxThreads, ParallelMLE); i++)
            {
                (int mlParameterCount, int bayesParameterCount, List<List<Parameter>> ratesToEstimate, List<(double remainingPi, List<Parameter> pis, int[] equalCounts)> pisToEstimate) parametersToEstimateByThread = Utils.GetParametersToEstimate(dependenciesByThread[i], ratesByThread[i], pisByThread[i]);
                ratesToEstimateByThread[i] = parametersToEstimateByThread.ratesToEstimate;
                pisToEstimateByThread[i] = parametersToEstimateByThread.pisToEstimate;
            }

            Func<double[], int, double> threadSafeLikelihoodFunc = (vars, threadInd) =>
            {
                if (vars.Min() < 0)
                {
                    return double.NaN;
                }

                int valInd = 0;

                for (int i = 0; i < ratesToEstimateByThread[threadInd].Count; i++)
                {
                    for (int j = 0; j < ratesToEstimateByThread[threadInd][i].Count; j++)
                    {
                        if (ratesToEstimateByThread[threadInd][i][j].Action == Parameter.ParameterAction.ML || ratesToEstimateByThread[threadInd][i][j].Action == Parameter.ParameterAction.Bayes)
                        {
                            ratesToEstimateByThread[threadInd][i][j].Value = vars[valInd];
                            valInd++;
                        }
                    }
                }

                for (int i = 0; i < pisToEstimateByThread[threadInd].Count; i++)
                {
                    for (int j = 0; j < pisToEstimateByThread[threadInd][i].pis.Count; j++)
                    {
                        if (pisToEstimateByThread[threadInd][i].pis[j].Action == Parameter.ParameterAction.Dirichlet || pisToEstimateByThread[threadInd][i].pis[j].Action == Parameter.ParameterAction.ML || pisToEstimateByThread[threadInd][i].pis[j].Action == Parameter.ParameterAction.Multinomial)
                        {
                            pisToEstimateByThread[threadInd][i].pis[j].Value = vars[valInd] / (pisToEstimateByThread[threadInd][i].equalCounts[j] + 1);
                            valInd++;
                        }
                    }
                }

                if (!StoreComputedLikelihoods)
                {
                    return ComputeAllLikelihoods(likModel, data, new CharacterDependency[][] { dependenciesByThread[threadInd] }, pisByThread[threadInd], ratesByThread[threadInd], UseEstimatedPis, false, out _);
                }
                else
                {
                    double likelihood = ComputeAllLikelihoods(likModel, data, new CharacterDependency[][] { dependenciesByThread[threadInd] }, pisByThread[threadInd], ratesByThread[threadInd], UseEstimatedPis, false, out _);

                    StoredLikelihoods.Add(vars.Aggregate(likelihood.ToString(System.Globalization.CultureInfo.InvariantCulture), (a, b) => a + "\t" + b.ToString(System.Globalization.CultureInfo.InvariantCulture)));

                    return likelihood;
                }
            };


            threadUnsafeLikelihoodFunc(startValues);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            threadUnsafeLikelihoodFunc(startValues);
            sw.Stop();

            string timedLikelihood = "";

            if (sw.ElapsedMilliseconds < 1000)
            {
                timedLikelihood = sw.ElapsedMilliseconds.ToString() + "ms";
            }
            else if (sw.ElapsedMilliseconds < 60000)
            {
                timedLikelihood = ((double)sw.ElapsedMilliseconds / 1000).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "s";
            }
            else
            {
                timedLikelihood = ((double)sw.ElapsedMilliseconds / 60000).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "min";
            }

            ConsoleWrapper.WriteLine("A single likelihood computation takes about {0}", timedLikelihood);

            for (int i = 0; i < 20 + 2; i++)
            {
                ConsoleWrapper.WriteLine();
            }

            int plotTop = ConsoleWrapper.CursorTop - 20;

            double[] bestVars = startValues;

            if (Utils.RunningGui)
            {
                Utils.Trigger("MLStarted", new object[] { });
            }

            sw.Restart();

            for (int j = 0; j < MLERounds; j++)
            {
                for (int i = 0; i < strategies.Length; i++)
                {
                    switch (strategies[i].Strategy)
                    {
                        case Strategies.RandomWalk:
                            if (ParallelMLE <= 1)
                            {
                                bestVars = Utils.AutoMaximiseFunctionRandomWalk(threadUnsafeLikelihoodFunc, bestVars, stepTypes.ToArray(), (RandomWalk)strategies[i], randomSource, Utils.RunningGui ? -2 : plotTop);
                            }
                            else
                            {
                                bestVars = RunParallelMaximisation((parameters, index) => Utils.AutoMaximiseFunctionRandomWalk((vars) => threadSafeLikelihoodFunc(vars, index), parameters, stepTypes.ToArray(), (RandomWalk)strategies[i], randomSource, Utils.RunningGui ? -2 : -1), threadSafeLikelihoodFunc, bestVars);
                                threadUnsafeLikelihoodFunc(bestVars);
                            }
                            break;
                        case Strategies.Sampling:
                            bestVars = Utils.AutoMaximiseFunctionSampling(threadUnsafeLikelihoodFunc, bestVars, stepTypes.ToArray(), (Sampling)strategies[i], j > 0 || i > 0, Utils.RunningGui ? -2 : plotTop);
                            break;
                        case Strategies.NesterovClimbing:
                            if (ParallelMLE <= 1)
                            {
                                bestVars = Utils.AutoMaximiseFunctionNesterov(threadUnsafeLikelihoodFunc, bestVars, stepTypes.ToArray(), (NesterovClimbing)strategies[i], randomSource, Utils.RunningGui ? -2 : plotTop);
                            }
                            else
                            {
                                bestVars = RunParallelMaximisation((parameters, index) => Utils.AutoMaximiseFunctionNesterov((vars) => threadSafeLikelihoodFunc(vars, index), bestVars, stepTypes.ToArray(), (NesterovClimbing)strategies[i], randomSource, Utils.RunningGui ? -2 : -1), threadSafeLikelihoodFunc, bestVars);
                                threadUnsafeLikelihoodFunc(bestVars);
                            }
                            break;
                        case Strategies.IterativeSampling:
                            bestVars = Utils.AutoMaximiseFunctionIterativeSampling(threadSafeLikelihoodFunc, bestVars, stepTypes.ToArray(), (IterativeSampling)strategies[i], j > 0 || i > 0, Utils.RunningGui ? -2 : plotTop);
                            threadUnsafeLikelihoodFunc(bestVars);
                            break;
                    }
                }
            }

            sw.Stop();

            timedLikelihood = "";

            if (sw.ElapsedMilliseconds < 1000)
            {
                timedLikelihood = sw.ElapsedMilliseconds.ToString() + "ms";
            }
            else if (sw.ElapsedMilliseconds < 60000)
            {
                timedLikelihood = ((double)sw.ElapsedMilliseconds / 1000).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "s";
            }
            else if (sw.ElapsedMilliseconds < 3600000)
            {
                timedLikelihood = ((double)sw.ElapsedMilliseconds / 60000).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "min";
            }
            else
            {
                timedLikelihood = ((double)sw.ElapsedMilliseconds / 3600000).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "h";
            }

            ConsoleWrapper.WriteLine("Maximum-likelihood optimization took about {0}", timedLikelihood);

            if (Utils.RunningGui)
            {
                Utils.Trigger("MLFinished", new object[] { });
            }

            ConsoleWrapper.WriteLine();
            ConsoleWrapper.WriteLine();

            /*if (computeHessian)
            {
                hessianMatrix = new double[2][,];
                hessianMatrix[0] = Utils.HessianMatrix(x => threadUnsafeLikelihoodFunc((from el in x select Math.Abs(el)).ToArray()), bestVars);

                List<int> non0Indices = new List<int>();

                for (int i = 0; i < bestVars.Length; i++)
                {
                    if (bestVars[i] >= 0.001)
                    {
                        non0Indices.Add(i);
                    }
                }

                Func<double[], double> non0LikFunc = (vals) =>
                {
                    double[] realVals = (double[])bestVars.Clone();
                    for (int i = 0; i < vals.Length; i++)
                    {
                        realVals[non0Indices[i]] = vals[i];
                    }
                    return threadUnsafeLikelihoodFunc(realVals);
                };



                hessianMatrix[1] = Utils.HessianMatrix(x => non0LikFunc((from el in x select Math.Abs(el)).ToArray()), (from el in non0Indices select bestVars[el]).ToArray());
            }
            else
            {
                hessianMatrix = null;
            }*/

            double mle = threadUnsafeLikelihoodFunc(bestVars);

            return bestVars;
        }


        private static double[] RunParallelMaximisation(Func<double[], int, double[]> threadSafeMaximizerFunction, Func<double[], int, double> threadSafeLikelihoodFunc, double[] bestVars)
        {
            double[][] bestVarsByThread = new double[ParallelMLE][];
            double[] bestValsByThread = new double[ParallelMLE];
            Thread[] threads = new Thread[ParallelMLE];
            for (int j = 0; j < ParallelMLE; j++)
            {
                int index = j;
                bestVarsByThread[j] = (double[])bestVars.Clone();
                threads[j] = new Thread(() =>
                {
                    bestVarsByThread[index] = threadSafeMaximizerFunction(bestVarsByThread[index], index);
                    bestValsByThread[index] = threadSafeLikelihoodFunc(bestVarsByThread[index], index);
                });
            }

            Utils.PlotTriggerMarshal = Utils.NewParallelTriggerMarshal(double.MinValue);
            Utils.StepFinishedTriggerMarshal = Utils.NewParallelStepFinishedTriggerMarshal();

            for (int j = 0; j < ParallelMLE; j++)
            {
                threads[j].Start();
            }

            for (int j = 0; j < ParallelMLE; j++)
            {
                threads[j].Join();
            }

            Utils.PlotTriggerMarshal = Utils.DefaultPlotTriggerMarshal;
            Utils.StepFinishedTriggerMarshal = Utils.DefaultStepFinishedTriggerMarshal;

            int maxInd = bestValsByThread.MaxInd();
            return bestVarsByThread[maxInd];
        }

        public static double ComputeJointLikelihoods(LikelihoodModel model, DataMatrix data, CharacterDependency[] dependencies, Dictionary<string, Parameter>[] pi, Dictionary<string, Parameter>[] rates, double[] parameters, double[][] likelihoods, bool initializeOutput)
        {
            if (parameters != null)
            {
                (int bayesParameterCount, int mlParameterCount, List<List<Parameter>> ratesToEstimate, List<(double remainingPi, List<Parameter> pis, int[] equalCounts)> pisToEstimate) parametersToEstimate = Utils.GetParametersToEstimate(dependencies, rates, pi);

                int bayesParameterCount = parametersToEstimate.bayesParameterCount;
                int mlParameterCount = parametersToEstimate.mlParameterCount;
                List<List<Parameter>> ratesToEstimate = parametersToEstimate.ratesToEstimate;
                List<(double remainingPi, List<Parameter> pis, int[] equalCounts)> pisToEstimate = parametersToEstimate.pisToEstimate;

                int valInd = 0;

                for (int i = 0; i < ratesToEstimate.Count; i++)
                {
                    for (int j = 0; j < ratesToEstimate[i].Count; j++)
                    {
                        if (ratesToEstimate[i][j].Action == Parameter.ParameterAction.ML || ratesToEstimate[i][j].Action == Parameter.ParameterAction.Bayes)
                        {
                            ratesToEstimate[i][j].Value = parameters[valInd];
                            valInd++;
                        }
                    }
                }

                for (int i = 0; i < pisToEstimate.Count; i++)
                {
                    for (int j = 0; j < pisToEstimate[i].pis.Count; j++)
                    {
                        if (pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.ML || pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.Dirichlet || pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.Multinomial)
                        {
                            pisToEstimate[i].pis[j].Value = parameters[valInd];
                            valInd++;
                        }
                    }
                }
            }




            if (initializeOutput)
            {
                likelihoods = new double[model.Children.Length][];        //logLikelihoods[node][state]
            }


            double[][][] individualLikelihoods = new double[model.Children.Length][][];      //individualLogLikelihoods[node][character][state]

            Matrix<double>[] rateMatrix = new Matrix<double>[dependencies.Length];
            MatrixExponential.MatrixExponential[] exps = new MatrixExponential.MatrixExponential[dependencies.Length];

            for (int k = 0; k < dependencies.Length; k++)
            {
                if (dependencies[k].Type == CharacterDependency.Types.Independent)
                {
                    rateMatrix[k] = Matrix<double>.Build.Dense(data.States[dependencies[k].Index].Length, data.States[dependencies[k].Index].Length);
                    for (int i = 0; i < data.States[dependencies[k].Index].Length; i++)
                    {
                        double sumI = 0;
                        for (int j = 0; j < data.States[dependencies[k].Index].Length; j++)
                        {
                            if (i != j)
                            {
                                rateMatrix[k][i, j] = rates[dependencies[k].Index][data.States[dependencies[k].Index][i] + ">" + data.States[dependencies[k].Index][j]];
                                sumI += rates[dependencies[k].Index][data.States[dependencies[k].Index][i] + ">" + data.States[dependencies[k].Index][j]];
                            }
                        }
                        rateMatrix[k][i, i] = -sumI;
                    }

                    exps[k] = rateMatrix[k].FastExponential(1);
                }
            }

            int numStates = 1;
            List<int[]> statesList = new List<int[]>();        //states[character][i]

            int numUnconditionedStates = 1;
            List<int[]> unconditionedStatesList = new List<int[]>();

            for (int i = 0; i < dependencies.Length; i++)
            {
                switch (dependencies[i].Type)
                {
                    case CharacterDependency.Types.Independent:
                        numStates *= data.States[dependencies[i].Index].Length;
                        statesList.Add(Utils.Range(0, data.States[dependencies[i].Index].Length));
                        numUnconditionedStates *= data.States[dependencies[i].Index].Length;
                        unconditionedStatesList.Add(Utils.Range(0, data.States[dependencies[i].Index].Length));
                        break;
                    case CharacterDependency.Types.Conditioned:
                        numStates *= data.States[dependencies[i].Index].Length;
                        statesList.Add(Utils.Range(0, data.States[dependencies[i].Index].Length));
                        unconditionedStatesList.Add(new int[] { -1 });
                        break;
                    case CharacterDependency.Types.Dependent:
                        throw new NotImplementedException();
                }

            }

            int[][] states = statesList.ToArray();

            int[][] allPossibleStates = Utils.GetCombinations(states);        //allPossibleStates[combinationIndex][i]

            int[][] allPossibleUnconditionedStates = Utils.GetCombinations(unconditionedStatesList.ToArray());

            int[][] unconditionedStatesCorresp = new int[allPossibleUnconditionedStates.Length][];

            for (int i = 0; i < allPossibleUnconditionedStates.Length; i++)
            {
                unconditionedStatesCorresp[i] = new int[numStates / numUnconditionedStates];
                int ind = 0;
                for (int j = 0; j < allPossibleStates.Length; j++)
                {
                    bool found = true;
                    for (int k = 0; k < dependencies.Length; k++)
                    {
                        if (dependencies[k].Type != CharacterDependency.Types.Conditioned && allPossibleStates[j][k] != allPossibleUnconditionedStates[i][k])
                        {
                            found = false;
                            break;
                        }
                    }

                    if (found)
                    {
                        unconditionedStatesCorresp[i][ind] = j;
                        ind++;
                    }
                }
            }

            double[][] allPossibleStatesCondProbs = new double[dependencies.Length][];


            for (int k = 0; k < dependencies.Length; k++)
            {
                if (dependencies[k].Type == CharacterDependency.Types.Conditioned)
                {
                    allPossibleStatesCondProbs[k] = new double[allPossibleStates.Length];
                    for (int j = 0; j < allPossibleStates.Length; j++)
                    {
                        allPossibleStatesCondProbs[k][j] = dependencies[k].ConditionedProbabilities[Utils.StringifyArray(from el in dependencies[k].Dependencies select data.States[el][allPossibleStates[j][el]]) + ">" + data.States[dependencies[k].Index][allPossibleStates[j][k]]];
                    }
                }
            }

            foreach (KeyValuePair<string, int> kvp in model.NamedBranches)
            {
                int i = kvp.Value;
                if (initializeOutput)
                {
                    likelihoods[i] = new double[numStates];
                }
                individualLikelihoods[i] = new double[dependencies.Length][];

                for (int j = 0; j < dependencies.Length; j++)
                {
                    individualLikelihoods[i][j] = new double[data.States[dependencies[j].Index].Length];
                    for (int k = 0; k < data.States[dependencies[j].Index].Length; k++)
                    {
                        individualLikelihoods[i][j][k] = Math.Log(data.Data[kvp.Key][dependencies[j].Index][k]);
                    }
                }

                for (int j = 0; j < numStates; j++)
                {
                    likelihoods[i][j] = 0;

                    for (int k = 0; k < dependencies.Length; k++)
                    {
                        switch (dependencies[k].Type)
                        {
                            case CharacterDependency.Types.Independent:
                                likelihoods[i][j] += individualLikelihoods[i][k][allPossibleStates[j][k]];
                                break;
                            case CharacterDependency.Types.Dependent:
                                throw new NotImplementedException();
                            case CharacterDependency.Types.Conditioned:
                                likelihoods[i][j] += individualLikelihoods[i][k][allPossibleStates[j][k]];
                                break;
                        }
                    }
                }
            }

            for (int i = 0; i < model.Children.Length; i++)
            {
                if (model.Children[i].Length != 0)
                {
                    if (initializeOutput)
                    {
                        likelihoods[i] = new double[numStates];
                    }

                    for (int l = 0; l < numStates; l++)
                    {
                        likelihoods[i][l] = 0;

                        for (int j = 0; j < model.Children[i].Length; j++)
                        {
                            Matrix<double>[] transitionMatrices = new Matrix<double>[dependencies.Length];
                            for (int k = 0; k < dependencies.Length; k++)
                            {
                                if (dependencies[k].Type == CharacterDependency.Types.Independent)
                                {
                                    transitionMatrices[k] = rateMatrix[k].FastExponential(model.BranchLengths[model.Children[i][j]], exps[k]).Result;
                                }
                            }

                            double[] stateLikelihoods = new double[numStates];

                            for (int m = 0; m < numStates; m++)
                            {
                                stateLikelihoods[m] = likelihoods[model.Children[i][j]][m];

                                double transProb = 1;
                                double logNoTransProb = 0;
                                double condProb = 1;
                                bool condSame = true;

                                for (int k = 0; k < dependencies.Length; k++)
                                {
                                    if (dependencies[k].Type == CharacterDependency.Types.Independent)
                                    {
                                        transProb *= transitionMatrices[k][allPossibleStates[l][k], allPossibleStates[m][k]];

                                        if (allPossibleStates[l][k] != allPossibleStates[m][k])
                                        {
                                            logNoTransProb = double.NegativeInfinity;
                                        }

                                        if (logNoTransProb > double.NegativeInfinity)
                                        {
                                            logNoTransProb += model.BranchLengths[model.Children[i][j]] * rateMatrix[k][allPossibleStates[l][k], allPossibleStates[l][k]];
                                        }
                                    }
                                    else if (dependencies[k].Type == CharacterDependency.Types.Conditioned)
                                    {
                                        condProb *= allPossibleStatesCondProbs[k][m];

                                        if (allPossibleStates[l][k] != allPossibleStates[m][k])
                                        {
                                            condSame = false;
                                        }
                                    }
                                }

                                stateLikelihoods[m] += Math.Log(transProb * condProb + Math.Exp(logNoTransProb) * ((condSame ? 1 : 0) - condProb));
                            }

                            likelihoods[i][l] += Utils.LogSumExp(stateLikelihoods);
                        }
                    }
                }
            }

            double[] rootStateLikelihoods = new double[numStates];

            for (int j = 0; j < numStates; j++)
            {
                double piJ = 1;

                for (int k = 0; k < dependencies.Length; k++)
                {
                    if (dependencies[k].Type == CharacterDependency.Types.Independent)
                    {
                        piJ *= pi[k][data.States[k][allPossibleStates[j][k]]];
                    }
                    else if (dependencies[k].Type == CharacterDependency.Types.Conditioned)
                    {
                        piJ *= allPossibleStatesCondProbs[k][j];
                    }
                }

                rootStateLikelihoods[j] = Math.Log(piJ) + likelihoods[likelihoods.Length - 1][j];
            }

            return Utils.LogSumExp(rootStateLikelihoods);

        }

        public static double ComputeLikelihoods(LikelihoodModel model, (string[] States, IDictionary<string, double[]> Data) data, Dictionary<string, Parameter> pi, Dictionary<string, Parameter> rates, bool useEstimatedPis, out double[][] likelihoods)
        {
            likelihoods = new double[model.Children.Length][];

            int numStates = data.States.Length;

            foreach (KeyValuePair<string, int> kvp in model.NamedBranches)
            {
                int i = kvp.Value;
                likelihoods[i] = new double[numStates];
                for (int j = 0; j < numStates; j++)
                {
                    likelihoods[i][j] = Math.Log(data.Data[kvp.Key][j]);
                }
            }

            Matrix<double> rateMatrix = Matrix<double>.Build.Dense(data.States.Length, data.States.Length);

            for (int i = 0; i < data.States.Length; i++)
            {
                double sumI = 0;
                for (int j = 0; j < data.States.Length; j++)
                {
                    if (i != j)
                    {
                        rateMatrix[i, j] = rates[data.States[i] + ">" + data.States[j]];
                        sumI += rates[data.States[i] + ">" + data.States[j]];
                    }
                }
                rateMatrix[i, i] = -sumI;
            }

            switch (numStates)
            {
                case 2:
                    double sum = rateMatrix[0, 1] + rateMatrix[1, 0];
                    double logSum = Math.Log(rateMatrix[0, 1] + rateMatrix[1, 0]);
                    double log01 = Math.Log(rateMatrix[0, 1]);
                    double log10 = Math.Log(rateMatrix[1, 0]);
                    for (int i = 0; i < model.Children.Length; i++)
                    {
                        if (model.Children[i].Length != 0)
                        {
                            likelihoods[i] = new double[numStates];

                            for (int l = 0; l < model.Children[i].Length; l++)
                            {
                                double expTerm = Math.Exp(-model.BranchLengths[model.Children[i][l]] * sum);

                                if (likelihoods[model.Children[i][l]][1] > likelihoods[model.Children[i][l]][0])
                                {
                                    likelihoods[i][0] += -logSum + log01 + likelihoods[model.Children[i][l]][1] + Utils.Log1p(-expTerm) + Utils.Log1p(Math.Exp(likelihoods[model.Children[i][l]][0] - likelihoods[model.Children[i][l]][1]) * (expTerm + rateMatrix[1, 0] / rateMatrix[0, 1]) / (1 - expTerm));
                                    likelihoods[i][1] += -logSum + log01 + likelihoods[model.Children[i][l]][1] + Utils.Log1p(expTerm * rateMatrix[1, 0] / rateMatrix[0, 1]) + Utils.Log1p(rateMatrix[1, 0] / rateMatrix[0, 1] * Math.Exp(likelihoods[model.Children[i][l]][0] - likelihoods[model.Children[i][l]][1]) * (1 - expTerm) / (1 + expTerm * rateMatrix[1, 0] / rateMatrix[0, 1]));
                                }
                                else
                                {
                                    likelihoods[i][0] += -logSum + log10 + likelihoods[model.Children[i][l]][0] + Utils.Log1p(expTerm * rateMatrix[0, 1] / rateMatrix[1, 0]) + Utils.Log1p(rateMatrix[0, 1] / rateMatrix[1, 0] * Math.Exp(likelihoods[model.Children[i][l]][1] - likelihoods[model.Children[i][l]][0]) * (1 - expTerm) / (1 + expTerm * rateMatrix[0, 1] / rateMatrix[1, 0]));
                                    likelihoods[i][1] += -logSum + log10 + likelihoods[model.Children[i][l]][0] + Utils.Log1p(-expTerm) + Utils.Log1p(Math.Exp(likelihoods[model.Children[i][l]][1] - likelihoods[model.Children[i][l]][0]) * (expTerm + rateMatrix[0, 1] / rateMatrix[1, 0]) / (1 - expTerm));
                                }
                            }
                        }
                    }
                    break;
                default:
                    MatrixExponential.MatrixExponential exp = rateMatrix.FastExponential(1);

                    for (int i = 0; i < model.Children.Length; i++)
                    {
                        if (model.Children[i].Length != 0)
                        {
                            likelihoods[i] = new double[numStates];

                            for (int l = 0; l < model.Children[i].Length; l++)
                            {
                                Matrix<double> res = rateMatrix.FastExponential(model.BranchLengths[model.Children[i][l]], exp).Result;

                                res.TimesLogVectorAndAdd(likelihoods[model.Children[i][l]], likelihoods[i]);
                            }


                        }
                    }
                    break;
            }

            double tbr = 0;

            if (!useEstimatedPis)
            {
                for (int i = 0; i < numStates; i++)
                {
                    tbr += pi[data.States[i]] * Math.Exp(likelihoods[likelihoods.Length - 1][i]);
                }
            }
            else
            {
                double[] estimatedPis = new double[numStates];

                double denomin = Utils.LogSumExp(likelihoods[likelihoods.Length - 1]);

                for (int i = 0; i < numStates; i++)
                {
                    estimatedPis[i] = Math.Exp(likelihoods[likelihoods.Length - 1][i] - denomin);
                }

                for (int i = 0; i < numStates; i++)
                {
                    tbr += estimatedPis[i] * Math.Exp(likelihoods[likelihoods.Length - 1][i]);
                }
            }

            tbr = Math.Log(tbr);

            return tbr;
        }

        public static double[][] ComputeAndSampleJointPriors(LikelihoodModel model, string[][] states, Dictionary<string, Parameter>[] pi, Dictionary<string, Parameter>[] rates, double[] parameters, CharacterDependency[] dependencies, double[][] prior, bool reinitializePrior, Random randomSource)
        {
            (int bayesParameterCount, int mlParameterCount, List<List<Parameter>> ratesToEstimate, List<(double remainingPi, List<Parameter> pis, int[] equalCounts)> pisToEstimate) parametersToEstimate = Utils.GetParametersToEstimate(dependencies, rates, pi);

            int bayesParameterCount = parametersToEstimate.bayesParameterCount;
            int mlParameterCount = parametersToEstimate.mlParameterCount;
            List<List<Parameter>> ratesToEstimate = parametersToEstimate.ratesToEstimate;
            List<(double remainingPi, List<Parameter> pis, int[] equalCounts)> pisToEstimate = parametersToEstimate.pisToEstimate;

            int valInd = 0;

            for (int i = 0; i < ratesToEstimate.Count; i++)
            {
                for (int j = 0; j < ratesToEstimate[i].Count; j++)
                {
                    if (ratesToEstimate[i][j].Action == Parameter.ParameterAction.ML || ratesToEstimate[i][j].Action == Parameter.ParameterAction.Bayes)
                    {
                        ratesToEstimate[i][j].Value = parameters[valInd];
                        valInd++;
                    }
                }
            }

            for (int i = 0; i < pisToEstimate.Count; i++)
            {
                for (int j = 0; j < pisToEstimate[i].pis.Count; j++)
                {
                    if (pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.ML || pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.Dirichlet || pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.Multinomial)
                    {
                        pisToEstimate[i].pis[j].Value = parameters[valInd];
                        valInd++;
                    }
                }
            }

            if (reinitializePrior)
            {
                prior = new double[model.Parents.Length][];
            }

            Matrix<double>[] rateMatrix = new Matrix<double>[dependencies.Length];
            MatrixExponential.MatrixExponential[] exps = new MatrixExponential.MatrixExponential[dependencies.Length];

            for (int k = 0; k < dependencies.Length; k++)
            {
                rateMatrix[k] = Matrix<double>.Build.Dense(states[dependencies[k].Index].Length, states[dependencies[k].Index].Length);

                for (int i = 0; i < states[dependencies[k].Index].Length; i++)
                {
                    double sumI = 0;
                    for (int j = 0; j < states[dependencies[k].Index].Length; j++)
                    {
                        if (i != j)
                        {
                            rateMatrix[k][i, j] = rates[dependencies[k].Index][states[dependencies[k].Index][i] + ">" + states[dependencies[k].Index][j]];
                            sumI += rates[dependencies[k].Index][states[dependencies[k].Index][i] + ">" + states[dependencies[k].Index][j]];
                        }
                    }
                    rateMatrix[k][i, i] = -sumI;
                }

                exps[k] = rateMatrix[k].FastExponential(1);
            }

            int numStates = 1;
            List<int[]> statesList = new List<int[]>();        //states[character][i]

            for (int i = 0; i < dependencies.Length; i++)
            {
                switch (dependencies[i].Type)
                {
                    case CharacterDependency.Types.Independent:
                    case CharacterDependency.Types.Conditioned:
                        numStates *= states[dependencies[i].Index].Length;
                        statesList.Add(Utils.Range(0, states[dependencies[i].Index].Length));
                        break;
                    case CharacterDependency.Types.Dependent:
                        throw new NotImplementedException();
                }

            }

            int[][] statesInts = statesList.ToArray();

            int[][] allPossibleStates = Utils.GetCombinations(statesInts);        //allPossibleStates[combinationIndex][i]

            double[][] allPossibleStatesCondProbs = new double[dependencies.Length][];


            for (int k = 0; k < dependencies.Length; k++)
            {
                if (dependencies[k].Type == CharacterDependency.Types.Conditioned)
                {
                    allPossibleStatesCondProbs[k] = new double[allPossibleStates.Length];
                    for (int j = 0; j < allPossibleStates.Length; j++)
                    {
                        allPossibleStatesCondProbs[k][j] = dependencies[k].ConditionedProbabilities[Utils.StringifyArray(from el in dependencies[k].Dependencies select states[el][allPossibleStates[j][el]]) + ">" + states[dependencies[k].Index][allPossibleStates[j][k]]];
                    }
                }
            }

            for (int i = model.Parents.Length - 1; i >= 0; i--)
            {
                if (reinitializePrior)
                {
                    prior[i] = new double[numStates];
                }

                if (model.Parents[i] < 0)
                {
                    for (int j = 0; j < numStates; j++)
                    {
                        prior[i][j] = 1;

                        for (int k = 0; k < dependencies.Length; k++)
                        {
                            switch (dependencies[k].Type)
                            {
                                case CharacterDependency.Types.Independent:
                                    prior[i][j] *= pi[dependencies[k].Index][states[dependencies[k].Index][allPossibleStates[j][k]]].Value;
                                    break;
                                case CharacterDependency.Types.Conditioned:
                                    prior[i][j] *= allPossibleStatesCondProbs[k][j];
                                    break;
                            }
                        }
                    }

                    int sample = Categorical.Sample(randomSource, prior[i]);
                    for (int j = 0; j < numStates; j++)
                    {
                        if (j != sample)
                        {
                            prior[i][j] = 0;
                        }
                        else
                        {
                            prior[i][j] = 1;
                        }
                    }
                }
                else
                {
                    double[,] bigTransMat = new double[numStates, numStates].Fill(1);
                    double[,] bigCondProb = new double[numStates, numStates].Fill(1);
                    double[] bigNoTransVect = new double[numStates].Fill(1);
                    double[,] bigDeltaIndep = new double[numStates, numStates].Fill(1);
                    double[,] bigDeltaCond = new double[numStates, numStates].Fill(1);

                    for (int k = 0; k < dependencies.Length; k++)
                    {
                        if (dependencies[k].Type == CharacterDependency.Types.Independent)
                        {
                            Matrix<double> transMat = rateMatrix[k].FastExponential(model.BranchLengths[i], exps[k]).Result;

                            for (int j = 0; j < numStates; j++)
                            {
                                for (int l = 0; l < numStates; l++)
                                {
                                    bigTransMat[j, l] *= transMat[allPossibleStates[j][k], allPossibleStates[l][k]];
                                    bigDeltaIndep[j, l] *= (allPossibleStates[j][k] == allPossibleStates[l][k] ? 1 : 0);
                                }
                                bigNoTransVect[j] *= Math.Exp(rateMatrix[k][allPossibleStates[j][k], allPossibleStates[j][k]] * model.BranchLengths[i]);
                            }
                        }
                        else if (dependencies[k].Type == CharacterDependency.Types.Conditioned)
                        {
                            for (int j = 0; j < numStates; j++)
                            {
                                for (int l = 0; l < numStates; l++)
                                {
                                    bigCondProb[j, l] *= allPossibleStatesCondProbs[k][l];
                                    bigDeltaCond[j, l] *= (allPossibleStates[j][k] == allPossibleStates[l][k] ? 1 : 0);
                                }
                            }
                        }
                    }

                    Matrix<double> completeTransMat = Matrix<double>.Build.Dense(numStates, numStates);

                    for (int j = 0; j < numStates; j++)
                    {
                        for (int k = 0; k < numStates; k++)
                        {
                            completeTransMat[j, k] = bigNoTransVect[j] * bigDeltaIndep[j, k] * bigDeltaCond[j, k] + (bigTransMat[j, k] - bigNoTransVect[j] * bigDeltaIndep[j, k]) * bigCondProb[j, k];
                        }
                    }

                    if (i == 14)
                    {
                        for (int k = 0; k < numStates; k++)
                        {
                            double total = 0;

                            for (int j = 0; j < numStates; j++)
                            {
                                total += completeTransMat[k, j];
                            }
                        }
                    }

                    Vector<double> parentPrior = Vector<double>.Build.DenseOfArray(prior[model.Parents[i]]);
                    double[] childPrior = (parentPrior * completeTransMat).ToArray();

                    int sample = Categorical.Sample(randomSource, childPrior);
                    for (int j = 0; j < numStates; j++)
                    {
                        if (j != sample)
                        {
                            prior[i][j] = 0;
                        }
                        else
                        {
                            prior[i][j] = 1;
                        }
                    }
                }
            }

            return prior;
        }

        public static double[][] ComputeAndSamplePriors(LikelihoodModel model, string[] states, Dictionary<string, Parameter>[] pi, Dictionary<string, Parameter>[] rates, double[] parameters, CharacterDependency[] dependencies, Random randomSource)
        {
            if (dependencies.Length > 1)
            {
                throw new NotImplementedException();
            }


            (int bayesParameterCount, int mlParameterCount, List<List<Parameter>> ratesToEstimate, List<(double remainingPi, List<Parameter> pis, int[] equalCounts)> pisToEstimate) parametersToEstimate = Utils.GetParametersToEstimate(dependencies, rates, pi);

            int bayesParameterCount = parametersToEstimate.bayesParameterCount;
            int mlParameterCount = parametersToEstimate.mlParameterCount;
            List<List<Parameter>> ratesToEstimate = parametersToEstimate.ratesToEstimate;
            List<(double remainingPi, List<Parameter> pis, int[] equalCounts)> pisToEstimate = parametersToEstimate.pisToEstimate;


            int valInd = 0;

            for (int i = 0; i < ratesToEstimate.Count; i++)
            {
                for (int j = 0; j < ratesToEstimate[i].Count; j++)
                {
                    if (ratesToEstimate[i][j].Action == Parameter.ParameterAction.ML || ratesToEstimate[i][j].Action == Parameter.ParameterAction.Bayes)
                    {
                        ratesToEstimate[i][j].Value = parameters[valInd];
                        valInd++;
                    }
                }
            }

            for (int i = 0; i < pisToEstimate.Count; i++)
            {
                for (int j = 0; j < pisToEstimate[i].pis.Count; j++)
                {
                    if (pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.ML || pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.Dirichlet || pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.Multinomial)
                    {
                        pisToEstimate[i].pis[j].Value = parameters[valInd];
                        valInd++;
                    }
                }
            }

            double[][] tbr = new double[model.Parents.Length][];

            Matrix<double> rateMatrix = Matrix<double>.Build.Dense(states.Length, states.Length);

            for (int i = 0; i < states.Length; i++)
            {
                double sumI = 0;
                for (int j = 0; j < states.Length; j++)
                {
                    if (i != j)
                    {
                        rateMatrix[i, j] = rates[dependencies[0].Index][states[i] + ">" + states[j]];
                        sumI += rates[dependencies[0].Index][states[i] + ">" + states[j]];
                    }
                }
                rateMatrix[i, i] = -sumI;
            }

            MatrixExponential.MatrixExponential exp = rateMatrix.FastExponential(1);

            for (int i = model.Parents.Length - 1; i >= 0; i--)
            {
                tbr[i] = new double[states.Length];

                if (model.Parents[i] < 0)
                {
                    for (int j = 0; j < states.Length; j++)
                    {
                        tbr[i][j] = pi[dependencies[0].Index][states[j]].Value;
                    }

                    int sample = Categorical.Sample(randomSource, tbr[i]);
                    tbr[i] = new double[states.Length];
                    tbr[i][sample] = 1;
                }
                else
                {
                    Vector<double> parentPrior = Vector<double>.Build.DenseOfArray(tbr[model.Parents[i]]);
                    tbr[i] = (parentPrior * rateMatrix.FastExponential(model.BranchLengths[i], exp).Result).ToArray();

                    int sample = Categorical.Sample(randomSource, tbr[i]);
                    tbr[i] = new double[states.Length];
                    tbr[i][sample] = 1;
                }
            }

            return tbr;
        }



        public static double[][] ComputeAndSamplePosteriors(LikelihoodModel model, string[] states, Dictionary<string, Parameter>[] pi, Dictionary<string, Parameter>[] rates, double[] parameters, CharacterDependency[] dependencies, double[][] logLikelihoods, out double[] branchProbs, Random randomSource)
        {
            if (dependencies.Length > 1)
            {
                throw new NotImplementedException();
            }


            (int bayesParameterCount, int mlParameterCount, List<List<Parameter>> ratesToEstimate, List<(double remainingPi, List<Parameter> pis, int[] equalCounts)> pisToEstimate) parametersToEstimate = Utils.GetParametersToEstimate(dependencies, rates, pi);

            int bayesParameterCount = parametersToEstimate.bayesParameterCount;
            int mlParameterCount = parametersToEstimate.mlParameterCount;
            List<List<Parameter>> ratesToEstimate = parametersToEstimate.ratesToEstimate;
            List<(double remainingPi, List<Parameter> pis, int[] equalCounts)> pisToEstimate = parametersToEstimate.pisToEstimate;


            int valInd = 0;

            for (int i = 0; i < ratesToEstimate.Count; i++)
            {
                for (int j = 0; j < ratesToEstimate[i].Count; j++)
                {
                    if (ratesToEstimate[i][j].Action == Parameter.ParameterAction.ML || ratesToEstimate[i][j].Action == Parameter.ParameterAction.Bayes)
                    {
                        ratesToEstimate[i][j].Value = parameters[valInd];
                        valInd++;
                    }
                }
            }

            for (int i = 0; i < pisToEstimate.Count; i++)
            {
                for (int j = 0; j < pisToEstimate[i].pis.Count; j++)
                {
                    if (pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.ML || pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.Dirichlet || pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.Multinomial)
                    {
                        pisToEstimate[i].pis[j].Value = parameters[valInd];
                        valInd++;
                    }
                }
            }

            double[][] tbr = new double[model.Parents.Length][];

            Matrix<double> rateMatrix = Matrix<double>.Build.Dense(states.Length, states.Length);

            for (int i = 0; i < states.Length; i++)
            {
                double sumI = 0;
                for (int j = 0; j < states.Length; j++)
                {
                    if (i != j)
                    {
                        rateMatrix[i, j] = rates[dependencies[0].Index][states[i] + ">" + states[j]];
                        sumI += rates[dependencies[0].Index][states[i] + ">" + states[j]];
                    }
                }
                rateMatrix[i, i] = -sumI;
            }

            MatrixExponential.MatrixExponential exp = rateMatrix.FastExponential(1);

            branchProbs = new double[model.Parents.Length];

            for (int i = model.Parents.Length - 1; i >= 0; i--)
            {
                tbr[i] = new double[states.Length];

                if (model.Parents[i] < 0)
                {
                    double[] rootPriors = new double[states.Length];
                    for (int j = 0; j < states.Length; j++)
                    {
                        rootPriors[j] = pi[dependencies[0].Index][states[j]].Value;
                    }

                    double logMarginalLikelihood = Utils.LogSumExpTimes(logLikelihoods[i], rootPriors);

                    for (int j = 0; j < states.Length; j++)
                    {
                        tbr[i][j] = rootPriors[j] * Math.Exp(logLikelihoods[i][j] - logMarginalLikelihood);
                    }

                    int sample = Categorical.Sample(randomSource, tbr[i]);
                    branchProbs[i] = rootPriors[sample];
                    tbr[i] = new double[states.Length];
                    tbr[i][sample] = 1;
                }
                else
                {
                    Vector<double> parentPost = Vector<double>.Build.DenseOfArray(tbr[model.Parents[i]]);
                    double[] childPrior = (parentPost * rateMatrix.FastExponential(model.BranchLengths[i], exp).Result).ToArray();

                    double logMarginalLikelihood = Utils.LogSumExpTimes(logLikelihoods[i], childPrior);
                    for (int j = 0; j < states.Length; j++)
                    {
                        tbr[i][j] = childPrior[j] * Math.Exp(logLikelihoods[i][j] - logMarginalLikelihood);
                    }

                    int sample = Categorical.Sample(randomSource, tbr[i]);
                    branchProbs[i] = childPrior[sample];
                    tbr[i] = new double[states.Length];
                    tbr[i][sample] = 1;
                }
            }

            return tbr;
        }


        public static double[][] ComputeAndSampleJointPosteriors(LikelihoodModel model, string[][] states, Dictionary<string, Parameter>[] pi, Dictionary<string, Parameter>[] rates, double[] parameters, CharacterDependency[] dependencies, double[][] logLikelihoods, double[][] posteriors, bool reinitializePosteriors, out double[] branchProbs, Random randomSource)
        {
            (int bayesParameterCount, int mlParameterCount, List<List<Parameter>> ratesToEstimate, List<(double remainingPi, List<Parameter> pis, int[] equalCounts)> pisToEstimate) parametersToEstimate = Utils.GetParametersToEstimate(dependencies, rates, pi);

            int bayesParameterCount = parametersToEstimate.bayesParameterCount;
            int mlParameterCount = parametersToEstimate.mlParameterCount;
            List<List<Parameter>> ratesToEstimate = parametersToEstimate.ratesToEstimate;
            List<(double remainingPi, List<Parameter> pis, int[] equalCounts)> pisToEstimate = parametersToEstimate.pisToEstimate;

            int valInd = 0;

            for (int i = 0; i < ratesToEstimate.Count; i++)
            {
                for (int j = 0; j < ratesToEstimate[i].Count; j++)
                {
                    if (ratesToEstimate[i][j].Action == Parameter.ParameterAction.ML || ratesToEstimate[i][j].Action == Parameter.ParameterAction.Bayes)
                    {
                        ratesToEstimate[i][j].Value = parameters[valInd];
                        valInd++;
                    }
                }
            }

            for (int i = 0; i < pisToEstimate.Count; i++)
            {
                for (int j = 0; j < pisToEstimate[i].pis.Count; j++)
                {
                    if (pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.ML || pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.Dirichlet || pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.Multinomial)
                    {
                        pisToEstimate[i].pis[j].Value = parameters[valInd];
                        valInd++;
                    }
                }
            }

            if (reinitializePosteriors)
            {
                posteriors = new double[model.Parents.Length][];
            }

            Matrix<double>[] rateMatrix = new Matrix<double>[dependencies.Length];
            MatrixExponential.MatrixExponential[] exps = new MatrixExponential.MatrixExponential[dependencies.Length];

            for (int k = 0; k < dependencies.Length; k++)
            {
                rateMatrix[k] = Matrix<double>.Build.Dense(states[dependencies[k].Index].Length, states[dependencies[k].Index].Length);

                for (int i = 0; i < states[dependencies[k].Index].Length; i++)
                {
                    double sumI = 0;
                    for (int j = 0; j < states[dependencies[k].Index].Length; j++)
                    {
                        if (i != j)
                        {
                            rateMatrix[k][i, j] = rates[dependencies[k].Index][states[dependencies[k].Index][i] + ">" + states[dependencies[k].Index][j]];
                            sumI += rates[dependencies[k].Index][states[dependencies[k].Index][i] + ">" + states[dependencies[k].Index][j]];
                        }
                    }
                    rateMatrix[k][i, i] = -sumI;
                }

                exps[k] = rateMatrix[k].FastExponential(1);
            }

            int numStates = 1;
            List<int[]> statesList = new List<int[]>();        //states[character][i]

            for (int i = 0; i < dependencies.Length; i++)
            {
                switch (dependencies[i].Type)
                {
                    case CharacterDependency.Types.Independent:
                    case CharacterDependency.Types.Conditioned:
                        numStates *= states[dependencies[i].Index].Length;
                        statesList.Add(Utils.Range(0, states[dependencies[i].Index].Length));
                        break;
                    case CharacterDependency.Types.Dependent:
                        throw new NotImplementedException();
                }

            }

            int[][] statesInts = statesList.ToArray();

            int[][] allPossibleStates = Utils.GetCombinations(statesInts);        //allPossibleStates[combinationIndex][i]

            double[][] allPossibleStatesCondProbs = new double[dependencies.Length][];


            for (int k = 0; k < dependencies.Length; k++)
            {
                if (dependencies[k].Type == CharacterDependency.Types.Conditioned)
                {
                    allPossibleStatesCondProbs[k] = new double[allPossibleStates.Length];
                    for (int j = 0; j < allPossibleStates.Length; j++)
                    {
                        allPossibleStatesCondProbs[k][j] = dependencies[k].ConditionedProbabilities[Utils.StringifyArray(from el in dependencies[k].Dependencies select states[el][allPossibleStates[j][el]]) + ">" + states[dependencies[k].Index][allPossibleStates[j][k]]];
                    }
                }
            }

            branchProbs = new double[model.Parents.Length];

            for (int i = model.Parents.Length - 1; i >= 0; i--)
            {
                if (reinitializePosteriors)
                {
                    posteriors[i] = new double[numStates];
                }

                if (model.Parents[i] < 0)
                {
                    double[] rootPriors = new double[numStates];

                    for (int j = 0; j < numStates; j++)
                    {
                        rootPriors[j] = 1;

                        for (int k = 0; k < dependencies.Length; k++)
                        {
                            switch (dependencies[k].Type)
                            {
                                case CharacterDependency.Types.Independent:
                                    rootPriors[j] *= pi[dependencies[k].Index][states[dependencies[k].Index][allPossibleStates[j][k]]].Value;
                                    break;
                                case CharacterDependency.Types.Conditioned:
                                    rootPriors[j] *= allPossibleStatesCondProbs[k][j];
                                    break;
                            }
                        }
                    }

                    double logMarginalLikelihood = Utils.LogSumExpTimes(logLikelihoods[i], rootPriors);

                    for (int j = 0; j < numStates; j++)
                    {
                        posteriors[i][j] = rootPriors[j] * Math.Exp(logLikelihoods[i][j] - logMarginalLikelihood);
                    }

                    int sample = Categorical.Sample(randomSource, posteriors[i]);
                    branchProbs[i] = rootPriors[sample];

                    for (int j = 0; j < numStates; j++)
                    {
                        if (j != sample)
                        {
                            posteriors[i][j] = 0;
                        }
                        else
                        {
                            posteriors[i][j] = 1;
                        }
                    }
                }
                else
                {
                    double[,] bigTransMat = new double[numStates, numStates].Fill(1);
                    double[,] bigCondProb = new double[numStates, numStates].Fill(1);
                    double[] bigNoTransVect = new double[numStates].Fill(1);
                    double[,] bigDeltaIndep = new double[numStates, numStates].Fill(1);
                    double[,] bigDeltaCond = new double[numStates, numStates].Fill(1);

                    for (int k = 0; k < dependencies.Length; k++)
                    {
                        if (dependencies[k].Type == CharacterDependency.Types.Independent)
                        {
                            Matrix<double> transMat = rateMatrix[k].FastExponential(model.BranchLengths[i], exps[k]).Result;

                            for (int j = 0; j < numStates; j++)
                            {
                                for (int l = 0; l < numStates; l++)
                                {
                                    bigTransMat[j, l] *= transMat[allPossibleStates[j][k], allPossibleStates[l][k]];
                                    bigDeltaIndep[j, l] *= (allPossibleStates[j][k] == allPossibleStates[l][k] ? 1 : 0);
                                }
                                bigNoTransVect[j] *= Math.Exp(rateMatrix[k][allPossibleStates[j][k], allPossibleStates[j][k]] * model.BranchLengths[i]);
                            }
                        }
                        else if (dependencies[k].Type == CharacterDependency.Types.Conditioned)
                        {
                            for (int j = 0; j < numStates; j++)
                            {
                                for (int l = 0; l < numStates; l++)
                                {
                                    bigCondProb[j, l] *= allPossibleStatesCondProbs[k][l];
                                    bigDeltaCond[j, l] *= (allPossibleStates[j][k] == allPossibleStates[l][k] ? 1 : 0);
                                }
                            }
                        }
                    }

                    Matrix<double> completeTransMat = Matrix<double>.Build.Dense(numStates, numStates);

                    for (int j = 0; j < numStates; j++)
                    {
                        for (int k = 0; k < numStates; k++)
                        {
                            completeTransMat[j, k] = bigNoTransVect[j] * bigDeltaIndep[j, k] * bigDeltaCond[j, k] + (bigTransMat[j, k] - bigNoTransVect[j] * bigDeltaIndep[j, k]) * bigCondProb[j, k];
                        }
                    }

                    Vector<double> parentPost = Vector<double>.Build.DenseOfArray(posteriors[model.Parents[i]]);
                    double[] childPrior = (parentPost * completeTransMat).ToArray();

                    double logMarginalLikelihood = Utils.LogSumExpTimes(logLikelihoods[i], childPrior);

                    for (int j = 0; j < numStates; j++)
                    {
                        posteriors[i][j] = childPrior[j] * Math.Exp(logLikelihoods[i][j] - logMarginalLikelihood);
                    }
                    int sample = Categorical.Sample(randomSource, posteriors[i]);

                    branchProbs[i] = childPrior[sample];

                    for (int j = 0; j < numStates; j++)
                    {
                        if (j != sample)
                        {
                            posteriors[i][j] = 0;
                        }
                        else
                        {
                            posteriors[i][j] = 1;
                        }
                    }
                }
            }

            return posteriors;
        }
    }
}
