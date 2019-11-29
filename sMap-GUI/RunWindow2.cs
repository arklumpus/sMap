using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils;

namespace sMap_GUI
{
    public partial class RunWindow : Window
    {
        List<int[]> bayesianSampleCounts;

        List<double[]> bayesianMeanCoVs;
        List<double[]> bayesianSdCoVs;

        List<double[][]> bayesianEssss;
        List<double[][]> bayesianMeanss;
        List<double[][]> bayesianStdDevss;

        List<int[][]> bayesianBinCountss;
        List<int[][][]> bayesianBinSampless;
        List<double[][]> bayesianRealBinWidthss;
        List<double[][]> bayesianMaxBinss;

        List<List<double[]>> bayesianConvergenceStats;



        List<List<int[]>> steppingStoneSampleCounts;

        List<List<double[]>> steppingStoneMeanCoVs;
        List<List<double[]>> steppingStoneSdCoVs;

        List<List<double[][]>> steppingStoneEssss;
        List<List<double[][]>> steppingStoneMeanss;
        List<List<double[][]>> steppingStoneStdDevss;

        List<List<int[][]>> steppingStoneBinCountss;
        List<List<int[][][]>> steppingStoneBinSampless;
        List<List<double[][]>> steppingStoneRealBinWidthss;
        List<List<double[][]>> steppingStoneMaxBinss;

        List<List<List<double[]>>> steppingStoneConvergenceStats;
        List<List<List<double[]>>> steppingStoneLikelihoodConvergenceStats;

        void UpdateBayesianPlotCache()
        {
            int index;

            double[][][] allValues;

            int[] sampleCounts;

            lock (plotObject)
            {
                index = samples.Count - 1;

                sampleCounts = (from el in samples[index] select el.Count).ToArray();
                allValues = new double[realParamNames[index].Count][][];
                for (int selIndex = 1; selIndex - 1 < realParamNames[index].Count; selIndex++)
                {
                    int[] paramRealIndex = Utils.Utils.GetRealIndex(selIndex - 1, realParamNames[index]);

                    if (selIndex > 1)
                    {
                        allValues[selIndex - 1] = (from el2 in samples[index] select (from el in el2 select el.Item2[paramRealIndex[0] - 1][paramRealIndex[1]]).ToArray()).ToArray();
                    }
                    else
                    {
                        allValues[selIndex - 1] = (from el2 in samples[index] select (from el in el2 select (double)el.Item1).ToArray()).ToArray();
                    }
                }
            }

            double[] meanCoVs = new double[realParamNames[index].Count];
            double[] sdCoVs = new double[realParamNames[index].Count];

            double[][] essss = new double[realParamNames[index].Count][];
            double[][] meanss = new double[realParamNames[index].Count][];
            double[][] stdDevss = new double[realParamNames[index].Count][];

            int[][] binCountss = new int[realParamNames[index].Count][];
            int[][][] binSampless = new int[realParamNames[index].Count][][];
            double[][] realBinWidthss = new double[realParamNames[index].Count][];
            double[][] maxBinss = new double[realParamNames[index].Count][];

            for (int selIndex = 1; selIndex - 1 < realParamNames[index].Count; selIndex++)
            {
                int[] paramRealIndex = Utils.Utils.GetRealIndex(selIndex - 1, realParamNames[index]);

                double[][] values = allValues[selIndex - 1];

                double min;
                double max;

                if (selIndex > 1)
                {
                    min = (from el in values select el.Min()).Min();
                    max = (from el in values select el.Max()).Max();
                }
                else
                {
                    min = 0;
                    max = totalTrees;
                }

                double[] means = (from el in values select el.Skip(el.Length / 10).Average()).ToArray();
                double[] stdDevs = (from el2 in Utils.Utils.Range(0, values.Length) select Math.Sqrt((from el in values[el2].Skip(values[el2].Length / 10) select el * el).Average() - means[el2] * means[el2])).ToArray();

                meanss[selIndex - 1] = means;
                stdDevss[selIndex - 1] = stdDevs;

                double meanMean = means.Average();
                double meanStdDev = Math.Sqrt((from el in means select el * el).Average() - meanMean * meanMean);
                double meanCoV = meanStdDev / meanMean;

                meanCoVs[selIndex - 1] = meanCoV;

                double stdDevMean = stdDevs.Average();
                double stdDevStdDev = Math.Sqrt((from el in stdDevs select el * el).Average() - stdDevMean * stdDevMean);
                double stdDevCoV = stdDevStdDev / stdDevMean;

                sdCoVs[selIndex - 1] = stdDevCoV;

                double[] esss = (from el in Utils.Utils.Range(0, values.Length) select Utils.Utils.computeESS(values[el], means[el], values[el].Length / 10)).ToArray();
                essss[selIndex - 1] = esss;

                int[][] binSamples = new int[samples[index].Length][];
                int[] binCounts = new int[samples[index].Length];
                double[] maxBins = new double[samples[index].Length];
                double[] realBinWidths = new double[samples[index].Length];

                for (int runInd = 0; runInd < samples[index].Length; runInd++)
                {
                    double iqr = values[runInd].IQR();
                    double h2 = 2 * iqr / Math.Pow(values[runInd].Length, 1.0 / 3.0);
                    int binCount = (int)Math.Ceiling((max - min) / h2);
                    double binWidth = (max - min) / binCount;

                    binCounts[runInd] = binCount;

                    if (binCount > 0)
                    {

                        binSamples[runInd] = new int[binCount];

                        for (int i = 0; i < binCount; i++)
                        {
                            binSamples[runInd][i] = (from el in values[runInd] where el > min + i * binWidth && el <= min + (i + 1) * binWidth select 1).Count();
                        }

                        maxBins[runInd] = binSamples[runInd].Max();

                        realBinWidths[runInd] = binWidth / (max - min) * 575;
                    }
                }

                if (maxBins.Max() > 0)
                {
                    int maxBinInd = (from el in Utils.Utils.Range(0, maxBins.Length) select -((double)sampleCounts[el] - 0.5 * ((double)binSamples[el].First() + (double)binSamples[el].Last())) / ((double)binCounts[el] * maxBins[el])).ToArray().MaxInd();

                    for (int runInd = 0; runInd < samples[index].Length; runInd++)
                    {
                        maxBins[runInd] /= (double)binCounts[runInd] / (double)binCounts[maxBinInd] * ((double)sampleCounts[maxBinInd] - 0.5 * ((double)binSamples[maxBinInd].First() + (double)binSamples[maxBinInd].Last())) / ((double)sampleCounts[runInd] - 0.5 * ((double)binSamples[runInd].First() + (double)binSamples[runInd].Last())) * maxBins[runInd] / maxBins[maxBinInd];
                    }
                }

                binSampless[selIndex - 1] = binSamples;
                binCountss[selIndex - 1] = binCounts;
                maxBinss[selIndex - 1] = maxBins;
                realBinWidthss[selIndex - 1] = realBinWidths;

            }

            lock (plotObject)
            {
                bayesianBinCountss[index] = binCountss;
                bayesianBinSampless[index] = binSampless;
                bayesianEssss[index] = essss;
                bayesianMaxBinss[index] = maxBinss;
                bayesianMeanCoVs[index] = meanCoVs;
                bayesianMeanss[index] = meanss;
                bayesianRealBinWidthss[index] = realBinWidthss;
                bayesianSdCoVs[index] = sdCoVs;
                bayesianStdDevss[index] = stdDevss;
                bayesianSampleCounts[index] = sampleCounts;

                bayesianConvergenceStats[index].Add(new double[] { sampleCounts.Average(), meanCoVs.Max(), sdCoVs.Max(), (from el in essss.Skip(1) select el.Min()).Min() });
            }
        }


