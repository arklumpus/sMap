using System;
using System.Collections.Generic;
using static Utils.MCMC;

namespace Utils
{
    internal class Convergence
    {
        public static (double maxCoVMean, double maxCoVStdDev, double minESS, double[] minEffSizes) ComputeConvergenceStats(int numRuns, MCMCVariable[][] variables, List<(int, double[][])>[] variableSamples, int diagnosticCount, bool finalESS)
        {
            double[][][] means = new double[variables[0].Length][][];
            for (int j = 0; j < variables[0].Length; j++)
            {
                means[j] = new double[variables[0][j].Length][];
                for (int i = 0; i < means[j].Length; i++)
                {
                    means[j][i] = new double[numRuns];
                }
            }

            double maxCoVMean = double.MinValue;
            double maxCoVStdDev = double.MinValue;

            for (int j = 0; j < variables[0].Length; j++)
            {
                if (variables[0][j].Type == MCMCVariable.VariableType.Uni || ((MCMCMultiVariable)variables[0][j]).ProposalType != MCMCMultiVariable.ProposalTypes.Multinomial)
                {
                    for (int k = 0; k < variables[0][j].Length; k++)
                    {
                        double ex = 0;
                        double exSq = 0;

                        double exStdDev = 0;
                        double exVar = 0;

                        for (int i = 0; i < numRuns; i++)
                        {
                            double singleEx = 0;
                            double singleExSq = 0;
                            for (int l = variableSamples[i].Count / 10; l < variableSamples[i].Count; l++)
                            {
                                singleEx += variableSamples[i][l].Item2[j][k];
                                singleExSq += variableSamples[i][l].Item2[j][k] * variableSamples[i][l].Item2[j][k];
                            }
                            singleEx /= variableSamples[i].Count - variableSamples[i].Count / 10;
                            singleExSq /= variableSamples[i].Count - variableSamples[i].Count / 10;
                            means[j][k][i] = singleEx;
                            ex += singleEx;
                            exSq += singleEx * singleEx;

                            double singleVariance = singleExSq - singleEx * singleEx;
                            exStdDev += Math.Sqrt(singleVariance);
                            exVar += singleVariance;
                        }

                        ex /= numRuns;
                        exSq /= numRuns;

                        exStdDev /= numRuns;
                        exVar /= numRuns;

                        double covMean = Math.Sqrt(exSq - ex * ex) / ex;
                        double covStdDev = Math.Sqrt(exVar - exStdDev * exStdDev) / exStdDev;

                        maxCoVMean = Utils.Max(covMean, maxCoVMean);
                        maxCoVStdDev = Utils.Max(covStdDev, maxCoVStdDev);
                    }
                }
            }

            double[] minEffSizes = null;

            double minESS = -1;

            if (finalESS)
            {
                minEffSizes = new double[numRuns];
                for (int i = 0; i < numRuns; i++)
                {
                    minEffSizes[i] = double.MaxValue;
                }
            }

            if (finalESS || (maxCoVMean < convergenceCoVThreshold && maxCoVStdDev < convergenceCoVThreshold && (diagnosticCount - diagnosticCount / 10) * diagnosticFrequency >= minSamples * sampleFrequency))
            {
                minESS = double.MaxValue;

                for (int j = 0; j < variables[0].Length; j++)
                {
                    for (int k = 0; k < variables[0][j].Length; k++)
                    {
                        if (variables[0][j].Type == MCMCVariable.VariableType.Uni || ((MCMCMultiVariable)variables[0][j]).ProposalType != MCMCMultiVariable.ProposalTypes.Multinomial)
                        {
                            for (int i = 0; i < numRuns; i++)
                            {
                                double ess = Utils.computeESS(variableSamples, means[j][k][i], i, j, k, variableSamples[i].Count / 10);
                                minESS = Math.Min(minESS, ess);
                                
                                if (finalESS)
                                {
                                    minEffSizes[i] = Math.Min(minEffSizes[i], ess);
                                }
                            }
                        }
                    }
                }
            }

            if (finalESS)
            {
                for (int i = 0; i < numRuns; i++)
                {
                    if (minEffSizes[i] == double.MaxValue)
                    {
                        minEffSizes[i] = 200;
                    }
                }
            }

            return (maxCoVMean, maxCoVStdDev, minESS, minEffSizes);
        }
    }
}
