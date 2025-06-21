using MathNet.Numerics.Distributions;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using static Utils.MCMC;

namespace Utils
{
    public class Convergence
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

        // RHat, bulk ESS and tail ESS as defined by Vehtari et al, 2021 (https://doi.org/10.1214/20-BA1221) and implemented in the R package "posterior".
        // See also https://mc-stan.org/docs/reference-manual/analysis.html
        public static (double maxRHat, double minBulkESS, double minTailESS, double[] minEffSizes) ComputeConvergenceStats(int numRuns, List<(int, double[][])>[] variableSamples, bool finalESS)
        {
            int sampleCount = variableSamples.Select(x => x.Count).Min();

            double maxRHat = double.MinValue;
            double minBulkESS = double.MaxValue;
            double minTailESS = double.MaxValue;
            double[] minEffSizes = null;

            for (int j = 0; j < variableSamples[0][0].Item2.Length; j++)
            {
                for (int k = 0; k < variableSamples[0][0].Item2[j].Length; k++)
                {
                    double[][] sample = new double[numRuns][];

                    for (int i = 0; i < numRuns; i++)
                    {
                        sample[i] = new double[sampleCount];

                        for (int l = 0; l < sampleCount; l++)
                        {
                            sample[i][l] = variableSamples[i][l].Item2[j][k];
                        }
                    }

                    double[][] rankNormalisedData = SplitData(RankNormaliseData(sample));
                    double rHat = ComputeRhat(rankNormalisedData);
                    rHat = Math.Max(rHat, ComputeRankNormalisedFoldedSplitRhat(sample));
                    maxRHat = Math.Max(maxRHat, rHat);

                    if (maxRHat < MCMC.convergenceRHatThreshold)
                    {
                        double bulkESS = ComputeESS(rankNormalisedData);
                        double tailESS = ComputeTailESS(sample);
                        minTailESS = Math.Min(minTailESS, tailESS);
                        minBulkESS = Math.Min(minBulkESS, bulkESS);
                    }
                    else
                    {
                        minTailESS = -1;
                        minBulkESS = -1;
                    }
                }
            }

            if (finalESS)
            {
                minEffSizes = new double[numRuns];

                for (int i = 0; i < numRuns; i++)
                {
                    minEffSizes[i] = double.MaxValue;

                    for (int j = 0; j < variableSamples[0][0].Item2.Length; j++)
                    {
                        for (int k = 0; k < variableSamples[0][0].Item2[j].Length; k++)
                        {
                            double[] sample = new double[sampleCount];

                            for (int l = 0; l < sampleCount; l++)
                            {
                                sample[l] = variableSamples[i][l].Item2[j][k];
                            }

                            double[][] rankNormalisedData = SplitData(RankNormaliseData(new double[][] { sample }));
                            double bulkESS = ComputeESS(rankNormalisedData);
                            double tailESS = ComputeTailESS(new double[][] { sample });
                            minEffSizes[i] = Math.Min(minEffSizes[i], Math.Min(bulkESS, tailESS));
                        }
                    }

                    if (minEffSizes[i] == double.MaxValue)
                    {
                        minEffSizes[i] = 200;
                    }
                }
            }

            return (maxRHat, minBulkESS, minTailESS, minEffSizes);
        }

        public static (double rHat, double bulkESS, double tailESS) ComputeConvergenceStats(double[][] sample)
        {
            double[][] rankNormalisedData = SplitData(RankNormaliseData(sample));
            double rHat = ComputeRhat(rankNormalisedData);
            rHat = Math.Max(rHat, ComputeRankNormalisedFoldedSplitRhat(sample));
            double bulkESS = ComputeESS(rankNormalisedData);
            double tailESS = ComputeTailESS(sample);

            return (rHat, bulkESS, tailESS);
        }

        static double ComputeRankNormalisedFoldedSplitRhat(double[][] data)
        {
            double[][] foldedRankNormalisedData = FoldAndRankNormaliseData(data);

            return ComputeSplitRhat(foldedRankNormalisedData);
        }