        void UpdateSteppingStonePlotCache()
        {
            int index;
            int stepIndex;

            double[][][] allValues;

            int[] sampleCounts;

            lock (plotObject)
            {
                index = steppingStoneSamples.Count - 1;

                stepIndex = steppingStoneStepItems.Last().Count - 1;

                sampleCounts = (from el in steppingStoneSamples[index][stepIndex] select el.Count).ToArray();

                allValues = new double[realParamNames[index].Count + 1][][];

                allValues[0] = (from el2 in steppingStoneSamples[index][stepIndex] select (from el in el2 select el.Item2[0][0]).ToArray()).ToArray();

                for (int selIndex = 1; selIndex - 1 < realParamNames[index].Count; selIndex++)
                {
                    int[] paramRealIndex = Utils.Utils.GetRealIndex(selIndex - 1, realParamNames[index]);

                    if (selIndex > 1)
                    {
                        allValues[selIndex] = (from el2 in steppingStoneSamples[index][stepIndex] select (from el in el2 select el.Item2[paramRealIndex[0]][paramRealIndex[1]]).ToArray()).ToArray();
                    }
                    else
                    {
                        allValues[selIndex] = (from el2 in steppingStoneSamples[index][stepIndex] select (from el in el2 select (double)el.Item1).ToArray()).ToArray();
                    }
                }
            }

            double[] meanCoVs = new double[realParamNames[index].Count + 1];
            double[] sdCoVs = new double[realParamNames[index].Count + 1];

            double[][] essss = new double[realParamNames[index].Count + 1][];
            double[][] meanss = new double[realParamNames[index].Count + 1][];
            double[][] stdDevss = new double[realParamNames[index].Count + 1][];

            int[][] binCountss = new int[realParamNames[index].Count + 1][];
            int[][][] binSampless = new int[realParamNames[index].Count + 1][][];
            double[][] realBinWidthss = new double[realParamNames[index].Count + 1][];
            double[][] maxBinss = new double[realParamNames[index].Count + 1][];

            for (int selIndex = 1; selIndex - 1 < realParamNames[index].Count + 1; selIndex++)
            {
                int[] paramRealIndex;

                if (selIndex == 1)
                {
                    paramRealIndex = new int[] { 0, 0 };
                }
                else
                {
                    paramRealIndex = Utils.Utils.GetRealIndex(selIndex - 2, realParamNames[index]);
                }

                double[][] values = allValues[selIndex - 1];

                double min;
                double max;

                if (selIndex != 2)
                {
                    min = (from el in values select el.Min()).Min();
                    max = (from el in values select el.Max()).Max();
                }
                else
                {
                    min = 0;
                    max = totalTrees;
                }

                double[] means = (from el in values select el.Skip(el.Length / 10).Average()).ToArray();
                double[] stdDevs = (from el2 in Utils.Utils.Range(0, values.Length) select Math.Sqrt((from el in values[el2].Skip(values[el2].Length / 10) select el * el).Average() - means[el2] * means[el2])).ToArray();

                meanss[selIndex - 1] = means;
                stdDevss[selIndex - 1] = stdDevs;

                double meanMean = means.Average();
                double meanStdDev = Math.Sqrt((from el in means select el * el).Average() - meanMean * meanMean);
                double meanCoV = Math.Abs(meanStdDev / meanMean);

                meanCoVs[selIndex - 1] = meanCoV;

                double stdDevMean = stdDevs.Average();
                double stdDevStdDev = Math.Sqrt((from el in stdDevs select el * el).Average() - stdDevMean * stdDevMean);
                double stdDevCoV = stdDevStdDev / stdDevMean;

                sdCoVs[selIndex - 1] = stdDevCoV;

                double[] esss = (from el in Utils.Utils.Range(0, values.Length) select Utils.Utils.computeESS(values[el], means[el], values[el].Length / 10)).ToArray();
                essss[selIndex - 1] = esss;

                int[][] binSamples = new int[steppingStoneSamples[index][stepIndex].Length][];
                int[] binCounts = new int[steppingStoneSamples[index][stepIndex].Length];
                double[] maxBins = new double[steppingStoneSamples[index][stepIndex].Length];
                double[] realBinWidths = new double[steppingStoneSamples[index][stepIndex].Length];

                for (int runInd = 0; runInd < steppingStoneSamples[index][stepIndex].Length; runInd++)
                {
                    double iqr = values[runInd].IQR();
                    double h2 = 2 * iqr / Math.Pow(values[runInd].Length, 1.0 / 3.0);
                    int binCount = (int)Math.Ceiling((max - min) / h2);
                    double binWidth = (max - min) / binCount;

                    binCounts[runInd] = binCount;

                    if (binCount > 0)
                    {

                        binSamples[runInd] = new int[binCount];

                        for (int i = 0; i < binCount; i++)
                        {
                            binSamples[runInd][i] = (from el in values[runInd] where el > min + i * binWidth && el <= min + (i + 1) * binWidth select 1).Count();
                        }

                        maxBins[runInd] = binSamples[runInd].Max();

                        realBinWidths[runInd] = binWidth / (max - min) * 575;
                    }
                }

                if (binCounts.Min() > 0)
                {
                    int maxBinInd = (from el in Utils.Utils.Range(0, maxBins.Length) select -((double)sampleCounts[el] - 0.5 * ((double)binSamples[el].First() + (double)binSamples[el].Last())) / ((double)binCounts[el] * maxBins[el])).ToArray().MaxInd();

                    for (int runInd = 0; runInd < steppingStoneSamples[index][stepIndex].Length; runInd++)
                    {
                        maxBins[runInd] /= (double)binCounts[runInd] / (double)binCounts[maxBinInd] * ((double)sampleCounts[maxBinInd] - 0.5 * ((double)binSamples[maxBinInd].First() + (double)binSamples[maxBinInd].Last())) / ((double)sampleCounts[runInd] - 0.5 * ((double)binSamples[runInd].First() + (double)binSamples[runInd].Last())) * maxBins[runInd] / maxBins[maxBinInd];
                    }
                }

                binSampless[selIndex - 1] = binSamples;
                binCountss[selIndex - 1] = binCounts;
                maxBinss[selIndex - 1] = maxBins;
                realBinWidthss[selIndex - 1] = realBinWidths;

            }

            lock (plotObject)
            {
                steppingStoneBinCountss[index][stepIndex] = binCountss;
                steppingStoneBinSampless[index][stepIndex] = binSampless;
                steppingStoneEssss[index][stepIndex] = essss;
                steppingStoneMaxBinss[index][stepIndex] = maxBinss;
                steppingStoneMeanCoVs[index][stepIndex] = meanCoVs;
                steppingStoneMeanss[index][stepIndex] = meanss;
                steppingStoneRealBinWidthss[index][stepIndex] = realBinWidthss;
                steppingStoneSdCoVs[index][stepIndex] = sdCoVs;
                steppingStoneStdDevss[index][stepIndex] = stdDevss;
                steppingStoneSampleCounts[index][stepIndex] = sampleCounts;

                steppingStoneConvergenceStats[index][stepIndex].Add(new double[] { sampleCounts.Average(), meanCoVs.Skip(1).Max(), sdCoVs.Skip(1).Max(), (from el in essss.Skip(1) select el.Min()).Min() });
                steppingStoneLikelihoodConvergenceStats[index][stepIndex].Add(new double[] { sampleCounts.Average(), meanCoVs[0], sdCoVs[0], essss[0].Min() });
            }
        }

