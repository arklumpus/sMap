using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using SlimTreeNode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace sMap_GUI
{
    public partial class RunWindow : Window
    {
        public RunWindow()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        bool CloseOnceFinished = false;
        bool PromptAtCompletion = false;

        public RunWindow(string[] args, bool closeOnceFinished, bool promptAtCompletion)
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            CloseOnceFinished = closeOnceFinished;
            PromptAtCompletion = promptAtCompletion;

            if (!args.Contains("--ss") && !args.Contains("--stepping-stone"))
            {
                this.FindControl<Canvas>("SteppingStoneSamplingCanvas").IsVisible = false;
            }

            sMap.Program.RunningGUI = true;

            Utils.Utils.Trigger = async (s, o) => { await ProgressTrigger(s, o); };

            EnableCanvas("DataParsingCanvas");
            EnableAnimation("DataParsingCanvas");

            Thread thr = new Thread(() =>
            {
                sMap.Program.Main(args);
            });

            thr.Start();
        }


        DataMatrix parsedData;

        int totalTrees = 0;

        TreeNode meanTree;

        CharacterDependency[][] inputDependencies;

        CharacterDependency[][] dependencies;
        Dictionary<string, Parameter>[] pi;
        Dictionary<string, Parameter>[] rates;
        DataMatrix charData;

        List<string>[] paramNames;
        List<List<string>> realParamNames;

        List<List<double>> previousSetsValues = new List<List<double>>();
        List<TextBlock> MLTextBlocks = new List<TextBlock>();
        List<double> sampledValues;
        List<double> currentValues;
        int currentSteps;
        object plotObject = new object();

        int completedNodeProbs = 0;

        EventWaitHandle plotTrigger;
        EventWaitHandle plotFinishedTrigger;

        double likelihoodMaxY = double.MinValue;
        double likelihoodMinY = double.MaxValue;

        bool awaitingScroll = false;

        double[][][] meanPriors;
        double[][][] meanPosteriors;
        double[][][] meanLikelihoods;

        int completedSimulations = 0;

        TaggedHistory[][] histories;

        double[][][] stateProbs;

        LikelihoodModel meanLikModel;
        LikelihoodModel[] likModels;
        int[][] meanNodeCorresp;
        int[] treeSamples;


        List<string> bayesianSetItems;
        List<double> burnInProgress;
        List<bool> estimateStepSizes;
        List<double[]> stepSizes;
        List<List<(int, double[][])>[]> samples;
        int steppingStoneTotalSteps;

        double[][][] sampledParameters;
        Dictionary<string, int>[] parameterIndices;

        EventWaitHandle bayesianSampleReceived;
        EventWaitHandle bayesianFinished;
        EventWaitHandle bayesianPlotUpdateRequested;

        List<string> steppingStoneSetItems;
        List<List<string>> steppingStoneStepItems;
        List<double[]> steppingStoneburninProgress;
        bool steppingStoneEstimateStep;
        List<double[][]> steppingStoneStepSizes;
        List<List<(int, double[][])>[][]> steppingStoneSamples;

        bool steppingStoneStarted = false;
        EventWaitHandle steppingStoneSampleReceived;
        EventWaitHandle steppingStoneFinished;
        EventWaitHandle steppingStonePlotUpdateRequested;

        List<double[]> steppingStoneContributions;

        List<double[]> minCurvatures = new List<double[]>();

        public SerializedRun[] FinishedRuns;

        private async Task ProgressTrigger(string state, object[] data)
        {
            if (state == "DataRead")
            {
                parsedData = (DataMatrix)data[0];

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<TextBlock>("TaxaCountBlock").Text = parsedData.Data.Count.ToString();
                    this.FindControl<TextBlock>("CharCountBlock").Text = parsedData.States.Length.ToString();
                    this.FindControl<TextBlock>("StateCountBlock").Text = (from el in parsedData.States select el.Length.ToString()).Aggregate((a, b) => (a + ", " + b));

                    this.FindControl<StackPanel>("ReadingStateDataInfo").IsVisible = false;
                    this.FindControl<StackPanel>("ReadStateDataInfo").IsVisible = true;
                });
            }
            else if (state == "StartReadingTrees")
            {
                string fileName = (string)data[0];

                try
                {
                    totalTrees = System.IO.File.ReadLines(fileName).Count();
                }
                catch (Exception e)
                {
                    await new MessageBox("Error!", "Error:\n" + e.Message).ShowDialog(this);
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<Grid>("TreeDataGrid").IsVisible = true;

                    this.FindControl<ProgressBar>("ReadTreeProgressBar").Value = 0;
                    this.FindControl<ProgressBar>("ReadTreeProgressBar").Maximum = totalTrees;

                    this.FindControl<TextBlock>("ReadTreeProgressDesc").Text = "0 / " + totalTrees.ToString();
                });
            }
            else if (state == "ReadTree")
            {
                int readTrees = (int)data[0];

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<ProgressBar>("ReadTreeProgressBar").Value = readTrees;
                    this.FindControl<TextBlock>("ReadTreeProgressDesc").Text = readTrees.ToString() + " / " + totalTrees.ToString();
                });
            }
            else if (state == "ReadAllTrees")
            {
                int readTrees = (int)data[0];

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<TextBlock>("TotalReadTrees").Text = readTrees.ToString();

                    this.FindControl<StackPanel>("ReadingTreesInfo").IsVisible = false;
                    this.FindControl<StackPanel>("ReadTreesInfo").IsVisible = true;
                });
            }
            else if (state == "ReadMeanTree")
            {
                meanTree = (TreeNode)data[0];
                meanLikModel = (LikelihoodModel)data[1];
                meanNodeCorresp = (int[][])data[2];
                likModels = (LikelihoodModel[])data[3];

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<Button>("ViewMeanTreeButton").IsVisible = true;
                });


            }
            else if (state == "ReadModel")
            {
                inputDependencies = (CharacterDependency[][])data[0];

                charData = (DataMatrix)data[1];
                dependencies = (CharacterDependency[][])data[2];

                FinishedRuns = new SerializedRun[dependencies.Length];

                bool foundCond = false;

                for (int i = 0; i < dependencies.Length && !foundCond; i++)
                {
                    for (int j = 0; j < dependencies[i].Length; j++)
                    {
                        if (dependencies[i][j].Type == CharacterDependency.Types.Conditioned)
                        {
                            foundCond = true;
                            break;
                        }
                    }
                }

                if (foundCond)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        this.FindControl<Button>("ViewCondProbsModelButton").IsVisible = true;
                        this.FindControl<Button>("ViewBayesianCondProbsButton").IsVisible = true;
                    });
                }

                pi = (Dictionary<string, Parameter>[])data[3];
                rates = (Dictionary<string, Parameter>[])data[4];

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<Grid>("ModelDataGrid").IsVisible = true;
                });
            }
            else if (state == "BayesSkipped")
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<Canvas>("BayesianSamplingCanvas").IsVisible = false;
                });
            }
            else if (state == "MLSkipped")
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<Canvas>("MLEstimationCanvas").IsVisible = false;
                });
            }
            else if (state == "DataParsingFinished")
            {
                paramNames = (List<string>[])data[0];

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<Spinner>("DataParsingSpinner").IsVisible = false;
                    DisableAnimation("DataParsingCanvas");
                });
            }
            else if (state == "MLStarted")
            {
                int ind;
                lock (plotObject)
                {
                    ind = MLTextBlocks.Count;
                    plotTrigger = new EventWaitHandle(false, EventResetMode.ManualReset);
                    plotFinishedTrigger = new EventWaitHandle(false, EventResetMode.ManualReset);
                }

                sampledValues = new List<double>();
                currentValues = new List<double>();
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<Grid>("MLSamplingGrid").IsVisible = true;
                    EnableCanvas("MLEstimationCanvas");
                    EnableAnimation("MLEstimationCanvas");

                    TextBlock blk = new TextBlock() { Text = "Set " + ind.ToString(), Foreground = Program.GetDarkBrush(Plotting.GetColor(previousSetsValues.Count, 0.75, dependencies.Length)), Margin = new Thickness(5), FontWeight = FontWeight.Bold };

                    MLTextBlocks.Add(blk);

                    this.FindControl<StackPanel>("SetNameContainer").Children.Add(blk);

                    awaitingScroll = true;
                });

                Thread MLPlotThread = new Thread(async () =>
                {
                    while (true)
                    {
                        int handle = WaitHandle.WaitAny(new WaitHandle[] { plotTrigger, plotFinishedTrigger });

                        if (awaitingScroll)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                this.FindControl<ScrollViewer>("MainScrollViewer").ScrollToBottom();
                            });
                            awaitingScroll = false;
                        }


                        if (handle == 1)
                        {
                            plotFinishedTrigger.Reset();
                            break;
                        }
                        else
                        {
                            plotTrigger.Reset();

                            List<double> localSampledValues;
                            List<double> localCurrentValues;
                            int localSteps;

                            lock (plotObject)
                            {
                                localSampledValues = new List<double>(sampledValues);
                                localCurrentValues = new List<double>(currentValues);
                                localSteps = currentSteps;
                            }

                            if (localCurrentValues.Count > 0)
                            {
                                likelihoodMaxY = Math.Max(likelihoodMaxY, localCurrentValues.Max());
                                likelihoodMinY = Math.Min(likelihoodMinY, localCurrentValues.Min());
                            }

                            if (localSampledValues.Count > 0)
                            {
                                likelihoodMaxY = Math.Max(likelihoodMaxY, localSampledValues.Max());
                                likelihoodMinY = Math.Min(likelihoodMinY, localSampledValues.Min());
                            }

                            if (likelihoodMaxY == likelihoodMinY)
                            {
                                likelihoodMinY *= 0.9;
                            }

                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                this.FindControl<Canvas>("MLPlotContainer").Children.Clear();

                                for (int j = 0; j < previousSetsValues.Count; j++)
                                {
                                    PathFigure fig = new PathFigure();

                                    bool figStarted = false;

                                    for (int i = 0; i < previousSetsValues[j].Count; i++)
                                    {
                                        double x = (double)i / (previousSetsValues[j].Count) * 575 + 10;

                                        double y = 290 - 270 * (previousSetsValues[j][i] - likelihoodMinY) / (likelihoodMaxY - likelihoodMinY);

                                        if (!double.IsNaN(y))
                                        {
                                            if (!figStarted)
                                            {
                                                fig.StartPoint = new Point(x, y);
                                                figStarted = true;
                                            }
                                            else
                                            {
                                                fig.Segments.Add(new LineSegment() { Point = new Point(x, y) });
                                            }
                                        }
                                    }

                                    fig.IsClosed = false;

                                    PathGeometry geo = new PathGeometry();
                                    geo.Figures.Add(fig);

                                    Path currentMLPlotPath = new Path() { Stroke = Program.GetDarkBrush(Plotting.GetColor(j, 0.75, dependencies.Length)), StrokeThickness = 3, StrokeJoin = PenLineJoin.Round };

                                    currentMLPlotPath.Data = geo;

                                    this.FindControl<Canvas>("MLPlotContainer").Children.Add(currentMLPlotPath);
                                }

                                if (localCurrentValues.Count > 0 || localSampledValues.Count > 0)
                                {

                                    double currML = double.MinValue;

                                    if (localCurrentValues.Count > 0)
                                    {
                                        currML = Math.Max(currML, localCurrentValues.Max());
                                    }

                                    if (localSampledValues.Count > 0)
                                    {
                                        currML = Math.Max(currML, localSampledValues.Max());
                                    }

                                    MLTextBlocks[MLTextBlocks.Count - 1].Text = "Set " + (MLTextBlocks.Count - 1).ToString() + ": " + currML.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
                                }

                                if (previousSetsValues.Count < dependencies.Length)
                                {
                                    PathFigure fig = new PathFigure();

                                    bool figStarted = false;

                                    for (int i = 0; i < localSampledValues.Count; i++)
                                    {
                                        double x = (double)i / (localSampledValues.Count + localSteps) * 575 + 10;

                                        double y = 290 - 270 * (localSampledValues[i] - likelihoodMinY) / (likelihoodMaxY - likelihoodMinY);

                                        if (!double.IsNaN(y))
                                        {
                                            if (!figStarted)
                                            {
                                                fig.StartPoint = new Point(x, y);
                                                figStarted = true;
                                            }
                                            else
                                            {
                                                fig.Segments.Add(new LineSegment() { Point = new Point(x, y) });
                                            }
                                        }
                                    }

                                    for (int i = 0; i < localCurrentValues.Count; i++)
                                    {
                                        double x = (double)(localSampledValues.Count + i) / (localSampledValues.Count + localSteps) * 575 + 10;

                                        double y = 290 - 270 * (localCurrentValues[i] - likelihoodMinY) / (likelihoodMaxY - likelihoodMinY);

                                        if (!double.IsNaN(y))
                                        {
                                            if (!figStarted)
                                            {
                                                fig.StartPoint = new Point(x, y);
                                                figStarted = true;
                                            }
                                            else
                                            {
                                                fig.Segments.Add(new LineSegment() { Point = new Point(x, y) });
                                            }
                                        }
                                    }

                                    fig.IsClosed = false;

                                    PathGeometry geo = new PathGeometry();
                                    geo.Figures.Add(fig);

                                    Path currentMLPlotPath = new Path() { Stroke = Program.GetDarkBrush(Plotting.GetColor(previousSetsValues.Count, 0.75, dependencies.Length)), StrokeThickness = 3, StrokeJoin = PenLineJoin.Round };

                                    currentMLPlotPath.Data = geo;

                                    this.FindControl<Canvas>("MLPlotContainer").Children.Add(currentMLPlotPath);
                                }
                            });
                        }
                    }
                });

                MLPlotThread.Start();
            }
            else if (state == "Plot" || state == "StepFinished")
            {
                lock (plotObject)
                {
                    currentSteps = (int)data[2];
                    currentValues = new List<double>((List<double>)data[1]);

                    if (state == "StepFinished")
                    {
                        sampledValues.AddRange((List<double>)data[1]);
                    }
                    plotTrigger.Set();
                }
            }
            else if (state == "MLFinished")
            {
                lock (plotObject)
                {
                    previousSetsValues.Add(sampledValues);
                    sampledValues = new List<double>();
                    currentValues = new List<double>();
                }
                plotFinishedTrigger.Set();
            }
            else if (state == "LikelihoodCurvature")
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    minCurvatures.Add(new double[] { (double)data[1], (double)data[2] });

                    this.FindControl<StackPanel>("CurvatureContainer").Children.Clear();

                    if (minCurvatures.Any(x => !double.IsNaN(x[0]) || !double.IsNaN(x[1])))
                    {
                        this.FindControl<StackPanel>("CurvatureContainer").Children.Add(new TextBlock() { Text = "Minimum curvature: ", Margin = new Thickness(5, 30, 5, 5), FontWeight = FontWeight.Bold });
                        for (int i = 0; i < minCurvatures.Count; i++)
                        {
                            if (!double.IsNaN(minCurvatures[i][0]) || !double.IsNaN(minCurvatures[i][1]))
                            {
                                this.FindControl<StackPanel>("CurvatureContainer").Children.Add(new TextBlock() { Text = "Set " + i.ToString() + ": " + minCurvatures[i][0].ToString("0.000") + " (" + minCurvatures[i][1].ToString("0.000") + ")", Foreground = Program.GetDarkBrush(Plotting.GetColor(i, 0.75, dependencies.Length)), Margin = new Thickness(15, 5, 5, 5), FontWeight = FontWeight.Bold });
                            }                            
                        }
                    }
                });
            }
            else if (state == "AllMLFinished")
            {
                double ml = (double)data[0];
                double aic = (double)data[1];
                double aicc = (double)data[2];
                double bic = (double)data[3];

                bool foundCond = false;

                for (int i = 0; i < dependencies.Length && !foundCond; i++)
                {
                    for (int j = 0; j < dependencies[i].Length; j++)
                    {
                        if (dependencies[i][j].Type == CharacterDependency.Types.Conditioned)
                        {
                            foundCond = true;
                            break;
                        }
                    }
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<StackPanel>("SetNameContainer").Children.Add(new TextBlock() { Text = "Overall: " + ml.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture), Margin = new Thickness(5, 30, 5, 5), FontWeight = FontWeight.Bold });
                    this.FindControl<StackPanel>("SetNameContainer").Children.Add(new TextBlock() { Text = "AIC: " + aic.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture), Margin = new Thickness(5, 0, 5, 5), FontWeight = FontWeight.Bold });
                    this.FindControl<StackPanel>("SetNameContainer").Children.Add(new TextBlock() { Text = "AICc: " + aicc.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture), Margin = new Thickness(5, 0, 5, 5), FontWeight = FontWeight.Bold });
                    this.FindControl<StackPanel>("SetNameContainer").Children.Add(new TextBlock() { Text = "BIC: " + bic.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture), Margin = new Thickness(5, 0, 5, 5), FontWeight = FontWeight.Bold });

                    this.FindControl<Button>("ViewMLRatesButton").IsVisible = true;

                    if (foundCond)
                    {
                        this.FindControl<Button>("ViewMLCondProbsButton").IsVisible = true;
                    }

                    this.FindControl<Spinner>("MLSamplingSpinner").IsVisible = false;
                    DisableAnimation("MLEstimationCanvas");
                });
            }
            else if (state == "StartComputingNodeProbs")
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<Grid>("NodeStatesGrid").IsVisible = true;
                    this.FindControl<ProgressBar>("ComputingProbsProgressBar").Value = 0;
                    EnableCanvas("NodeStatesCanvas");
                    EnableAnimation("NodeStatesCanvas");
                    awaitingScroll = true;
                });
            }
            else if (state == "NodeStateProbProgress")
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<ProgressBar>("ComputingProbsProgressBar").Value = (completedNodeProbs + (double)data[0]) / dependencies.Length;
                    this.FindControl<TextBlock>("ComputingProbsDesc").Text = ((completedNodeProbs + (double)data[0]) / dependencies.Length).ToString("0%", System.Globalization.CultureInfo.InvariantCulture);
                    if (awaitingScroll)
                    {
                        this.FindControl<ScrollViewer>("MainScrollViewer").ScrollToBottom();
                        awaitingScroll = false;
                    }
                });
            }
            else if (state == "NodeStateProbSetFinished")
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    completedNodeProbs++;
                    this.FindControl<ProgressBar>("ComputingProbsProgressBar").Value = (double)completedNodeProbs / dependencies.Length;
                    this.FindControl<TextBlock>("ComputingProbsDesc").Text = ((double)completedNodeProbs / dependencies.Length).ToString("0%", System.Globalization.CultureInfo.InvariantCulture);
                });
            }
            else if (state == "FinishedComputingNodeProbs")
            {
                meanPriors = (double[][][])data[0];
                meanLikelihoods = (double[][][])data[1];
                meanPosteriors = (double[][][])data[2];
                treeSamples = (int[])data[3];

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<StackPanel>("ComputingProbsInfo").IsVisible = false;
                    this.FindControl<StackPanel>("ViewNodeProbsButtons").IsVisible = true;
                    this.FindControl<Spinner>("NodeStatesSpinner").IsVisible = false;
                    DisableAnimation("NodeStatesCanvas");
                });

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<ScrollViewer>("MainScrollViewer").ScrollToBottom();
                });
            }
            else if (state == "StartedSimulatingHistories")
            {
                histories = new TaggedHistory[dependencies.Length][];
                stateProbs = new double[dependencies.Length][][];

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    EnableCanvas("SimulationsCanvas");
                    EnableAnimation("SimulationsCanvas");

                    this.FindControl<Grid>("SimulationsGrid").IsVisible = true;

                    this.FindControl<ProgressBar>("SimulationsProgressBar").Value = 0;
                    this.FindControl<TextBlock>("SimulationsDesc").Text = "0%";
                    awaitingScroll = true;
                });
            }
            else if (state == "SimulationsProgress")
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<ProgressBar>("SimulationsProgressBar").Value = (completedSimulations + (double)data[0]) / dependencies.Length;
                    this.FindControl<TextBlock>("SimulationsDesc").Text = ((completedSimulations + (double)data[0]) / dependencies.Length).ToString("0%", System.Globalization.CultureInfo.InvariantCulture);
                    if (awaitingScroll)
                    {
                        this.FindControl<ScrollViewer>("MainScrollViewer").ScrollToBottom();
                        awaitingScroll = false;
                    }
                });
            }
            else if (state == "SimulationStepFinished")
            {
                histories[completedSimulations] = (TaggedHistory[])data[0];
                stateProbs[completedSimulations] = (double[][])data[1];
                completedSimulations++;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<ProgressBar>("SimulationsProgressBar").Value = ((double)completedSimulations) / dependencies.Length;
                    this.FindControl<TextBlock>("SimulationsDesc").Text = (((double)completedSimulations) / dependencies.Length).ToString("0%", System.Globalization.CultureInfo.InvariantCulture);
                    if (awaitingScroll)
                    {
                        this.FindControl<ScrollViewer>("MainScrollViewer").ScrollToBottom();
                        awaitingScroll = false;
                    }
                });
            }
            else if (state == "FinishedSimulatingHistories")
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<StackPanel>("RunningSimulationsInfo").IsVisible = false;
                    this.FindControl<StackPanel>("SimulationsButtons").IsVisible = true;
                    this.FindControl<Spinner>("SimulationsSpinner").IsVisible = false;
                    DisableAnimation("SimulationsCanvas");
                });

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<ScrollViewer>("MainScrollViewer").ScrollToBottom();
                });
            }
            else if (state == "BayesianStarted")
            {
                lock (plotObject)
                {
                    bayesianSetItems = new List<string>();
                    burnInProgress = new List<double>();
                    estimateStepSizes = new List<bool>();
                    stepSizes = new List<double[]>();
                    realParamNames = new List<List<string>>();
                    samples = new List<List<(int, double[][])>[]>();

                    bayesianMeanCoVs = new List<double[]>();
                    bayesianSdCoVs = new List<double[]>();

                    bayesianEssss = new List<double[][]>();
                    bayesianMeanss = new List<double[][]>();
                    bayesianStdDevss = new List<double[][]>();

                    bayesianBinCountss = new List<int[][]>();
                    bayesianBinSampless = new List<int[][][]>();
                    bayesianRealBinWidthss = new List<double[][]>();
                    bayesianMaxBinss = new List<double[][]>();

                    bayesianSampleCounts = new List<int[]>();

                    bayesianConvergenceStats = new List<List<double[]>>();
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<ComboBox>("BayesianSetChoiceBox").Items = new List<ComboBoxItem>(from el in bayesianSetItems select new ComboBoxItem() { Content = el });

                    this.FindControl<StackPanel>("BayesianSamplingPanel").IsVisible = true;
                    EnableCanvas("BayesianSamplingCanvas");
                    EnableAnimation("BayesianSamplingCanvas");
                });

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<ScrollViewer>("MainScrollViewer").ScrollToBottom();
                });

                bayesianSampleReceived = new EventWaitHandle(false, EventResetMode.ManualReset);
                bayesianFinished = new EventWaitHandle(false, EventResetMode.ManualReset);
                bayesianPlotUpdateRequested = new EventWaitHandle(false, EventResetMode.ManualReset);

                Thread updateCacheThread = new Thread(() =>
                {
                    while (true)
                    {
                        int handle = WaitHandle.WaitAny(new WaitHandle[] { bayesianSampleReceived, bayesianFinished });

                        if (handle == 1)
                        {
                            break;
                        }
                        else if (handle == 0)
                        {
                            bayesianSampleReceived.Reset();

                            try
                            {
                                UpdateBayesianPlotCache();
                            }
                            catch
                            {

                            }

                            bayesianPlotUpdateRequested.Set();

                            Thread.Sleep(100);
                        }
                    }

                });

                updateCacheThread.Start();

                Thread updatePlotThread = new Thread(async () =>
                {
                    while (true)
                    {
                        int handle = WaitHandle.WaitAny(new WaitHandle[] { bayesianPlotUpdateRequested, bayesianFinished });

                        if (handle == 1)
                        {
                            break;
                        }
                        else if (handle == 0)
                        {
                            bayesianPlotUpdateRequested.Reset();

                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                int index = this.FindControl<ComboBox>("BayesianSetChoiceBox").SelectedIndex;
                                int selIndex = this.FindControl<ComboBox>("BayesianParameterChoiceBox").SelectedIndex;

                                this.FindControl<ComboBox>("BayesianParameterChoiceBox").SelectedIndex = Math.Max(0, Math.Min(selIndex, paramNames[index].Count));
                                selIndex = Math.Max(0, Math.Min(selIndex, paramNames[index].Count));

                                Canvas plotCan = this.FindControl<Canvas>("BayesianPlotContainer");
                                StackPanel statsContainer = this.FindControl<StackPanel>("BayesianStatsContainer");

                                plotCan.Children.Clear();
                                statsContainer.Children.Clear();

                                if (selIndex > 0)
                                {
                                    this.FindControl<TextBlock>("BayesianXLabel").Text = paramNames[index][selIndex - 1];
                                    this.FindControl<TextBlock>("BayesianYLabel").Text = "Density";

                                    PlotBayesianDistribution(index, selIndex, plotCan, statsContainer);
                                }
                                else
                                {
                                    this.FindControl<TextBlock>("BayesianXLabel").Text = "Step";
                                    this.FindControl<TextBlock>("BayesianYLabel").Text = "Value";

                                    PlotConvergenceStats(index, plotCan, statsContainer);
                                }
                            });

                            if (awaitingScroll)
                            {
                                awaitingScroll = false;
                                await Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    this.FindControl<ScrollViewer>("MainScrollViewer").ScrollToBottom();
                                });
                            }

                            Thread.Sleep(100);
                        }
                    }
                });

                updatePlotThread.Start();
            }
            else if (state == "BayesianSetStarted")
            {
                int runCount = (int)data[1];

                lock (plotObject)
                {
                    burnInProgress.Add(-1);

                    samples.Add(new List<(int, double[][])>[runCount]);

                    for (int i = 0; i < runCount; i++)
                    {
                        samples[samples.Count - 1][i] = new List<(int, double[][])>();
                    }

                    bayesianMeanCoVs.Add(new double[runCount]);
                    bayesianSdCoVs.Add(new double[runCount]);

                    bayesianEssss.Add(new double[runCount][]);
                    bayesianMeanss.Add(new double[runCount][]);
                    bayesianStdDevss.Add(new double[runCount][]);

                    bayesianBinCountss.Add(new int[runCount][]);
                    bayesianBinSampless.Add(new int[runCount][][]);
                    bayesianRealBinWidthss.Add(new double[runCount][]);
                    bayesianMaxBinss.Add(new double[runCount][]);

                    bayesianSampleCounts.Add(new int[runCount]);

                    bayesianConvergenceStats.Add(new List<double[]>());
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    bayesianSetItems.Add("Set " + ((int)data[0]).ToString());
                    this.FindControl<ComboBox>("BayesianSetChoiceBox").Items = new List<ComboBoxItem>(from el in bayesianSetItems select new ComboBoxItem() { Content = el });
                    this.FindControl<ComboBox>("BayesianSetChoiceBox").SelectedIndex = bayesianSetItems.Count - 1;
                });

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<ScrollViewer>("MainScrollViewer").ScrollToBottom();
                });
            }
            else if (state == "BurnInStarted")
            {
                if (!steppingStoneStarted)
                {
                    lock (plotObject)
                    {
                        estimateStepSizes.Add((bool)data[0]);
                        burnInProgress[burnInProgress.Count - 1] = 0;
                        realParamNames.Add((List<string>)data[1]);
                    }

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        this.FindControl<ComboBox>("BayesianSetChoiceBox").SelectedIndex = bayesianSetItems.Count - 1;
                        UpdateBayesianPanel();
                    });
                }
                else
                {
                    lock (plotObject)
                    {
                        steppingStoneburninProgress[steppingStoneburninProgress.Count - 1][steppingStoneStepItems.Last().Count - 1] = 0;
                    }

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        UpdateSteppingStonePanel(false);
                    });
                }
            }
            else if (state == "BurnInProgress")
            {
                if (!steppingStoneStarted)
                {
                    lock (plotObject)
                    {
                        burnInProgress[burnInProgress.Count - 1] = (double)data[0];
                    }

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        UpdateBayesianPanel();
                    });
                }
                else
                {
                    lock (plotObject)
                    {
                        steppingStoneburninProgress[steppingStoneburninProgress.Count - 1][steppingStoneStepItems.Last().Count - 1] = (double)data[0];
                    }

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        UpdateSteppingStonePanel(false);
                    });
                }
            }
            else if (state == "BurnInFinished")
            {
                if (!steppingStoneStarted)
                {
                    lock (plotObject)
                    {
                        stepSizes.Add((double[])data[0]);
                        burnInProgress[burnInProgress.Count - 1] = -2;
                    }

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        UpdateBayesianPanel();
                    });
                }
                else
                {
                    lock (plotObject)
                    {
                        steppingStoneStepSizes.Last()[steppingStoneStepItems.Last().Count - 1] = (double[])data[0];
                        steppingStoneburninProgress[steppingStoneburninProgress.Count - 1][steppingStoneStepItems.Last().Count - 1] = -2;
                    }

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        UpdateSteppingStonePanel(false);
                    });
                }
            }
            else if (state == "MCMCSamplingStarted")
            {
                if (!steppingStoneStarted)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        UpdateBayesianPanel();
                    });

                    awaitingScroll = true;
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        UpdateSteppingStonePanel(false);
                    });

                    awaitingScroll = true;
                }
            }
            else if (state == "MCMCSample")
            {
                if (!steppingStoneStarted)
                {
                    int runInd = (int)data[0];
                    (int, double[][], double) sample = ((int, double[][], double))data[1];

                    lock (plotObject)
                    {
                        samples[samples.Count - 1][runInd].Add((sample.Item1, sample.Item2));
                    }

                    bayesianSampleReceived.Set();
                }
                else
                {
                    int runInd = (int)data[0];
                    (int, double[][], double) sample = ((int, double[][], double))data[1];

                    List<double[]> realSample = new List<double[]>() { new double[] { sample.Item3 } };

                    realSample.AddRange(sample.Item2);

                    lock (plotObject)
                    {
                        steppingStoneSamples[steppingStoneSamples.Count - 1][steppingStoneStepItems.Last().Count - 1][runInd].Add((sample.Item1, realSample.ToArray()));
                    }

                    steppingStoneSampleReceived.Set();
                }
            }
            else if (state == "BayesianFinished")
            {
                bayesianFinished.Set();

                sampledParameters = (double[][][])data[0];

                parameterIndices = new Dictionary<string, int>[paramNames.Length];

                for (int i = 0; i < paramNames.Length; i++)
                {
                    parameterIndices[i] = new Dictionary<string, int>();

                    for (int j = 0; j < paramNames[i].Count; j++)
                    {
                        string dictName = paramNames[i][j];

                        if (dictName.Contains(":"))
                        {
                            string charName = dictName.Substring(0, dictName.IndexOf(":"));
                            dictName = dictName.Substring(dictName.IndexOf("(") + 1);
                            dictName = charName + ":" + dictName.Substring(0, dictName.IndexOf(")")).Replace(" ", "");
                        }

                        parameterIndices[i].Add(dictName, j - 1);
                    }
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<Button>("BayesianInterruptButton").IsVisible = false;
                    this.FindControl<StackPanel>("BayesianViewParametersPanel").IsVisible = true;
                    DisableAnimation("BayesianSamplingCanvas");
                    this.FindControl<Spinner>("BayesianSamplingSpinner").IsVisible = false;
                });
            }
            else if (state == "SteppingStoneStarted")
            {
                lock (plotObject)
                {
                    steppingStoneStarted = true;

                    steppingStoneSetItems = new List<string>();
                    steppingStoneStepItems = new List<List<string>>();
                    steppingStoneburninProgress = new List<double[]>();
                    steppingStoneEstimateStep = (bool)data[0];
                    steppingStoneSamples = new List<List<(int, double[][])>[][]>();
                    steppingStoneStepSizes = new List<double[][]>();


                    steppingStoneMeanCoVs = new List<List<double[]>>();
                    steppingStoneSdCoVs = new List<List<double[]>>();

                    steppingStoneEssss = new List<List<double[][]>>();
                    steppingStoneMeanss = new List<List<double[][]>>();
                    steppingStoneStdDevss = new List<List<double[][]>>();

                    steppingStoneBinCountss = new List<List<int[][]>>();
                    steppingStoneBinSampless = new List<List<int[][][]>>();
                    steppingStoneRealBinWidthss = new List<List<double[][]>>();
                    steppingStoneMaxBinss = new List<List<double[][]>>();

                    steppingStoneSampleCounts = new List<List<int[]>>();

                    steppingStoneConvergenceStats = new List<List<List<double[]>>>();
                    steppingStoneLikelihoodConvergenceStats = new List<List<List<double[]>>>();

                    steppingStoneContributions = new List<double[]>();
                }



                steppingStoneSampleReceived = new EventWaitHandle(false, EventResetMode.ManualReset);
                steppingStoneFinished = new EventWaitHandle(false, EventResetMode.ManualReset);
                steppingStonePlotUpdateRequested = new EventWaitHandle(false, EventResetMode.ManualReset);

                Thread updateCacheThread = new Thread(() =>
                {
                    while (true)
                    {
                        int handle = WaitHandle.WaitAny(new WaitHandle[] { steppingStoneSampleReceived, steppingStoneFinished });

                        if (handle == 1)
                        {
                            break;
                        }
                        else if (handle == 0)
                        {
                            steppingStoneSampleReceived.Reset();

                            try
                            {
                                UpdateSteppingStonePlotCache();
                            }
                            catch
                            {

                            }

                            Thread.Sleep(100);

                            steppingStonePlotUpdateRequested.Set();
                        }
                    }

                });

                updateCacheThread.Start();

                Thread updatePlotThread = new Thread(async () =>
                {
                    while (true)
                    {
                        int handle = WaitHandle.WaitAny(new WaitHandle[] { steppingStonePlotUpdateRequested, steppingStoneFinished });

                        if (handle == 1)
                        {
                            break;
                        }
                        else if (handle == 0)
                        {
                            steppingStonePlotUpdateRequested.Reset();

                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {


                                int index;
                                int stepIndex;
                                int selIndex;

                                lock (plotObject)
                                {
                                    index = this.FindControl<ComboBox>("SteppingStoneSetChoiceBox").SelectedIndex;
                                    stepIndex = this.FindControl<ComboBox>("SteppingStoneStepChoiceBox").SelectedIndex;
                                    selIndex = this.FindControl<ComboBox>("SteppingStoneParameterChoiceBox").SelectedIndex;


                                    this.FindControl<ComboBox>("SteppingStoneParameterChoiceBox").SelectedIndex = Math.Max(0, Math.Min(selIndex, paramNames[index].Count));
                                    selIndex = Math.Max(0, Math.Min(selIndex, paramNames[index].Count));
                                }


                                Canvas plotCan = this.FindControl<Canvas>("SteppingStonePlotContainer");
                                StackPanel statsContainer = this.FindControl<StackPanel>("SteppingStoneStatsContainer");

                                plotCan.Children.Clear();
                                statsContainer.Children.Clear();

                                if (selIndex > 0)
                                {
                                    if (selIndex > 1)
                                    {
                                        this.FindControl<TextBlock>("SteppingStoneXLabel").Text = paramNames[index][selIndex - 2];
                                        this.FindControl<TextBlock>("SteppingStoneYLabel").Text = "Density";
                                    }
                                    else
                                    {
                                        this.FindControl<TextBlock>("SteppingStoneXLabel").Text = "Log-Likelihood";
                                        this.FindControl<TextBlock>("SteppingStoneYLabel").Text = "Density";
                                    }

                                    try
                                    {
                                        PlotSteppingStoneDistribution(index, stepIndex, selIndex, plotCan, statsContainer);
                                    }
                                    catch
                                    {

                                    }
                                }
                                else
                                {
                                    this.FindControl<TextBlock>("SteppingStoneXLabel").Text = "Step";
                                    this.FindControl<TextBlock>("SteppingStoneYLabel").Text = "Value";

                                    PlotSteppingStoneConvergenceStats(index, stepIndex, plotCan, statsContainer);
                                }
                            });

                            if (awaitingScroll)
                            {
                                awaitingScroll = false;
                                await Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    this.FindControl<ScrollViewer>("MainScrollViewer").ScrollToBottom();
                                });
                            }

                            Thread.Sleep(100);
                        }
                    }
                });

                updatePlotThread.Start();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    lock (plotObject)
                    {
                        this.FindControl<ComboBox>("SteppingStoneSetChoiceBox").Items = new List<ComboBoxItem>(from el in steppingStoneSetItems select new ComboBoxItem() { Content = el });
                    }

                    this.FindControl<StackPanel>("SteppingStoneSamplingPanel").IsVisible = true;
                    EnableCanvas("SteppingStoneSamplingCanvas");
                    EnableAnimation("SteppingStoneSamplingCanvas");
                });

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<ScrollViewer>("MainScrollViewer").ScrollToBottom();
                });
            }
            else if (state == "SteppingStoneSetStarted")
            {
                lock (plotObject)
                {
                    steppingStoneTotalSteps = (int)data[1];
                    steppingStoneStepItems.Add(new List<string>());
                    steppingStoneburninProgress.Add(new double[steppingStoneTotalSteps]);

                    steppingStoneSamples.Add(new List<(int, double[][])>[steppingStoneTotalSteps][]);
                    steppingStoneStepSizes.Add(new double[steppingStoneTotalSteps][]);

                    steppingStoneMeanCoVs.Add(new List<double[]>());
                    steppingStoneSdCoVs.Add(new List<double[]>());

                    steppingStoneEssss.Add(new List<double[][]>());
                    steppingStoneMeanss.Add(new List<double[][]>());
                    steppingStoneStdDevss.Add(new List<double[][]>());

                    steppingStoneBinCountss.Add(new List<int[][]>());
                    steppingStoneBinSampless.Add(new List<int[][][]>());
                    steppingStoneRealBinWidthss.Add(new List<double[][]>());
                    steppingStoneMaxBinss.Add(new List<double[][]>());

                    steppingStoneSampleCounts.Add(new List<int[]>());

                    steppingStoneConvergenceStats.Add(new List<List<double[]>>());
                    steppingStoneLikelihoodConvergenceStats.Add(new List<List<double[]>>());

                    steppingStoneContributions.Add(new double[steppingStoneTotalSteps]);
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    steppingStoneSetItems.Add("Set " + ((int)data[0]).ToString());

                    lock (plotObject)
                    {
                        this.FindControl<ComboBox>("SteppingStoneSetChoiceBox").Items = new List<ComboBoxItem>(from el in steppingStoneSetItems select new ComboBoxItem() { Content = el });
                        this.FindControl<ComboBox>("SteppingStoneSetChoiceBox").SelectedIndex = steppingStoneSetItems.Count - 1;
                    }
                });

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<ScrollViewer>("MainScrollViewer").ScrollToBottom();
                });
            }
            else if (state == "SteppingStoneStepStarted")
            {
                int index = (int)data[0];

                int runCount = (int)data[1];

                int stepIndex = (int)data[2] - 1;

                lock (plotObject)
                {
                    steppingStoneStepItems[index].Add("Step " + (stepIndex + 1).ToString());

                    steppingStoneburninProgress[index][stepIndex] = -1;

                    steppingStoneSamples[index][stepIndex] = new List<(int, double[][])>[runCount];

                    for (int i = 0; i < runCount; i++)
                    {
                        steppingStoneSamples[index][stepIndex][i] = new List<(int, double[][])>();
                    }


                    steppingStoneMeanCoVs[index].Add(new double[runCount]);
                    steppingStoneSdCoVs[index].Add(new double[runCount]);

                    steppingStoneEssss[index].Add(new double[runCount][]);
                    steppingStoneMeanss[index].Add(new double[runCount][]);
                    steppingStoneStdDevss[index].Add(new double[runCount][]);

                    steppingStoneBinCountss[index].Add(new int[runCount][]);
                    steppingStoneBinSampless[index].Add(new int[runCount][][]);
                    steppingStoneRealBinWidthss[index].Add(new double[runCount][]);
                    steppingStoneMaxBinss[index].Add(new double[runCount][]);

                    steppingStoneSampleCounts[index].Add(new int[runCount]);

                    steppingStoneConvergenceStats[index].Add(new List<double[]>());
                    steppingStoneLikelihoodConvergenceStats[index].Add(new List<double[]>());
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!updatingSteppingStonePanel)
                    {
                        UpdateSteppingStonePanel(true);
                    }
                });

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<ScrollViewer>("MainScrollViewer").ScrollToBottom();
                });

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<ScrollViewer>("MainScrollViewer").ScrollToBottom();
                });
            }
            else if (state == "SteppingStoneStepFinished")
            {
                lock (plotObject)
                {
                    steppingStoneContributions[(int)data[0]][(int)data[1]] = (double)data[2];
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!updatingSteppingStonePanel)
                    {
                        UpdateSteppingStonePanel(false);
                    }
                });
            }
            else if (state == "SteppingStoneSetFinished")
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Grid grd = this.FindControl<Grid>("MarginalLikelihoodsContainerGrid");

                    grd.IsVisible = true;
                    grd.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));

                    {
                        TextBlock blk = new TextBlock() { Text = "Set " + ((int)data[0]).ToString() + ": ", Margin = new Thickness(42, 5, 5, 0), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                        Grid.SetRow(blk, (int)data[0] + 1);
                        grd.Children.Add(blk);
                    }

                    {
                        TextBlock blk = new TextBlock() { Text = ((double)data[1]).ToString(3), Margin = new Thickness(0, 5, 5, 0), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                        Grid.SetRow(blk, (int)data[0] + 1);
                        Grid.SetColumn(blk, 1);
                        grd.Children.Add(blk);
                    }
                });
            }
            else if (state == "SteppingStoneFinished")
            {
                steppingStoneFinished.Set();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<Button>("SteppingStoneInterruptButton").IsVisible = false;

                    DisableAnimation("SteppingStoneSamplingCanvas");
                    this.FindControl<Spinner>("SteppingStoneSamplingSpinner").IsVisible = false;


                    Grid grd = this.FindControl<Grid>("MarginalLikelihoodsContainerGrid");

                    grd.IsVisible = true;
                    grd.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));

                    {
                        TextBlock blk = new TextBlock() { Text = "Overall: ", Margin = new Thickness(32, 10, 5, 0), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, FontWeight = FontWeight.Bold };
                        Grid.SetRow(blk, grd.RowDefinitions.Count - 1);
                        grd.Children.Add(blk);
                    }

                    {
                        TextBlock blk = new TextBlock() { Text = ((double)data[0]).ToString(3), Margin = new Thickness(0, 10, 5, 0), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                        Grid.SetRow(blk, grd.RowDefinitions.Count - 1);
                        Grid.SetColumn(blk, 1);
                        grd.Children.Add(blk);
                    }
                });
            }
            else if (state == "SerializedRun")
            {
                FinishedRuns[(int)data[0]] = (SerializedRun)data[1];
            }
            else if (state == "AllFinished")
            {
                if (CloseOnceFinished)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        this.Close();
                    });
                }

                if (PromptAtCompletion)
                {
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        await new MessageBox("Completed!", "All done! When you are ready, close the window to proceed.", MessageBox.MessageBoxButtonTypes.OK, MessageBox.MessageBoxIconTypes.Tick).ShowDialog(this);
                    });
                }
            }
        }

        bool updatingSteppingStonePanel = false;

        private void UpdateSteppingStonePanel(bool moveToLastStep)
        {
            updatingSteppingStonePanel = true;


            int index;
            lock (plotObject)
            {
                index = this.FindControl<ComboBox>("SteppingStoneSetChoiceBox").SelectedIndex;
            }

            ComboBox stepBox = this.FindControl<ComboBox>("SteppingStoneStepChoiceBox");

            int stepIndex = stepBox.SelectedIndex;

            lock (plotObject)
            {
                stepBox.Items = new List<ComboBoxItem>(from el in steppingStoneStepItems[index] select new ComboBoxItem() { Content = el });
            }

            if (moveToLastStep)
            {
                stepBox.SelectedIndex = steppingStoneStepItems[index].Count - 1;
                stepIndex = steppingStoneStepItems[index].Count - 1;
            }

            stepBox.SelectedIndex = Math.Max(0, Math.Min(stepIndex, steppingStoneStepItems[index].Count - 1));

            stepIndex = Math.Max(0, Math.Min(stepIndex, steppingStoneStepItems[index].Count - 1));

            if (steppingStoneStepItems[index].Count > 0 && (steppingStoneStepItems[index].Count < steppingStoneTotalSteps || (index == steppingStoneSetItems.Count - 1 && !steppingStoneFinished.WaitOne(0))))
            {
                this.FindControl<StackPanel>("SteppingStoneStepProgressPanel").IsVisible = true;
                this.FindControl<ProgressBar>("SteppingStoneStepProgressBar").Maximum = steppingStoneTotalSteps;
                this.FindControl<ProgressBar>("SteppingStoneStepProgressBar").Value = steppingStoneStepItems[index].Count;

                this.FindControl<TextBlock>("SteppingStoneStepDesc").Text = steppingStoneStepItems[index].Count.ToString() + " / " + steppingStoneTotalSteps.ToString();
            }
            else
            {
                this.FindControl<StackPanel>("SteppingStoneStepProgressPanel").IsVisible = true;
            }

            if (steppingStoneburninProgress[index][stepIndex] == -1)
            {
                this.FindControl<StackPanel>("SteppingStoneBurnInProgressPanel").IsVisible = false;
                this.FindControl<Button>("SteppingStoneViewStepSizesButton").IsVisible = false;
                this.FindControl<StackPanel>("SteppingStoneParameterChoicePanel").IsVisible = false;
                this.FindControl<Grid>("SteppingStonePlotGrid").IsVisible = false;
                this.FindControl<TextBlock>("SteppingStoneContribution").IsVisible = false;
            }
            else if (steppingStoneburninProgress[index][stepIndex] >= 0)
            {
                this.FindControl<StackPanel>("SteppingStoneBurnInProgressPanel").IsVisible = true;
                this.FindControl<Button>("SteppingStoneViewStepSizesButton").IsVisible = false;
                this.FindControl<StackPanel>("SteppingStoneParameterChoicePanel").IsVisible = false;
                this.FindControl<TextBlock>("SteppingStoneBurnInName").Text = estimateStepSizes[index] ? "Burn-in and step sizes..." : "Burn-in...";
                this.FindControl<ProgressBar>("SteppingStoneBurnInProgressBar").Value = steppingStoneburninProgress[index][stepIndex];
                this.FindControl<TextBlock>("SteppingStoneBurnInDesc").Text = steppingStoneburninProgress[index][stepIndex].ToString("0%", System.Globalization.CultureInfo.InvariantCulture);
                this.FindControl<Grid>("SteppingStonePlotGrid").IsVisible = false;
                this.FindControl<TextBlock>("SteppingStoneContribution").IsVisible = false;
            }
            else if (steppingStoneburninProgress[index][stepIndex] == -2)
            {
                this.FindControl<StackPanel>("SteppingStoneBurnInProgressPanel").IsVisible = false;
                this.FindControl<Button>("SteppingStoneViewStepSizesButton").IsVisible = true;
                this.FindControl<StackPanel>("SteppingStoneParameterChoicePanel").IsVisible = true;

                ComboBox box = this.FindControl<ComboBox>("SteppingStoneParameterChoiceBox");

                int selIndex = box.SelectedIndex;

                List<ComboBoxItem> items = new List<ComboBoxItem>() { new ComboBoxItem() { Content = "Convergence stats" }, new ComboBoxItem() { Content = "Log-Likelihood" } };
                items.AddRange(from el in paramNames[index] select new ComboBoxItem() { Content = el });
                box.Items = items;
                box.SelectedIndex = Math.Max(0, Math.Min(selIndex, paramNames[index].Count + 1));

                selIndex = Math.Max(0, Math.Min(selIndex, paramNames[index].Count + 1));

                lock (plotObject)
                {
                    if (steppingStoneSamples.Count > index && steppingStoneSamples[index][stepIndex] != null)
                    {
                        if (steppingStoneContributions[index][stepIndex] != 0)
                        {
                            this.FindControl<TextBlock>("SteppingStoneContribution").Text = "Contribution to log marginal likelihood: " + steppingStoneContributions[index][stepIndex].ToString(3);
                            this.FindControl<TextBlock>("SteppingStoneContribution").IsVisible = true;
                        }
                        else
                        {
                            this.FindControl<TextBlock>("SteppingStoneContribution").IsVisible = false;
                        }

                        this.FindControl<Grid>("SteppingStonePlotGrid").IsVisible = true;

                        if (!steppingStoneFinished.WaitOne(0))
                        {
                            steppingStonePlotUpdateRequested.Set();
                        }
                        else
                        {
                            Canvas plotCan = this.FindControl<Canvas>("SteppingStonePlotContainer");
                            StackPanel statsContainer = this.FindControl<StackPanel>("SteppingStoneStatsContainer");

                            plotCan.Children.Clear();
                            statsContainer.Children.Clear();

                            if (selIndex > 0)
                            {
                                if (selIndex > 1)
                                {
                                    this.FindControl<TextBlock>("SteppingStoneXLabel").Text = paramNames[index][selIndex - 2];
                                    this.FindControl<TextBlock>("SteppingStoneYLabel").Text = "Density";
                                }
                                else
                                {
                                    this.FindControl<TextBlock>("SteppingStoneXLabel").Text = "Log-Likelihood";
                                    this.FindControl<TextBlock>("SteppingStoneYLabel").Text = "Density";
                                }

                                try
                                {
                                    PlotSteppingStoneDistribution(index, stepIndex, selIndex, plotCan, statsContainer);
                                }
                                catch
                                {

                                }
                            }
                            else
                            {
                                this.FindControl<TextBlock>("SteppingStoneXLabel").Text = "Step";
                                this.FindControl<TextBlock>("SteppingStoneYLabel").Text = "Value";

                                PlotSteppingStoneConvergenceStats(index, stepIndex, plotCan, statsContainer);
                            }
                        }

                    }
                    else
                    {
                        this.FindControl<Grid>("SteppingStonePlotGrid").IsVisible = false;
                    }
                }
            }

            updatingSteppingStonePanel = false;
        }

        private void SteppingStoneChoiceChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!updatingSteppingStonePanel)
            {
                UpdateSteppingStonePanel(false);
            }
        }

        bool updatingBayesianPanel = false;

        private void UpdateBayesianPanel()
        {
            updatingBayesianPanel = true;

            int index = this.FindControl<ComboBox>("BayesianSetChoiceBox").SelectedIndex;

            if (burnInProgress[index] == -1)
            {
                this.FindControl<StackPanel>("BurnInProgressPanel").IsVisible = false;
                this.FindControl<Button>("ViewStepSizesButton").IsVisible = false;
                this.FindControl<StackPanel>("BayesianParameterChoicePanel").IsVisible = false;
                this.FindControl<Grid>("BayesianPlotGrid").IsVisible = false;
            }
            else if (burnInProgress[index] >= 0)
            {
                this.FindControl<StackPanel>("BurnInProgressPanel").IsVisible = true;
                this.FindControl<Button>("ViewStepSizesButton").IsVisible = false;
                this.FindControl<StackPanel>("BayesianParameterChoicePanel").IsVisible = false;
                this.FindControl<TextBlock>("BurnInName").Text = estimateStepSizes[index] ? "Burn-in and step sizes..." : "Burn-in...";
                this.FindControl<ProgressBar>("BurnInProgressBar").Value = burnInProgress[index];
                this.FindControl<TextBlock>("BurnInDesc").Text = burnInProgress[index].ToString("0%", System.Globalization.CultureInfo.InvariantCulture);
                this.FindControl<Grid>("BayesianPlotGrid").IsVisible = false;
            }
            else if (burnInProgress[index] == -2)
            {
                this.FindControl<StackPanel>("BurnInProgressPanel").IsVisible = false;
                this.FindControl<Button>("ViewStepSizesButton").IsVisible = true;
                this.FindControl<StackPanel>("BayesianParameterChoicePanel").IsVisible = true;

                ComboBox box = this.FindControl<ComboBox>("BayesianParameterChoiceBox");

                int selIndex = box.SelectedIndex;

                List<ComboBoxItem> items = new List<ComboBoxItem>() { new ComboBoxItem() { Content = "Convergence stats" } };
                items.AddRange(from el in paramNames[index] select new ComboBoxItem() { Content = el });
                box.Items = items;
                box.SelectedIndex = Math.Max(0, Math.Min(selIndex, paramNames[index].Count));

                selIndex = Math.Max(0, Math.Min(selIndex, paramNames[index].Count));

                lock (plotObject)
                {
                    if (samples.Count > index)
                    {
                        this.FindControl<Grid>("BayesianPlotGrid").IsVisible = true;

                        if (!bayesianFinished.WaitOne(0))
                        {
                            bayesianPlotUpdateRequested.Set();
                        }
                        else
                        {
                            Canvas plotCan = this.FindControl<Canvas>("BayesianPlotContainer");
                            StackPanel statsContainer = this.FindControl<StackPanel>("BayesianStatsContainer");

                            plotCan.Children.Clear();
                            statsContainer.Children.Clear();

                            if (selIndex > 0)
                            {
                                this.FindControl<TextBlock>("BayesianXLabel").Text = paramNames[index][selIndex - 1];
                                this.FindControl<TextBlock>("BayesianYLabel").Text = "Density";

                                PlotBayesianDistribution(index, selIndex, plotCan, statsContainer);
                            }
                            else
                            {
                                this.FindControl<TextBlock>("BayesianXLabel").Text = "Step";
                                this.FindControl<TextBlock>("BayesianYLabel").Text = "Value";

                                PlotConvergenceStats(index, plotCan, statsContainer);
                            }
                        }

                    }
                    else
                    {
                        this.FindControl<Grid>("BayesianPlotGrid").IsVisible = false;
                    }
                }
            }

            updatingBayesianPanel = false;
        }

        private async void ViewStepSizesClicked(object sender, RoutedEventArgs e)
        {
            int index = this.FindControl<ComboBox>("BayesianSetChoiceBox").SelectedIndex;

            ViewStepSizesWindow win = new ViewStepSizesWindow(stepSizes[index], paramNames[index], realParamNames[index], ViewStepSizesWindow.WindowType.BlueHeader);
            await win.ShowDialog(this);
        }

        private async void SteppingStoneViewStepSizesClicked(object sender, RoutedEventArgs e)
        {
            int index;
            int stepIndex;

            lock (plotObject)
            {
                index = this.FindControl<ComboBox>("SteppingStoneSetChoiceBox").SelectedIndex;
                stepIndex = this.FindControl<ComboBox>("SteppingStoneStepChoiceBox").SelectedIndex;

            }


            ViewStepSizesWindow win = new ViewStepSizesWindow(steppingStoneStepSizes[index][stepIndex], paramNames[index], realParamNames[index], ViewStepSizesWindow.WindowType.GreenHeader);
            await win.ShowDialog(this);
        }

        private void BayesianSetChoiceChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!updatingBayesianPanel)
            {
                UpdateBayesianPanel();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            DisableCanvas("DataParsingCanvas");
            DisableCanvas("MLEstimationCanvas");
            DisableCanvas("BayesianSamplingCanvas");
            DisableCanvas("SteppingStoneSamplingCanvas");
            DisableCanvas("NodeStatesCanvas");
            DisableCanvas("SimulationsCanvas");
        }

        private void DisableCanvas(string name)
        {
            if (name == "DataParsingCanvas")
            {
                this.FindControl<Canvas>("MarginCanvasLeft").Opacity = 0.15;
            }
            else if (name == "SimulationsCanvas")
            {
                this.FindControl<Canvas>("MarginCanvasRight").Opacity = 0.15;
            }

            this.FindControl<Canvas>(name).Children[0].Opacity = 0.15;
            this.FindControl<Canvas>(name).Children[2].Opacity = 0.5;
            ((TextBlock)this.FindControl<Canvas>(name).Children[2]).Foreground = ((Path)this.FindControl<Canvas>(name).Children[0]).Fill;
        }

        private void EnableCanvas(string name)
        {
            if (name == "DataParsingCanvas")
            {
                this.FindControl<Canvas>("MarginCanvasLeft").Opacity = 1;
            }
            else if (name == "SimulationsCanvas")
            {
                this.FindControl<Canvas>("MarginCanvasRight").Opacity = 1;
            }

            this.FindControl<Canvas>(name).Children[0].Opacity = 1;
            this.FindControl<Canvas>(name).Children[2].Opacity = 1;
            ((TextBlock)this.FindControl<Canvas>(name).Children[2]).Foreground = new SolidColorBrush(Colors.White);
        }

        private void EnableAnimation(string name)
        {
            this.FindControl<Canvas>(name).Children[1].Classes.Remove("AnimationInactive");

            if (this.FindControl<Canvas>(name).Width < 150)
            {
                this.FindControl<Canvas>(name).Children[1].Classes.Add("ShortAnimationActive");
            }
            else
            {
                this.FindControl<Canvas>(name).Children[1].Classes.Add("LongAnimationActive");
            }
        }

        private void DisableAnimation(string name)
        {
            this.FindControl<Canvas>(name).Children[1].Classes.Remove("ShortAnimationActive");
            this.FindControl<Canvas>(name).Children[1].Classes.Remove("LongAnimationActive");
            this.FindControl<Canvas>(name).Children[1].Classes.Add("AnimationInactive");
        }

        private async void ViewParsedDataClicked(object sender, RoutedEventArgs e)
        {
            ViewDataWindow win = new ViewDataWindow(parsedData);

            await win.ShowDialog(this);
        }

        private async void ViewMeanTreeClicked(object sender, RoutedEventArgs e)
        {
            ViewTreeWindow win = new ViewTreeWindow(meanTree);
            await win.ShowDialog(this);
        }

        private async void ViewDependenciesClicked(object sender, RoutedEventArgs e)
        {
            ViewDependenciesWindow win = new ViewDependenciesWindow(inputDependencies);
            await win.ShowDialog(this);
        }

        private async void ViewPiClicked(object sender, RoutedEventArgs e)
        {
            ViewPiWindow win = new ViewPiWindow(charData.States, dependencies, pi);
            await win.ShowDialog(this);
        }

        private async void ViewCondProbsClicked(object sender, RoutedEventArgs e)
        {
            ViewPiWindow win = new ViewPiWindow(charData.States, dependencies, pi, true);
            win.Header = "Conditioned probabilities";
            win.Title = "View conditioned probabilities";
            await win.ShowDialog(this);
        }

        private async void ViewRatesClicked(object sender, RoutedEventArgs e)
        {
            ViewRatesWindow win = new ViewRatesWindow(charData.States, dependencies, rates, new SolidColorBrush(Color.FromArgb(255, 163, 73, 164)));
            await win.ShowDialog(this);
        }

        private async void ViewMLRatesClicked(object sender, RoutedEventArgs e)
        {
            Dictionary<string, Parameter>[] MLRates = new Dictionary<string, Parameter>[rates.Length];

            for (int i = 0; i < rates.Length; i++)
            {
                MLRates[i] = Parameter.CloneParameterDictionary(rates[i]);

                foreach (KeyValuePair<string, Parameter> kvp in MLRates[i])
                {
                    if (kvp.Value.Action == Parameter.ParameterAction.ML)
                    {
                        kvp.Value.Action = Parameter.ParameterAction.Fix;
                    }
                }
            }

            ViewRatesWindow win = new ViewRatesWindow(charData.States, dependencies, MLRates, new SolidColorBrush(Color.FromArgb(255, 63, 72, 204)));
            await win.ShowDialog(this);
        }

        private async void ViewMLCondProbsClicked(object sender, RoutedEventArgs e)
        {
            CharacterDependency[][] clonedDeps = new CharacterDependency[dependencies.Length][];

            for (int i = 0; i < dependencies.Length; i++)
            {
                clonedDeps[i] = new CharacterDependency[dependencies[i].Length];

                for (int j = 0; j < dependencies[i].Length; j++)
                {
                    clonedDeps[i][j] = new CharacterDependency(dependencies[i][j].Index, dependencies[i][j].Type, dependencies[i][j].Dependencies == null ? null : (int[])dependencies[i][j].Dependencies.Clone(), dependencies[i][j].ConditionedProbabilities == null ? null : Parameter.CloneParameterDictionary(dependencies[i][j].ConditionedProbabilities));

                    clonedDeps[i][j].InputDependencyName = dependencies[i][j].InputDependencyName;

                    if (clonedDeps[i][j].Type == CharacterDependency.Types.Conditioned)
                    {
                        foreach (KeyValuePair<string, Parameter> kvp in clonedDeps[i][j].ConditionedProbabilities)
                        {
                            kvp.Value.Action = Parameter.ParameterAction.Fix;
                        }
                    }
                }
            }

            ViewPiWindow win = new ViewPiWindow(charData.States, clonedDeps, pi, true);
            win.Header = "Conditioned probabilities";
            win.Title = "View conditioned probabilities";
            await win.ShowDialog(this);
        }

        private async void ViewPriorsClicked(object sender, RoutedEventArgs e)
        {
            string[][] realStates = new string[dependencies.Length][];

            for (int i = 0; i < dependencies.Length; i++)
            {
                List<string[]> currStates = new List<string[]>();

                for (int j = 0; j < dependencies[i].Length; j++)
                {
                    currStates.Add(charData.States[dependencies[i][j].Index]);
                }

                realStates[i] = (from el in Utils.Utils.GetCombinations(currStates.ToArray()) select Utils.Utils.StringifyArray(el)).ToArray();
            }

            ViewPriorsWindow win = new ViewPriorsWindow(meanTree, realStates, meanPriors, ViewPriorsWindow.ProbWindowType.Priors);
            await win.ShowDialog(this);
        }

        private async void ViewLikelihoodsClicked(object sender, RoutedEventArgs e)
        {
            string[][] realStates = new string[dependencies.Length][];

            for (int i = 0; i < dependencies.Length; i++)
            {
                List<string[]> currStates = new List<string[]>();

                for (int j = 0; j < dependencies[i].Length; j++)
                {
                    currStates.Add(charData.States[dependencies[i][j].Index]);
                }

                realStates[i] = (from el in Utils.Utils.GetCombinations(currStates.ToArray()) select Utils.Utils.StringifyArray(el)).ToArray();
            }

            ViewPriorsWindow win = new ViewPriorsWindow(meanTree, realStates, meanLikelihoods, ViewPriorsWindow.ProbWindowType.Likelihoods);
            await win.ShowDialog(this);
        }

        private async void ViewPosteriorsClicked(object sender, RoutedEventArgs e)
        {
            string[][] realStates = new string[dependencies.Length][];

            for (int i = 0; i < dependencies.Length; i++)
            {
                List<string[]> currStates = new List<string[]>();

                for (int j = 0; j < dependencies[i].Length; j++)
                {
                    currStates.Add(charData.States[dependencies[i][j].Index]);
                }

                realStates[i] = (from el in Utils.Utils.GetCombinations(currStates.ToArray()) select Utils.Utils.StringifyArray(el)).ToArray();
            }

            ViewPriorsWindow win = new ViewPriorsWindow(meanTree, realStates, meanPosteriors, ViewPriorsWindow.ProbWindowType.Posteriors);
            await win.ShowDialog(this);
        }

        private async void ViewSMapClicked(object sender, RoutedEventArgs e)
        {
            string[][] realStates = new string[dependencies.Length][];

            for (int i = 0; i < dependencies.Length; i++)
            {
                List<string[]> currStates = new List<string[]>();

                for (int j = 0; j < dependencies[i].Length; j++)
                {
                    currStates.Add(charData.States[dependencies[i][j].Index]);
                }

                realStates[i] = (from el in Utils.Utils.GetCombinations(currStates.ToArray()) select Utils.Utils.StringifyArray(el)).ToArray();
            }


            ViewSMap win = new ViewSMap(histories, meanTree, realStates, stateProbs, treeSamples, likModels, meanLikModel, meanNodeCorresp, ViewSMap.SMapWindowType.SMap);
            await win.ShowDialog(this);
        }


        private async void ViewSampleSizesClicked(object sender, RoutedEventArgs e)
        {
            ViewSMap win = new ViewSMap(histories, meanTree, charData.States, stateProbs, treeSamples, likModels, meanLikModel, meanNodeCorresp, ViewSMap.SMapWindowType.SampleSizes);
            await win.ShowDialog(this);
        }

        private void BayesianInterrupt(object sender, RoutedEventArgs e)
        {
            foreach (KeyValuePair<string, ConsoleCancelEventHandler> h in MCMC.cancelEventHandlers)
            {
                h.Value.Invoke(h.Value.Target, null);
            }
        }

        private async void ViewBayesianPisClicked(object sender, RoutedEventArgs e)
        {
            ViewPiWindow win = new ViewPiWindow(charData, dependencies, pi, parameterIndices, sampledParameters);
            await win.ShowDialog(this);
        }

        private async void ViewBayesianCondProbsClicked(object sender, RoutedEventArgs e)
        {
            ViewPiWindow win = new ViewPiWindow(charData, dependencies, pi, parameterIndices, sampledParameters, true);
            win.Header = "Conditioned probabilities";
            win.Title = "View conditioned probabilities";
            await win.ShowDialog(this);
        }

        private async void ViewBayesianRatesClicked(object sender, RoutedEventArgs e)
        {
            Dictionary<string, Parameter>[] MLRates = new Dictionary<string, Parameter>[rates.Length];

            for (int i = 0; i < rates.Length; i++)
            {
                MLRates[i] = Parameter.CloneParameterDictionary(rates[i]);

                foreach (KeyValuePair<string, Parameter> kvp in MLRates[i])
                {
                    if (kvp.Value.Action == Parameter.ParameterAction.ML)
                    {
                        kvp.Value.Action = Parameter.ParameterAction.Fix;
                    }
                }
            }

            ViewRatesWindow win = new ViewRatesWindow(charData, dependencies, MLRates, new SolidColorBrush(Color.FromArgb(255, 0, 162, 232)), parameterIndices, sampledParameters);
            await win.ShowDialog(this);
        }
    }
}