        static double[][] FoldAndRankNormaliseData(double[][] data)
        {
            int numChains = data.Length;
            int numSamples = data.Select(x => x.Length).Min();

            double[] allSamples = new double[numChains * numSamples];

            for (int i = 0; i < numChains; i++)
            {
                for (int j = 0; j < numSamples; j++)
                {
                    allSamples[i * numSamples + j] = data[i][j];
                }
            }

            double median = allSamples.Median();

            for (int i = 0; i < allSamples.Length; i++)
            {
                allSamples[i] = Math.Abs(allSamples[i] - median);
            }

            double[] ranks = allSamples.Ranks();

            double[][] normalisedData = new double[data.Length][];

            for (int i = 0; i < data.Length; i++)
            {
                normalisedData[i] = new double[numSamples];

                for (int j = 0; j < numSamples; j++)
                {
                    normalisedData[i][j] = Normal.InvCDF(0, 1, (ranks[i * numSamples + j] - 0.375) / (numChains * numSamples + 0.25));
                }
            }
            return normalisedData;
        }

        static double[][] RankNormaliseData(double[][] data)
        {
            int numChains = data.Length;
            int numSamples = data.Select(x => x.Length).Min();

            double[] allSamples = new double[numChains * numSamples];

            for (int i = 0; i < numChains; i++)
            {
                for (int j = 0; j < numSamples; j++)
                {
                    allSamples[i * numSamples + j] = data[i][j];
                }
            }

            double[] ranks = allSamples.Ranks();

            double[][] normalisedData = new double[data.Length][];

            for (int i = 0; i < data.Length; i++)
            {
                normalisedData[i] = new double[numSamples];

                for (int j = 0; j < numSamples; j++)
                {
                    normalisedData[i][j] = Normal.InvCDF(0, 1, (ranks[i * numSamples + j] - 0.375) / (numChains * numSamples + 0.25));
                }
            }

            return normalisedData;
        }

        static double ComputeSplitRhat(double[][] data)
        {
            return ComputeRhat(SplitData(data));
        }

        static double[][] SplitData(double[][] data)
        {
            int numChains = data.Length;
            int numSamples = data.Select(x => x.Length).Min() / 2;

            double[][] splitData = new double[numChains * 2][];

            for (int i = 0; i < numChains; i++)
            {
                splitData[i * 2] = new double[numSamples];
                splitData[i * 2 + 1] = new double[numSamples];
                for (int j = 0; j < numSamples; j++)
                {
                    splitData[i * 2][j] = data[i][j];
                    splitData[i * 2 + 1][j] = data[i][data[i].Length - numSamples + j];
                }
            }

            return splitData;
        }

        static double ComputeRhat(double[][] data)
        {
            int numChains = data.Length;
            int numSamples = data.Select(x => x.Length).Min();

            (double mean, double variance)[] means = data.Select(x => x.MeanVariance().ToValueTuple()).ToArray();

            (double mean, double variance) between = means.Select(x => x.mean).MeanVariance().ToValueTuple();

            double w = means.Select(x => x.variance).Average();

            double Var = (numSamples - 1) * w / numSamples + between.variance;

            double rhat = Math.Sqrt(Var / w);

            return rhat;
        }

        static double ComputeTailESS(double[][] data)
        {
            return ComputeQuantileESS(data, new double[] { 0.05, 0.95 }).Min();
        }

        static double ComputeSplitESS(double[][] data)
        {
            return ComputeESS(SplitData(data));
        }

        static IEnumerable<double> ComputeQuantileESS(double[][] data, IEnumerable<double> quantiles)
        {
            int numChains = data.Length;
            int numSamples = data.Select(x => x.Length).Min();

            double[] allSamples = new double[numChains * numSamples];

            for (int i = 0; i < numChains; i++)
            {
                for (int j = 0; j < numSamples; j++)
                {
                    allSamples[i * numSamples + j] = data[i][j];
                }
            }

            Func<double, double> quantileFunc = allSamples.QuantileFunc();

            double[][] quantData = new double[numChains][];

            foreach (double quantile in quantiles)
            {
                double threshold = quantileFunc(quantile);

                for (int i = 0; i < numChains; i++)
                {
                    quantData[i] = new double[numSamples];

                    for (int j = 0; j < numSamples; j++)
                    {
                        quantData[i][j] = data[i][j] <= threshold ? 1 : 0;
                    }
                }

                yield return ComputeSplitESS(quantData);
            }
        }