        void PlotBayesianDistribution(int index, int selIndex, Canvas plotCan, StackPanel statsContainer)
        {
            double meanCoV;
            double sdCoV;

            double[] esss;
            double[] means;
            double[] stdDevs;

            int[] binCounts;
            int[][] binSamples;
            double[] realBinWidths;

            double[] maxBins;

            int[] sampleCounts;

            lock (plotObject)
            {
                meanCoV = bayesianMeanCoVs[index][selIndex - 1];
                sdCoV = bayesianSdCoVs[index][selIndex - 1];

                esss = (double[])bayesianEssss[index][selIndex - 1].Clone();
                means = (double[])bayesianMeanss[index][selIndex - 1].Clone();
                stdDevs = (double[])bayesianStdDevss[index][selIndex - 1].Clone();

                binCounts = (int[])bayesianBinCountss[index][selIndex - 1].Clone();
                binSamples = (from el in bayesianBinSampless[index][selIndex - 1] where el != null select (int[])el.Clone()).ToArray();
                realBinWidths = (double[])bayesianRealBinWidthss[index][selIndex - 1].Clone();
                maxBins = (double[])bayesianMaxBinss[index][selIndex - 1].Clone();

                sampleCounts = (int[])bayesianSampleCounts[index].Clone();
            }

            if (binSamples.Length > 0)
            {

                StackPanel meanPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                meanPanel.Children.Add(new Viewbox() { Width = 12, Height = 12, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Child = new Tick() { IconType = meanCoV <= MCMC.convergenceCoVThreshold ? Tick.Type.Tick : Tick.Type.Cross } });
                meanPanel.Children.Add(new TextBlock() { Text = "Mean CoV: " + meanCoV.ToString(3, false), Margin = new Thickness(5, 0, 0, 0) });
                statsContainer.Children.Add(meanPanel);

                StackPanel sdPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                sdPanel.Children.Add(new Viewbox() { Width = 12, Height = 12, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Child = new Tick() { IconType = sdCoV <= MCMC.convergenceCoVThreshold ? Tick.Type.Tick : Tick.Type.Cross } });
                sdPanel.Children.Add(new TextBlock() { Text = "SD CoV: " + sdCoV.ToString(3, false), Margin = new Thickness(5, 0, 0, 0) });
                statsContainer.Children.Add(sdPanel);


                for (int runInd = 0; runInd < samples[index].Length; runInd++)
                {

                    StackPanel sp = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 20, 0, 0) };
                    sp.Children.Add(new Viewbox() { Child = new Octagon() { Fill = Program.GetBrush(Plotting.GetColor(runInd, 1, samples[index].Length)) }, Width = 12, Height = 12, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
                    sp.Children.Add(new TextBlock() { Text = "Run " + (runInd + 1).ToString(), FontWeight = FontWeight.Bold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Foreground = Program.GetBrush(Plotting.GetColor(runInd, 1, samples[index].Length)), Margin = new Thickness(5, 0, 0, 0) });
                    statsContainer.Children.Add(sp);

                    statsContainer.Children.Add(new TextBlock() { Text = "Samples: " + sampleCounts[runInd].ToString(), Margin = new Thickness(17, 5, 0, 0) });

                    StackPanel essPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                    essPanel.Children.Add(new Viewbox() { Width = 12, Height = 12, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Child = new Tick() { IconType = esss[runInd] >= MCMC.convergenceESSThreshold ? Tick.Type.Tick : Tick.Type.Cross } });
                    essPanel.Children.Add(new TextBlock() { Text = "ESS: " + esss[runInd].ToString(2), Margin = new Thickness(5, 0, 0, 0) });
                    statsContainer.Children.Add(essPanel);

                    statsContainer.Children.Add(new TextBlock() { Text = "Mean: " + means[runInd].ToString(3, false), Margin = new Thickness(17, 5, 0, 0) });
                    statsContainer.Children.Add(new TextBlock() { Text = "SD: " + stdDevs[runInd].ToString(3, false), Margin = new Thickness(17, 5, 0, 0) });
                }

                for (int runInd = 0; runInd < samples[index].Length; runInd++)
                {
                    if (binCounts[runInd] > 1)
                    {
                        SolidColorBrush fillBrush = Program.GetTransparentBrush(Plotting.GetColor(runInd, 0.5, samples[index].Length));
                        SolidColorBrush strokeBrush = Program.GetBrush(Plotting.GetColor(runInd, 1, samples[index].Length));

                        PathFigure fig = new PathFigure() { StartPoint = new Point(10 + 0.5 * realBinWidths[runInd], 290 - 270 * binSamples[runInd][0] / maxBins[runInd]) };
                        PathFigure fig2 = new PathFigure() { StartPoint = new Point(10 + 0.5 * realBinWidths[runInd], 290) };
                        fig2.Segments.Add(new LineSegment() { Point = new Point(10 + 0.5 * realBinWidths[runInd], 290 - 270 * binSamples[runInd][0] / maxBins[runInd]) });

                        for (int i = 1; i < binCounts[runInd]; i++)
                        {
                            fig.Segments.Add(new LineSegment() { Point = new Point(10 + (i + 0.5) * realBinWidths[runInd], 290 - 270 * binSamples[runInd][i] / maxBins[runInd]) });
                            fig2.Segments.Add(new LineSegment() { Point = new Point(10 + (i + 0.5) * realBinWidths[runInd], 290 - 270 * binSamples[runInd][i] / maxBins[runInd]) });
                        }

                        fig2.Segments.Add(new LineSegment() { Point = new Point(10 + (binCounts[runInd] - 0.5) * realBinWidths[runInd], 290) });

                        fig2.IsClosed = true;
                        fig.IsClosed = false;

                        PathGeometry geo2 = new PathGeometry();
                        geo2.Figures.Add(fig2);
                        plotCan.Children.Add(new Path() { Fill = fillBrush, Data = geo2 });

                        PathGeometry geo = new PathGeometry();
                        geo.Figures.Add(fig);

                        plotCan.Children.Add(new Path() { Stroke = strokeBrush, StrokeThickness = 2, Data = geo });
                    }
                }
            }
            else
            {
                TextBlock blk = new TextBlock() { Text = "Constant value: " + means.Average().ToString(3, false), FontSize = 20, FontWeight = FontWeight.Bold, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                Grid grd = new Grid() { Width = 600, Height = 300 };
                grd.Children.Add(blk);
                plotCan.Children.Add(grd);
            }
        }


        void PlotSteppingStoneDistribution(int index, int stepIndex, int selIndex, Canvas plotCan, StackPanel statsContainer)
        {
            if (stepIndex > 0 || selIndex > 1)
            {
                double meanCoV;
                double sdCoV;

                double[] esss;
                double[] means;
                double[] stdDevs;

                int[] binCounts;
                int[][] binSamples;
                double[] realBinWidths;

                double[] maxBins;

                int[] sampleCounts;

                lock (plotObject)
                {
                    meanCoV = steppingStoneMeanCoVs[index][stepIndex][selIndex - 1];
                    sdCoV = steppingStoneSdCoVs[index][stepIndex][selIndex - 1];

                    esss = (double[])steppingStoneEssss[index][stepIndex][selIndex - 1].Clone();
                    means = (double[])steppingStoneMeanss[index][stepIndex][selIndex - 1].Clone();
                    stdDevs = (double[])steppingStoneStdDevss[index][stepIndex][selIndex - 1].Clone();

                    binCounts = (int[])steppingStoneBinCountss[index][stepIndex][selIndex - 1].Clone();
                    binSamples = (from el in steppingStoneBinSampless[index][stepIndex][selIndex - 1] where el != null select (int[])el.Clone()).ToArray();
                    realBinWidths = (double[])steppingStoneRealBinWidthss[index][stepIndex][selIndex - 1].Clone();
                    maxBins = (double[])steppingStoneMaxBinss[index][stepIndex][selIndex - 1].Clone();

                    sampleCounts = (int[])steppingStoneSampleCounts[index][stepIndex].Clone();
                }

                if (binSamples.Length > 0)
                {

                    StackPanel meanPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                    meanPanel.Children.Add(new Viewbox() { Width = 12, Height = 12, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Child = new Tick() { IconType = meanCoV <= MCMC.convergenceCoVThreshold ? Tick.Type.Tick : Tick.Type.Cross } });
                    meanPanel.Children.Add(new TextBlock() { Text = "Mean CoV: " + meanCoV.ToString(3, false), Margin = new Thickness(5, 0, 0, 0) });
                    statsContainer.Children.Add(meanPanel);

                    StackPanel sdPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                    sdPanel.Children.Add(new Viewbox() { Width = 12, Height = 12, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Child = new Tick() { IconType = sdCoV <= MCMC.convergenceCoVThreshold ? Tick.Type.Tick : Tick.Type.Cross } });
                    sdPanel.Children.Add(new TextBlock() { Text = "SD CoV: " + sdCoV.ToString(3, false), Margin = new Thickness(5, 0, 0, 0) });
                    statsContainer.Children.Add(sdPanel);


                    for (int runInd = 0; runInd < steppingStoneSamples[index][stepIndex].Length; runInd++)
                    {
                        StackPanel sp = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 20, 0, 0) };
                        sp.Children.Add(new Viewbox() { Child = new Octagon() { Fill = Program.GetBrush(Plotting.GetColor(runInd, 1, steppingStoneSamples[index][stepIndex].Length)) }, Width = 12, Height = 12, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
                        sp.Children.Add(new TextBlock() { Text = "Run " + (runInd + 1).ToString(), FontWeight = FontWeight.Bold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Foreground = Program.GetBrush(Plotting.GetColor(runInd, 1, steppingStoneSamples[index][stepIndex].Length)), Margin = new Thickness(5, 0, 0, 0) });
                        statsContainer.Children.Add(sp);

                        statsContainer.Children.Add(new TextBlock() { Text = "Samples: " + sampleCounts[runInd].ToString(), Margin = new Thickness(17, 5, 0, 0) });

                        StackPanel essPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                        essPanel.Children.Add(new Viewbox() { Width = 12, Height = 12, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Child = new Tick() { IconType = esss[runInd] >= MCMC.convergenceESSThreshold ? Tick.Type.Tick : Tick.Type.Cross } });
                        essPanel.Children.Add(new TextBlock() { Text = "ESS: " + esss[runInd].ToString(2), Margin = new Thickness(5, 0, 0, 0) });
                        statsContainer.Children.Add(essPanel);

                        statsContainer.Children.Add(new TextBlock() { Text = "Mean: " + means[runInd].ToString(3, false), Margin = new Thickness(17, 5, 0, 0) });
                        statsContainer.Children.Add(new TextBlock() { Text = "SD: " + stdDevs[runInd].ToString(3, false), Margin = new Thickness(17, 5, 0, 0) });
                    }

                    for (int runInd = 0; runInd < steppingStoneSamples[index][stepIndex].Length; runInd++)
                    {
                        if (binCounts[runInd] > 1)
                        {
                            SolidColorBrush fillBrush = Program.GetTransparentBrush(Plotting.GetColor(runInd, 0.5, steppingStoneSamples[index][stepIndex].Length));
                            SolidColorBrush strokeBrush = Program.GetBrush(Plotting.GetColor(runInd, 1, steppingStoneSamples[index][stepIndex].Length));

                            PathFigure fig = new PathFigure() { StartPoint = new Point(10 + 0.5 * realBinWidths[runInd], 290 - 270 * binSamples[runInd][0] / maxBins[runInd]) };
                            PathFigure fig2 = new PathFigure() { StartPoint = new Point(10 + 0.5 * realBinWidths[runInd], 290) };
                            fig2.Segments.Add(new LineSegment() { Point = new Point(10 + 0.5 * realBinWidths[runInd], 290 - 270 * binSamples[runInd][0] / maxBins[runInd]) });

                            for (int i = 1; i < binCounts[runInd]; i++)
                            {
                                fig.Segments.Add(new LineSegment() { Point = new Point(10 + (i + 0.5) * realBinWidths[runInd], 290 - 270 * binSamples[runInd][i] / maxBins[runInd]) });
                                fig2.Segments.Add(new LineSegment() { Point = new Point(10 + (i + 0.5) * realBinWidths[runInd], 290 - 270 * binSamples[runInd][i] / maxBins[runInd]) });
                            }

                            fig2.Segments.Add(new LineSegment() { Point = new Point(10 + (binCounts[runInd] - 0.5) * realBinWidths[runInd], 290) });

                            fig2.IsClosed = true;
                            fig.IsClosed = false;

                            PathGeometry geo2 = new PathGeometry();
                            geo2.Figures.Add(fig2);
                            plotCan.Children.Add(new Path() { Fill = fillBrush, Data = geo2 });

                            PathGeometry geo = new PathGeometry();
                            geo.Figures.Add(fig);

                            plotCan.Children.Add(new Path() { Stroke = strokeBrush, StrokeThickness = 2, Data = geo });
                        }
                    }
                }
                else
                {
                    TextBlock blk = new TextBlock() { Text = "Constant value: " + means.Average().ToString(3, false), FontSize = 20, FontWeight = FontWeight.Bold, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                    Grid grd = new Grid() { Width = 600, Height = 300 };
                    grd.Children.Add(blk);
                    plotCan.Children.Add(grd);
                }
            }
            else
            {
                double meanCoV = 0;
                double sdCoV = 0;

                double[] esss;
                double[] means;
                double[] stdDevs;

                int[] sampleCounts;

                lock (plotObject)
                {
                    
                    esss = (double[])steppingStoneEssss[index][stepIndex][selIndex - 1].Clone();
                    means = (double[])steppingStoneMeanss[index][stepIndex][selIndex - 1].Clone();
                    stdDevs = (double[])steppingStoneStdDevss[index][stepIndex][selIndex - 1].Clone();

                    sampleCounts = (int[])steppingStoneSampleCounts[index][stepIndex].Clone();
                }

                StackPanel meanPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                meanPanel.Children.Add(new Viewbox() { Width = 12, Height = 12, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Child = new Tick() { IconType = meanCoV <= MCMC.convergenceCoVThreshold ? Tick.Type.Tick : Tick.Type.Cross } });
                meanPanel.Children.Add(new TextBlock() { Text = "Mean CoV: " + meanCoV.ToString(3, false), Margin = new Thickness(5, 0, 0, 0) });
                statsContainer.Children.Add(meanPanel);

                StackPanel sdPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                sdPanel.Children.Add(new Viewbox() { Width = 12, Height = 12, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Child = new Tick() { IconType = sdCoV <= MCMC.convergenceCoVThreshold ? Tick.Type.Tick : Tick.Type.Cross } });
                sdPanel.Children.Add(new TextBlock() { Text = "SD CoV: " + sdCoV.ToString(3, false), Margin = new Thickness(5, 0, 0, 0) });
                statsContainer.Children.Add(sdPanel);


                for (int runInd = 0; runInd < steppingStoneSamples[index][stepIndex].Length; runInd++)
                {

                    StackPanel sp = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 20, 0, 0) };
                    sp.Children.Add(new Viewbox() { Child = new Octagon() { Fill = Program.GetBrush(Plotting.GetColor(runInd, 1, steppingStoneSamples[index][stepIndex].Length)) }, Width = 12, Height = 12, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
                    sp.Children.Add(new TextBlock() { Text = "Run " + (runInd + 1).ToString(), FontWeight = FontWeight.Bold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Foreground = Program.GetBrush(Plotting.GetColor(runInd, 1, steppingStoneSamples[index][stepIndex].Length)), Margin = new Thickness(5, 0, 0, 0) });
                    statsContainer.Children.Add(sp);

                    statsContainer.Children.Add(new TextBlock() { Text = "Samples: " + sampleCounts[runInd].ToString(), Margin = new Thickness(17, 5, 0, 0) });

                    statsContainer.Children.Add(new TextBlock() { Text = "Mean: " + means[runInd].ToString(3, false), Margin = new Thickness(17, 5, 0, 0) });
                    statsContainer.Children.Add(new TextBlock() { Text = "SD: " + stdDevs[runInd].ToString(3, false), Margin = new Thickness(17, 5, 0, 0) });
                }
            }
        }

        void PlotConvergenceStats(int index, Canvas plotCan, StackPanel statsContainer)
        {
            double[][] convergenceStats;

            lock (plotObject)
            {
                int samplingSkip = bayesianConvergenceStats[index].Count / 600 + 1;
                convergenceStats = (from el in Utils.Utils.Range(0, bayesianConvergenceStats[index].Count) where el % samplingSkip == 0 select bayesianConvergenceStats[index][el]).ToArray();
            }

            if (convergenceStats.Length > 0)
            {

                StackPanel samplesPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                samplesPanel.Children.Add(new Viewbox() { Width = 12, Height = 12, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Child = new Tick() { IconType = ((int)convergenceStats.Last()[0] / (MCMC.diagnosticFrequency / MCMC.sampleFrequency) - ((int)convergenceStats.Last()[0] / (MCMC.diagnosticFrequency / MCMC.sampleFrequency)) / 10) * MCMC.diagnosticFrequency >= MCMC.minSamples * MCMC.sampleFrequency ? Tick.Type.Tick : Tick.Type.Cross } });
                samplesPanel.Children.Add(new TextBlock() { Text = "Samples: " + convergenceStats.Last()[0].ToString(0), Margin = new Thickness(5, 0, 0, 0), FontWeight = FontWeight.Bold });
                statsContainer.Children.Add(samplesPanel);

                StackPanel meanPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                meanPanel.Children.Add(new Viewbox() { Width = 12, Height = 12, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Child = new Tick() { IconType = convergenceStats.Last()[1] <= MCMC.convergenceCoVThreshold ? Tick.Type.Tick : Tick.Type.Cross } });
                meanPanel.Children.Add(new TextBlock() { Text = "Max Mean CoV: " + convergenceStats.Last()[1].ToString(3, false), Margin = new Thickness(5, 0, 0, 0), FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 162, 232)) });
                statsContainer.Children.Add(meanPanel);

                StackPanel sdPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                sdPanel.Children.Add(new Viewbox() { Width = 12, Height = 12, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Child = new Tick() { IconType = convergenceStats.Last()[2] <= MCMC.convergenceCoVThreshold ? Tick.Type.Tick : Tick.Type.Cross } });
                sdPanel.Children.Add(new TextBlock() { Text = "Max SD CoV: " + convergenceStats.Last()[2].ToString(3, false), Margin = new Thickness(5, 0, 0, 0), FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.FromArgb(255, 34, 177, 76)) });
                statsContainer.Children.Add(sdPanel);

                StackPanel ESSPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                ESSPanel.Children.Add(new Viewbox() { Width = 12, Height = 12, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Child = new Tick() { IconType = convergenceStats.Last()[3] >= MCMC.convergenceESSThreshold ? Tick.Type.Tick : Tick.Type.Cross } });
                ESSPanel.Children.Add(new TextBlock() { Text = "Min ESS: " + convergenceStats.Last()[3].ToString(3, false), Margin = new Thickness(5, 0, 0, 0), FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.FromArgb(255, 237, 28, 36)) });
                statsContainer.Children.Add(ESSPanel);



                double maxX = Math.Max((from el in convergenceStats select el[0]).Max(), MCMC.minSamples * 1.1) * 1.05;
                double maxCoV = Math.Max((from el in convergenceStats select Math.Max(el[1], el[2])).Max(), MCMC.convergenceCoVThreshold) * 1.05;
                double maxESS = Math.Max((from el in convergenceStats select el[3]).Max(), MCMC.convergenceESSThreshold) * 1.05;

                plotCan.Children.Add(new Line() { StrokeThickness = 2, Stroke = new SolidColorBrush(Color.FromArgb(255, 246, 142, 146)), StartPoint = new Point(10, 290 - 270 * MCMC.convergenceESSThreshold / maxESS), EndPoint = new Point(585, 290 - 270 * MCMC.convergenceESSThreshold / maxESS) });
                plotCan.Children.Add(new Line() { StrokeThickness = 2, Stroke = new SolidColorBrush(Color.FromArgb(255, 136, 212, 205)), StartPoint = new Point(10, 290 - 270 * MCMC.convergenceCoVThreshold / maxCoV), EndPoint = new Point(585, 290 - 270 * MCMC.convergenceCoVThreshold / maxCoV) });
                plotCan.Children.Add(new Line() { StrokeThickness = 2, Stroke = new SolidColorBrush(Color.FromArgb(255, 180, 180, 180)), StartPoint = new Point(10 + 575 * MCMC.minSamples * 1.1 / maxX, 20), EndPoint = new Point(10 + 575 * MCMC.minSamples * 1.1 / maxX, 290) });