        static double ComputeESS(double[][] data)
        {
            int numChains = data.Length;
            int numSamples = data.Select(x => x.Length).Min();

            if (numSamples < 3)
            {
                return 1;
            }

            double[][] autocorr = new double[numChains][];

            for (int i = 0; i < numChains; i++)
            {
                autocorr[i] = Autocovariance(data[i]);
            }

            double[] autocorrMeans = new double[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                for (int j = 0; j < numChains; j++)
                {
                    autocorrMeans[i] += autocorr[j][i];
                }

                autocorrMeans[i] /= numChains;
            }

            double meanVar = autocorrMeans[0] * numSamples / (numSamples - 1);
            double varPlus = autocorrMeans[0];

            if (numChains > 1)
            {
                double[] means = data.Select(x => x.Average()).ToArray();

                varPlus += data.Select(x => x.Average()).Variance();
            }

            double[] rhoHatT = new double[numSamples];

            int t = 0;
            double rhoHatEven = 1;
            rhoHatT[t] = rhoHatEven;
            double rhoHatOdd = 1 - (meanVar - autocorrMeans[t + 1]) / varPlus;
            rhoHatT[t + 1] = rhoHatOdd;

            while (t + 2 < numSamples - 1 && rhoHatEven + rhoHatOdd > 0)
            {
                t += 2;

                rhoHatEven = 1 - (meanVar - autocorrMeans[t]) / varPlus;
                rhoHatOdd = 1 - (meanVar - autocorrMeans[t + 1]) / varPlus;

                if (rhoHatEven + rhoHatOdd >= 0)
                {
                    rhoHatT[t] = rhoHatEven;
                    rhoHatT[t + 1] = rhoHatOdd;
                }
            }

            int maxT = t;

            if (rhoHatEven > 0)
            {
                rhoHatT[maxT] = rhoHatEven;
            }

            for (int i = 2; i <= maxT - 4; i += 2)
            {
                if (rhoHatT[i] + rhoHatT[i + 1] > rhoHatT[i - 2] + rhoHatT[i - 1])
                {
                    rhoHatT[i] = (rhoHatT[i - 2] + rhoHatT[i - 1]) * 0.5;
                    rhoHatT[i + 1] = rhoHatT[i];
                }
            }

            double ess = numChains * numSamples;

            double tauHat = -1 + 2 * rhoHatT.Take(maxT).Sum() + rhoHatT[maxT];

            tauHat = Math.Max(tauHat, 1.0 / Math.Log10(ess));

            return ess / tauHat;
        }

        static double[] Autocovariance(double[] data)
        {
            (double mean, double variance) = data.MeanVariance();

            if (variance == 0)
            {
                return new double[data.Length];
            }

            int m2 = NextN(data.Length) * 2;

            double[] centeredData = new double[m2];

            for (int i = 0; i < data.Length; i++)
            {
                centeredData[i] = data[i] - mean;
            }

            double[] imaginary = new double[centeredData.Length];

            Fourier.Forward(centeredData, imaginary, FourierOptions.NumericalRecipes);

            for (int i = 0; i < centeredData.Length; i++)
            {
                centeredData[i] = centeredData[i] * centeredData[i] + imaginary[i] * imaginary[i];
                imaginary[i] = 0;
            }

            Fourier.Inverse(centeredData, imaginary, FourierOptions.NumericalRecipes);

            double normFactor = variance * (double)(data.Length - 1) / data.Length / centeredData[0];

            double[] tbr = new double[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                tbr[i] = centeredData[i] * normFactor;
            }

            return tbr;
        }

        static int NextN(int n)
        {
            while (!OkN(n))
            {
                n++;
            }

            return n;
        }

        static bool OkN(int n)
        {
            while (n % 2 == 0)
            {
                n /= 2;
            }

            while (n % 3 == 0)
            {
                n /= 3;
            }

            while (n % 5 == 0)
            {
                n /= 5;
            }

            return n == 1;
        }
    }
}