                PathFigure figMeanCoV = new PathFigure() { IsClosed = false };
                PathFigure figSDCoV = new PathFigure() { IsClosed = false };
                PathFigure figESS = new PathFigure() { IsClosed = false };

                for (int i = 0; i < convergenceStats.Length; i++)
                {
                    double x = 10 + 575 * convergenceStats[i][0] / maxX;

                    double yMeanCov = 290 - 270 * (convergenceStats[i][1] / maxCoV);
                    double ySDCov = 290 - 270 * (convergenceStats[i][2] / maxCoV);
                    double yESS = 290 - 270 * (convergenceStats[i][3] / maxESS);

                    if (i == 0)
                    {
                        figMeanCoV.StartPoint = new Point(x, yMeanCov);
                        figSDCoV.StartPoint = new Point(x, ySDCov);
                        figESS.StartPoint = new Point(x, yESS);
                    }
                    else
                    {
                        figMeanCoV.Segments.Add(new LineSegment() { Point = new Point(x, yMeanCov) });
                        figSDCoV.Segments.Add(new LineSegment() { Point = new Point(x, ySDCov) });
                        figESS.Segments.Add(new LineSegment() { Point = new Point(x, yESS) });
                    }
                }

                PathGeometry geoMeanCov = new PathGeometry();
                geoMeanCov.Figures.Add(figMeanCoV);

                PathGeometry geoSDCov = new PathGeometry();
                geoSDCov.Figures.Add(figSDCoV);

                PathGeometry geoESS = new PathGeometry();
                geoESS.Figures.Add(figESS);

                plotCan.Children.Add(new Path() { Stroke = new SolidColorBrush(Color.FromArgb(255, 237, 28, 36)), StrokeThickness = 2, StrokeJoin = PenLineJoin.Round, Data = geoESS });
                plotCan.Children.Add(new Path() { Stroke = new SolidColorBrush(Color.FromArgb(255, 34, 177, 76)), StrokeThickness = 2, StrokeJoin = PenLineJoin.Round, Data = geoSDCov });
                plotCan.Children.Add(new Path() { Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 162, 232)), StrokeThickness = 2, StrokeJoin = PenLineJoin.Round, Data = geoMeanCov });
            }
        }



        void PlotSteppingStoneConvergenceStats(int index, int stepIndex, Canvas plotCan, StackPanel statsContainer)
        {
            double[][] convergenceStats;
            double[][] likelihoodConvergenceStats = null;


            lock (plotObject)
            {
                int samplingSkip = steppingStoneConvergenceStats[index].Count / 600 + 1;
                convergenceStats = (from el in Utils.Utils.Range(0, steppingStoneConvergenceStats[index][stepIndex].Count) where el % samplingSkip == 0 select steppingStoneConvergenceStats[index][stepIndex][el]).ToArray();
                if (stepIndex > 0)
                {
                    likelihoodConvergenceStats = (from el in Utils.Utils.Range(0, steppingStoneLikelihoodConvergenceStats[index][stepIndex].Count) where el % samplingSkip == 0 select steppingStoneLikelihoodConvergenceStats[index][stepIndex][el]).ToArray();
                }
            }

            if (convergenceStats.Length > 0)
            {

                StackPanel samplesPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                samplesPanel.Children.Add(new Viewbox() { Width = 12, Height = 12, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Child = new Tick() { IconType = ((int)convergenceStats.Last()[0] / (MCMC.diagnosticFrequency / MCMC.sampleFrequency) - ((int)convergenceStats.Last()[0] / (MCMC.diagnosticFrequency / MCMC.sampleFrequency)) / 10) * MCMC.diagnosticFrequency >= MCMC.minSamples * MCMC.sampleFrequency ? Tick.Type.Tick : Tick.Type.Cross } });
                samplesPanel.Children.Add(new TextBlock() { Text = "Samples: " + convergenceStats.Last()[0].ToString(0), Margin = new Thickness(5, 0, 0, 0), FontWeight = FontWeight.Bold });
                statsContainer.Children.Add(samplesPanel);

                StackPanel meanPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                meanPanel.Children.Add(new Viewbox() { Width = 12, Height = 12, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Child = new Tick() { IconType = convergenceStats.Last()[1] <= MCMC.convergenceCoVThreshold ? Tick.Type.Tick : Tick.Type.Cross } });
                meanPanel.Children.Add(new TextBlock() { Text = "Max Mean CoV: " + convergenceStats.Last()[1].ToString(3, false), Margin = new Thickness(5, 0, 0, 0), FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 162, 232)) });
                statsContainer.Children.Add(meanPanel);

                StackPanel sdPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                sdPanel.Children.Add(new Viewbox() { Width = 12, Height = 12, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Child = new Tick() { IconType = convergenceStats.Last()[2] <= MCMC.convergenceCoVThreshold ? Tick.Type.Tick : Tick.Type.Cross } });
                sdPanel.Children.Add(new TextBlock() { Text = "Max SD CoV: " + convergenceStats.Last()[2].ToString(3, false), Margin = new Thickness(5, 0, 0, 0), FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.FromArgb(255, 34, 177, 76)) });
                statsContainer.Children.Add(sdPanel);

                StackPanel ESSPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                ESSPanel.Children.Add(new Viewbox() { Width = 12, Height = 12, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Child = new Tick() { IconType = convergenceStats.Last()[3] >= MCMC.convergenceESSThreshold ? Tick.Type.Tick : Tick.Type.Cross } });
                ESSPanel.Children.Add(new TextBlock() { Text = "Min ESS: " + convergenceStats.Last()[3].ToString(3, false), Margin = new Thickness(5, 0, 0, 0), FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.FromArgb(255, 237, 28, 36)) });
                statsContainer.Children.Add(ESSPanel);

                if (stepIndex > 0)
                {
                    StackPanel likMeanPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                    likMeanPanel.Children.Add(new Viewbox() { Width = 12, Height = 12, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Child = new Tick() { IconType = likelihoodConvergenceStats.Last()[1] <= MCMC.convergenceCoVThreshold ? Tick.Type.Tick : Tick.Type.Cross } });
                    likMeanPanel.Children.Add(new TextBlock() { Text = "Likelihood Mean CoV: " + likelihoodConvergenceStats.Last()[1].ToString(3, false), Margin = new Thickness(5, 0, 0, 0), FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.FromArgb(255, 153, 217, 234)) });
                    statsContainer.Children.Add(likMeanPanel);

                    StackPanel likSdPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                    likSdPanel.Children.Add(new Viewbox() { Width = 12, Height = 12, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Child = new Tick() { IconType = likelihoodConvergenceStats.Last()[2] <= MCMC.convergenceCoVThreshold ? Tick.Type.Tick : Tick.Type.Cross } });
                    likSdPanel.Children.Add(new TextBlock() { Text = "Likelihood SD CoV: " + likelihoodConvergenceStats.Last()[2].ToString(3, false), Margin = new Thickness(5, 0, 0, 0), FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.FromArgb(255, 168, 238, 189)) });
                    statsContainer.Children.Add(likSdPanel);

                    StackPanel likESSPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                    likESSPanel.Children.Add(new Viewbox() { Width = 12, Height = 12, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Child = new Tick() { IconType = likelihoodConvergenceStats.Last()[3] >= MCMC.convergenceESSThreshold ? Tick.Type.Tick : Tick.Type.Cross } });
                    likESSPanel.Children.Add(new TextBlock() { Text = "Likelihood Min ESS: " + likelihoodConvergenceStats.Last()[3].ToString(3, false), Margin = new Thickness(5, 0, 0, 0), FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 174, 201)) });
                    statsContainer.Children.Add(likESSPanel);
                }

                double maxX = Math.Max((from el in convergenceStats select el[0]).Max(), MCMC.minSamples * 1.1) * 1.05;
                double maxCoV = Math.Max((from el in convergenceStats select Math.Max(el[1], el[2])).Max(), MCMC.convergenceCoVThreshold);
                double maxESS = Math.Max((from el in convergenceStats select el[3]).Max(), MCMC.convergenceESSThreshold);

                if (stepIndex > 0)
                {
                    maxCoV = Math.Max((from el in likelihoodConvergenceStats select Math.Max(el[1], el[2])).Max(), maxCoV);
                    maxESS = Math.Max((from el in likelihoodConvergenceStats select el[3]).Max(), maxESS);
                }

                plotCan.Children.Add(new Line() { StrokeThickness = 2, Stroke = new SolidColorBrush(Color.FromArgb(255, 246, 142, 146)), StartPoint = new Point(10, 290 - 270 * MCMC.convergenceESSThreshold / maxESS), EndPoint = new Point(585, 290 - 270 * MCMC.convergenceESSThreshold / maxESS) });
                plotCan.Children.Add(new Line() { StrokeThickness = 2, Stroke = new SolidColorBrush(Color.FromArgb(255, 136, 212, 205)), StartPoint = new Point(10, 290 - 270 * MCMC.convergenceCoVThreshold / maxCoV), EndPoint = new Point(585, 290 - 270 * MCMC.convergenceCoVThreshold / maxCoV) });
                plotCan.Children.Add(new Line() { StrokeThickness = 2, Stroke = new SolidColorBrush(Color.FromArgb(255, 180, 180, 180)), StartPoint = new Point(10 + 575 * MCMC.minSamples * 1.1 / maxX, 20), EndPoint = new Point(10 + 575 * MCMC.minSamples * 1.1 / maxX, 290) });

                PathFigure figMeanCoV = new PathFigure() { IsClosed = false };
                PathFigure figSDCoV = new PathFigure() { IsClosed = false };
                PathFigure figESS = new PathFigure() { IsClosed = false };

                PathFigure figLikMeanCoV = new PathFigure() { IsClosed = false };
                PathFigure figLikSDCoV = new PathFigure() { IsClosed = false };
                PathFigure figLikESS = new PathFigure() { IsClosed = false };

                for (int i = 0; i < convergenceStats.Length; i++)
                {
                    double x = 10 + 575 * convergenceStats[i][0] / maxX;

                    double yMeanCov = 290 - 270 * (convergenceStats[i][1] / maxCoV);
                    double ySDCov = 290 - 270 * (convergenceStats[i][2] / maxCoV);
                    double yESS = 290 - 270 * (convergenceStats[i][3] / maxESS);

                    if (i == 0)
                    {
                        figMeanCoV.StartPoint = new Point(x, yMeanCov);
                        figSDCoV.StartPoint = new Point(x, ySDCov);
                        figESS.StartPoint = new Point(x, yESS);
                    }
                    else
                    {
                        figMeanCoV.Segments.Add(new LineSegment() { Point = new Point(x, yMeanCov) });
                        figSDCoV.Segments.Add(new LineSegment() { Point = new Point(x, ySDCov) });
                        figESS.Segments.Add(new LineSegment() { Point = new Point(x, yESS) });   
                    }

                    if (stepIndex > 0)
                    {
                        double yLikMeanCov = 290 - 270 * (likelihoodConvergenceStats[i][1] / maxCoV);
                        double yLikSDCov = 290 - 270 * (likelihoodConvergenceStats[i][2] / maxCoV);
                        double yLikESS = 290 - 270 * (likelihoodConvergenceStats[i][3] / maxESS);

                        if (i == 0)
                        {
                            figLikMeanCoV.StartPoint = new Point(x, yLikMeanCov);
                            figLikSDCoV.StartPoint = new Point(x, yLikSDCov);
                            figLikESS.StartPoint = new Point(x, yLikESS);
                        }
                        else
                        {
                            figLikMeanCoV.Segments.Add(new LineSegment() { Point = new Point(x, yLikMeanCov) });
                            figLikSDCoV.Segments.Add(new LineSegment() { Point = new Point(x, yLikSDCov) });
                            figLikESS.Segments.Add(new LineSegment() { Point = new Point(x, yLikESS) });
                        }
                    }
                }

                if (stepIndex > 0)
                {

                    PathGeometry geoLikMeanCov = new PathGeometry();
                    geoLikMeanCov.Figures.Add(figLikMeanCoV);

                    PathGeometry geoLikSDCov = new PathGeometry();
                    geoLikSDCov.Figures.Add(figLikSDCoV);

                    PathGeometry geoLikESS = new PathGeometry();
                    geoLikESS.Figures.Add(figLikESS);

                    plotCan.Children.Add(new Path() { Stroke = new SolidColorBrush(Color.FromArgb(255, 255, 174, 201)), StrokeThickness = 2, StrokeJoin = PenLineJoin.Round, Data = geoLikESS });
                    plotCan.Children.Add(new Path() { Stroke = new SolidColorBrush(Color.FromArgb(255, 168, 238, 189)), StrokeThickness = 2, StrokeJoin = PenLineJoin.Round, Data = geoLikSDCov });
                    plotCan.Children.Add(new Path() { Stroke = new SolidColorBrush(Color.FromArgb(255, 153, 217, 234)), StrokeThickness = 2, StrokeJoin = PenLineJoin.Round, Data = geoLikMeanCov });
                }

                PathGeometry geoMeanCov = new PathGeometry();
                geoMeanCov.Figures.Add(figMeanCoV);

                PathGeometry geoSDCov = new PathGeometry();
                geoSDCov.Figures.Add(figSDCoV);

                PathGeometry geoESS = new PathGeometry();
                geoESS.Figures.Add(figESS);

                plotCan.Children.Add(new Path() { Stroke = new SolidColorBrush(Color.FromArgb(255, 237, 28, 36)), StrokeThickness = 2, StrokeJoin = PenLineJoin.Round, Data = geoESS });
                plotCan.Children.Add(new Path() { Stroke = new SolidColorBrush(Color.FromArgb(255, 34, 177, 76)), StrokeThickness = 2, StrokeJoin = PenLineJoin.Round, Data = geoSDCov });
                plotCan.Children.Add(new Path() { Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 162, 232)), StrokeThickness = 2, StrokeJoin = PenLineJoin.Round, Data = geoMeanCov });
            }
        }
    }
}
