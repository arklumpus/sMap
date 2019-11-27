using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;
using Utils;
using System.Linq;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Input;
using System;
using System.Drawing;
using System.IO;
using MathNet.Numerics.Distributions;
using System.Threading;
using System.Drawing.Drawing2D;
using Avalonia.Threading;
using System.Threading.Tasks;

namespace sMap_GUI
{
    public class EditMLStrategyWindow : Window
    {
        public EditMLStrategyWindow()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        List<MaximisationStrategy> strategies = new List<MaximisationStrategy>();

        public EditMLStrategyWindow(string currentStrategy)
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            strategies = new List<MaximisationStrategy>(from el in currentStrategy.Split('|') select MaximisationStrategy.Parse(el));

            BuildStrategyList();
        }

        int selectedIndex = 0;

        public string result = null;

        void BuildStrategyList()
        {
            initialised = false;

            this.FindControl<StackPanel>("CurrentStrategyPanel").Children.Clear();

            this.FindControl<Grid>("SamplingGrid").IsVisible = false;
            this.FindControl<Grid>("IterativeSamplingGrid").IsVisible = false;
            this.FindControl<Grid>("RandomWalkGrid").IsVisible = false;
            this.FindControl<Grid>("NesterovClimbingGrid").IsVisible = false;

            for (int i = 0; i < strategies.Count; i++)
            {
                Grid grd = new Grid();
                grd.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(23, GridUnitType.Pixel) });
                grd.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(23, GridUnitType.Pixel) });
                grd.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(28, GridUnitType.Pixel) });
                grd.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

                if (i == selectedIndex)
                {
                    grd.Background = new SolidColorBrush(Avalonia.Media.Color.FromArgb(255, 220, 220, 220));

                    switch (strategies[i].Strategy)
                    {
                        case Strategies.Sampling:
                            this.FindControl<NumericUpDown>("SamplingMinBox").Value = ((Sampling)strategies[i]).Min;
                            this.FindControl<NumericUpDown>("SamplingMaxBox").Value = ((Sampling)strategies[i]).Max;
                            this.FindControl<NumericUpDown>("SamplingResolutionBox").Value = ((Sampling)strategies[i]).Resolution;
                            this.FindControl<CheckBox>("SamplingPlotBox").IsChecked = strategies[i].Plot;
                            this.FindControl<Grid>("SamplingGrid").IsVisible = true;
                            break;
                        case Strategies.IterativeSampling:
                            this.FindControl<NumericUpDown>("IterativeSamplingMinBox").Value = ((IterativeSampling)strategies[i]).Min;
                            this.FindControl<NumericUpDown>("IterativeSamplingMaxBox").Value = ((IterativeSampling)strategies[i]).Max;
                            this.FindControl<NumericUpDown>("IterativeSamplingResolutionBox").Value = ((IterativeSampling)strategies[i]).Resolution;
                            this.FindControl<NumericUpDown>("IterativeSamplingThresholdBox").Value = ((IterativeSampling)strategies[i]).ConvergenceThreshold;
                            this.FindControl<CheckBox>("IterativeSamplingPlotBox").IsChecked = strategies[i].Plot;
                            this.FindControl<Grid>("IterativeSamplingGrid").IsVisible = true;
                            break;
                        case Strategies.RandomWalk:
                            this.FindControl<NumericUpDown>("RandomWalkStepsBox").Value = ((RandomWalk)strategies[i]).StepsPerRun;
                            this.FindControl<ComboBox>("RandomWalkCriterionBox").SelectedIndex = (int)((RandomWalk)strategies[i]).ConvergenceCriterion;
                            this.FindControl<NumericUpDown>("RandomWalkThresholdBox").Value = ((RandomWalk)strategies[i]).ConvergenceThreshold;
                            this.FindControl<CheckBox>("RandomWalkPlotBox").IsChecked = strategies[i].Plot;
                            this.FindControl<Grid>("RandomWalkGrid").IsVisible = true;
                            break;
                        case Strategies.NesterovClimbing:
                            this.FindControl<NumericUpDown>("NesterovClimbingStepsBox").Value = ((NesterovClimbing)strategies[i]).StepsPerRun;
                            this.FindControl<ComboBox>("NesterovClimbingCriterionBox").SelectedIndex = (int)((NesterovClimbing)strategies[i]).ConvergenceCriterion;
                            this.FindControl<NumericUpDown>("NesterovClimbingThresholdBox").Value = ((NesterovClimbing)strategies[i]).ConvergenceThreshold;
                            this.FindControl<CheckBox>("NesterovClimbingPlotBox").IsChecked = strategies[i].Plot;
                            this.FindControl<Grid>("NesterovClimbingGrid").IsVisible = true;
                            break;
                    }
                }
                else
                {
                    grd.Background = new SolidColorBrush(Colors.White);
                }

                int j = i;

                grd.PointerPressed += (s, e) =>
                {
                    if (e.MouseButton == Avalonia.Input.MouseButton.Left)
                    {
                        selectedIndex = j;
                        BuildStrategyList();
                    }
                };

                if (strategies.Count > 1)
                {
                    AddButton remButt = new AddButton() { Type = AddButton.ButtonTypes.Remove, Margin = new Thickness(0, 0, 5, 0) };
                    remButt.PointerPressed += async (s, e) =>
                    {
                        if (e.MouseButton == MouseButton.Left)
                        {
                            await RemoveStrategy(j);
                            e.Handled = true;
                        }
                    };
                    grd.Children.Add(remButt);
                }

                if (i > 0)
                {
                    AddButton moveUp = new AddButton() { Type = AddButton.ButtonTypes.Up, Margin = new Thickness(0, 0, 5, 0) };
                    Grid.SetColumn(moveUp, 1);

                    moveUp.PointerPressed += (s, e) =>
                    {
                        if (e.MouseButton == MouseButton.Left)
                        {
                            if (selectedIndex == j)
                            {
                                selectedIndex--;
                            }
                            MoveStrategyUp(j);
                            e.Handled = true;
                        }
                    };

                    grd.Children.Add(moveUp);

                }

                if (i < strategies.Count - 1)
                {
                    AddButton moveDown = new AddButton() { Type = AddButton.ButtonTypes.Down, Margin = new Thickness(0, 0, 10, 0) };
                    Grid.SetColumn(moveDown, 2);

                    moveDown.PointerPressed += (s, e) =>
                    {
                        if (e.MouseButton == MouseButton.Left)
                        {
                            if (selectedIndex == j)
                            {
                                selectedIndex++;
                            }
                            MoveStrategyDown(j);
                            e.Handled = true;
                        }
                    };

                    grd.Children.Add(moveDown);
                }

                TextBlock block = new TextBlock() { Text = strategies[i].ToString() };
                Grid.SetColumn(block, 3);
                grd.Children.Add(block);

                this.FindControl<StackPanel>("CurrentStrategyPanel").Children.Add(grd);
            }

            initialised = true;
        }

        bool initialised = false;

        private void ValueChanged(object sender, NumericUpDownValueChangedEventArgs e)
        {
            if (initialised)
            {
                switch (strategies[selectedIndex].Strategy)
                {
                    case Strategies.Sampling:
                        strategies[selectedIndex] = new Sampling(this.FindControl<CheckBox>("SamplingPlotBox").IsChecked == true, this.FindControl<NumericUpDown>("SamplingMinBox").Value, this.FindControl<NumericUpDown>("SamplingMaxBox").Value, this.FindControl<NumericUpDown>("SamplingResolutionBox").Value);
                        break;
                    case Strategies.IterativeSampling:
                        strategies[selectedIndex] = new IterativeSampling(this.FindControl<CheckBox>("IterativeSamplingPlotBox").IsChecked == true, this.FindControl<NumericUpDown>("IterativeSamplingMinBox").Value, this.FindControl<NumericUpDown>("IterativeSamplingMaxBox").Value, this.FindControl<NumericUpDown>("IterativeSamplingResolutionBox").Value, ((IterativeSampling)strategies[selectedIndex]).ConvergenceCriterion, this.FindControl<NumericUpDown>("IterativeSamplingThresholdBox").Value);
                        break;
                    case Strategies.RandomWalk:
                        strategies[selectedIndex] = new RandomWalk(this.FindControl<CheckBox>("RandomWalkPlotBox").IsChecked == true, (ConvergenceCriteria)this.FindControl<ComboBox>("RandomWalkCriterionBox").SelectedIndex, this.FindControl<NumericUpDown>("RandomWalkThresholdBox").Value, (int)this.FindControl<NumericUpDown>("RandomWalkStepsBox").Value);
                        break;
                    case Strategies.NesterovClimbing:
                        ConvergenceCriteria crit = (ConvergenceCriteria)this.FindControl<ComboBox>("NesterovClimbingCriterionBox").SelectedIndex;
                        strategies[selectedIndex] = new NesterovClimbing(this.FindControl<CheckBox>("NesterovClimbingPlotBox").IsChecked == true, crit, this.FindControl<NumericUpDown>("NesterovClimbingThresholdBox").Value, (int)this.FindControl<NumericUpDown>("NesterovClimbingStepsBox").Value);
                        break;
                }

                BuildStrategyList();
            }
        }

        private void PlotChanged(object sender, RoutedEventArgs e)
        {
            ValueChanged(null, null);
        }

        private void CriterionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValueChanged(null, null);
        }

        private void AddSampling(object sender, PointerPressedEventArgs e)
        {
            if (e.MouseButton == MouseButton.Left)
            {
                strategies.Add(MaximisationStrategy.Parse("Sampling()"));
                BuildStrategyList();
            }
        }

        private void AddIterativeSampling(object sender, PointerPressedEventArgs e)
        {
            if (e.MouseButton == MouseButton.Left)
            {
                strategies.Add(MaximisationStrategy.Parse("IterativeSampling()"));
                BuildStrategyList();
            }
        }

        private void AddRandomWalk(object sender, PointerPressedEventArgs e)
        {
            if (e.MouseButton == MouseButton.Left)
            {
                strategies.Add(MaximisationStrategy.Parse("RandomWalk()"));
                BuildStrategyList();
            }
        }

        private void AddNesterovClimbing(object sender, PointerPressedEventArgs e)
        {
            if (e.MouseButton == MouseButton.Left)
            {
                strategies.Add(MaximisationStrategy.Parse("NesterovClimbing()"));
                BuildStrategyList();
            }
        }

        private async Task RemoveStrategy(int index)
        {
            if (strategies.Count > 1)
            {
                strategies.RemoveAt(index);
                selectedIndex = Math.Min(index, strategies.Count - 1);
                BuildStrategyList();
            }
            else
            {
                await new MessageBox("Warning!", "You must choose at least one likelihood optimisation strategy!").ShowDialog(this);
            }
        }

        private void MoveStrategyUp(int index)
        {
            if (index > 0)
            {
                MaximisationStrategy item = strategies[index];
                strategies.RemoveAt(index);
                strategies.Insert(index - 1, item);
                BuildStrategyList();
            }
        }

        private void MoveStrategyDown(int index)
        {
            if (index < strategies.Count - 1)
            {
                MaximisationStrategy item = strategies[index];
                strategies.RemoveAt(index);
                strategies.Insert(index + 1, item);
                BuildStrategyList();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void TestButtonClicked(object sender, RoutedEventArgs e)
        {
            varMax = 10;

            (Func<double, double, double> func, double[] maxPoint) likFunc = Utils.FunctionGenerator.GetFake2VariateLikelihood(varMax);

            Bitmap bmp = new Bitmap(150, 150);

            double[,] computedVals = new double[bmp.Width, bmp.Height];

            double max = double.MinValue;
            double min = double.MaxValue;

            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    double realX = (double)x / bmp.Width * varMax;
                    double realY = (double)y / bmp.Height * varMax;
                    double val = -Math.Log(-likFunc.func(realX, realY));
                    max = Math.Max(max, val);
                    min = Math.Min(min, val);
                    computedVals[x, y] = val;
                }
            }

            landscapeMax = max;
            landscapeMin = min;

            currLikFunc = likFunc.func;

            maximum = likFunc.func(likFunc.maxPoint[0], likFunc.maxPoint[1]);

            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    int col = Math.Max(0, Math.Min(255, (int)((computedVals[x, y] - min) / (max - min) * 255)));
                    bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(col, col, col));
                }
            }

            MemoryStream s = new MemoryStream();
            bmp.Save(s, System.Drawing.Imaging.ImageFormat.Bmp);

            s.Position = 0;

            Avalonia.Media.Imaging.Bitmap avaloniaBmp = new Avalonia.Media.Imaging.Bitmap(s);

            this.FindControl<Avalonia.Controls.Image>("SampledValuesImage").Source = avaloniaBmp;

            lock (plotLock)
            {
                landscapeBmp = bmp;
            }

            plottedXs = new List<double>();
            plottedYs = new List<double>();

            currentXs = new List<double>();
            currentYs = new List<double>();

            sampledValues = new List<double[]>();

            lastSampledPlotted = -1;

            MaximiseFunction((x, y) => likFunc.func(x, y));
        }

        double varMax;

        List<double> plottedXs;
        List<double> plottedYs;

        List<double> currentXs;
        List<double> currentYs;

        int currentTotalSteps;

        object plotLock = new object();

        bool plotEnqueued = false;

        double maximum;

        Bitmap landscapeBmp;

        List<double[]> sampledValues;

        int lastSampledPlotted = -1;

        double landscapeMin;
        double landscapeMax;

        Func<double, double, double> currLikFunc;

        async Task PlotStatus()
        {
            double[] localPlottedYs;

            double[] localCurrentXs;
            double[] localCurrentYs;

            int localSteps;

            lock (plotLock)
            {
                localPlottedYs = plottedYs.ToArray();

                localCurrentXs = currentXs.ToArray();
                localCurrentYs = currentYs.ToArray();
                localSteps = currentTotalSteps;
                plotEnqueued = false;
            }

            int w = (int)this.FindControl<Grid>("PlotsGrid").ColumnDefinitions[0].ActualWidth;

            Bitmap bmp = new Bitmap(w, 150);

            Graphics gpr = Graphics.FromImage(bmp);

            gpr.SmoothingMode = SmoothingMode.HighQuality;
            gpr.InterpolationMode = InterpolationMode.High;

            gpr.Clear(System.Drawing.Color.White);

            gpr.DrawLine(new System.Drawing.Pen(System.Drawing.Color.Black) { Width = 2 }, 5, 145, w - 3, 145);
            gpr.DrawLine(new System.Drawing.Pen(System.Drawing.Color.Black) { Width = 2 }, 5, 145, 5, 3);

            GraphicsPath pth = new GraphicsPath();
            pth.StartFigure();
            pth.AddLine(0, 10, 5, 0);
            pth.AddLine(5, 0, 10, 10);
            pth.CloseFigure();

            gpr.FillPath(new SolidBrush(System.Drawing.Color.Black), pth);

            pth = new GraphicsPath();
            pth.StartFigure();
            pth.AddLine(w - 10, 140, w, 145);
            pth.AddLine(w, 145, w - 10, 150);
            pth.CloseFigure();

            gpr.FillPath(new SolidBrush(System.Drawing.Color.Black), pth);

            

            if (localCurrentYs.Length > 0 || localPlottedYs.Length > 0)
            {

                double minY = double.MaxValue;
                double maxY = double.MinValue;

                if (localCurrentYs.Length > 0)
                {
                    minY = Math.Min(localCurrentYs.Min(), minY);
                    maxY = Math.Max(localCurrentYs.Max(), maxY);
                }

                if (localPlottedYs.Length > 0)
                {
                    minY = Math.Min(localPlottedYs.Min(), minY);
                    maxY = Math.Max(localPlottedYs.Max(), maxY);
                }

                maxY = maximum;

                gpr.DrawLine(new System.Drawing.Pen(System.Drawing.Color.FromArgb(0, 162, 232)), 5, 10 + (float)(135.0 - (maximum - minY) / (maxY - minY) * 135.0), w, 10 + (float)(135.0 - (maximum - minY) / (maxY - minY) * 135.0));

                double maxX = localPlottedYs.Length + localSteps;

                float lastX = 0;
                float lastY = 0;

                pth = new GraphicsPath();
                pth.StartFigure();

                for (int x = 0; x < localPlottedYs.Length; x++)
                {
                    float plotX = 5 + (float)(x / maxX * (w - 5));
                    float plotY = 10 + (float)(135.0 - (localPlottedYs[x] - minY) / (maxY - minY) * 135.0);

                    if (x == 0)
                    {
                        lastX = plotX;
                        lastY = plotY;
                    }
                    else
                    {
                        pth.AddLine(lastX, lastY, plotX, plotY);
                        lastX = plotX;
                        lastY = plotY;
                    }
                }

                for (int x = 0; x < localCurrentYs.Length; x++)
                {
                    float plotX = 5 + (float)((x + localPlottedYs.Length) / maxX * (w - 5));
                    float plotY = 10 + (float)(135.0 - (localCurrentYs[x] - minY) / (maxY - minY) * 135.0);

                    if (x != 0 || localPlottedYs.Length > 0)
                    {
                        pth.AddLine(lastX, lastY, plotX, plotY);
                    }

                    lastX = plotX;
                    lastY = plotY;
                }

                gpr.DrawPath(new System.Drawing.Pen(System.Drawing.Color.FromArgb(58, 78, 124), 2) { LineJoin = LineJoin.Round }, pth);
            }

            gpr.Flush();
            gpr.Dispose();

            MemoryStream s = new MemoryStream();
            bmp.Save(s, System.Drawing.Imaging.ImageFormat.Bmp);

            s.Position = 0;

            Avalonia.Media.Imaging.Bitmap avaloniaBmp = new Avalonia.Media.Imaging.Bitmap(s);

            MemoryStream s2 = new MemoryStream();

            lock (plotLock)
            {

                for (int i = lastSampledPlotted + 1; i < sampledValues.Count; i++)
                {
                    int x = (int)(sampledValues[i][0] / varMax * landscapeBmp.Width);
                    int y = (int)(sampledValues[i][1] / varMax * landscapeBmp.Height);

                    

                    if (x >= 0 && x < landscapeBmp.Width && y >= 0 && y < landscapeBmp.Height)
                    {
                        double val = -Math.Log(-currLikFunc(sampledValues[i][0], sampledValues[i][1]));

                        int col = Math.Max(0, Math.Min(1023, (int)((val - landscapeMin) / (landscapeMax - landscapeMin) * 1024)));

                        landscapeBmp.SetPixel(x, y, System.Drawing.Color.FromArgb(Plotting.ViridisColorScale[col][0], Plotting.ViridisColorScale[col][1], Plotting.ViridisColorScale[col][2]));
                    }
                }
                lastSampledPlotted = sampledValues.Count;
                landscapeBmp.Save(s2, System.Drawing.Imaging.ImageFormat.Bmp);
            }

            s2.Position = 0;

            Avalonia.Media.Imaging.Bitmap avaloniaLandscapeBmp = new Avalonia.Media.Imaging.Bitmap(s2);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                this.FindControl<Avalonia.Controls.Image>("EstimateImage").Source = avaloniaBmp;
                this.FindControl<Avalonia.Controls.Image>("SampledValuesImage").Source = avaloniaLandscapeBmp;
            });
        }


        void MaximiseFunction(Func<double, double, double> func)
        {
            double[] startValues = new double[2];
            List<(Utils.Utils.VariableStepType stepType, int[] affectedVariables, double sigma)> stepTypes = new List<(Utils.Utils.VariableStepType stepType, int[] affectedVariables, double sigma)>();

            ThreadSafeRandom rnd = new ThreadSafeRandom();

            startValues[0] = Exponential.Sample(rnd, 1);
            startValues[1] = Exponential.Sample(rnd, 1);

            stepTypes.Add((Utils.Utils.VariableStepType.NormalSlide, new int[] { 0, 1 }, 0.2));

            Func<double[], double> likelihoodFunc = (vars) =>
            {
                return func(vars[0], vars[1]);
            };

            double[] bestVars = startValues;

            Utils.Utils.Trigger = (s, o) =>
            {
                switch (s)
                {
                    case "Plot":
                        lock (plotLock)
                        {
                            currentXs = (List<double>)o[0];
                            currentYs = (List<double>)o[1];
                            currentTotalSteps = (int)o[2];
                            plotEnqueued = true;
                        }
                        break;
                    case "StepFinished":
                        lock (plotLock)
                        {
                            currentXs = new List<double>();
                            currentYs = new List<double>();
                            plottedXs.AddRange((List<double>)o[0]);
                            plottedYs.AddRange((List<double>)o[1]);
                            currentTotalSteps = 0;
                            plotEnqueued = true;
                        }
                        break;
                    case "ValueSampled":
                        lock (plotLock)
                        {
                            sampledValues.Add((double[])o[0]);
                            plotEnqueued = true;
                        }
                        break;
                }
            };

            EventWaitHandle plotHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

            Thread thr = new Thread(() =>
            {
                int prevMax = Utils.Utils.MaxThreads;
                Utils.Utils.MaxThreads = 1;
                for (int i = 0; i < strategies.Count; i++)
                {
                    switch (strategies[i].Strategy)
                    {
                        case Strategies.RandomWalk:
                            bestVars = Utils.Utils.AutoMaximiseFunctionRandomWalk(likelihoodFunc, bestVars, stepTypes.ToArray(), (RandomWalk)strategies[i], rnd, -2);
                            break;
                        case Strategies.Sampling:
                            bestVars = Utils.Utils.AutoMaximiseFunctionSampling(likelihoodFunc, bestVars, stepTypes.ToArray(), (Sampling)strategies[i], -2);
                            break;
                        case Strategies.NesterovClimbing:
                            bestVars = Utils.Utils.AutoMaximiseFunctionNesterov(likelihoodFunc, bestVars, stepTypes.ToArray(), (NesterovClimbing)strategies[i], rnd, -2);
                            break;
                        case Strategies.IterativeSampling:
                            bestVars = Utils.Utils.AutoMaximiseFunctionIterativeSampling((a, b) => likelihoodFunc(a), bestVars, stepTypes.ToArray(), (IterativeSampling)strategies[i], -2);
                            break;
                    }
                }

                Utils.Utils.MaxThreads = prevMax;

                plotHandle.Set();

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<Button>("StartTestButton").IsEnabled = true;
                });
            });



            Thread plottingThread = new Thread(async () =>
            {
                while (!plotHandle.WaitOne(0))
                {
                    if (plotEnqueued)
                    {
                        await PlotStatus();
                    }
                }

                if (plotEnqueued)
                {
                    await PlotStatus();
                }
            });

            this.FindControl<Button>("StartTestButton").IsEnabled = false;

            thr.Start();
            plottingThread.Start();
        }

        private void OKClicked(object sender, RoutedEventArgs e)
        {
            this.result = "";

            for (int i = 0; i < strategies.Count; i++)
            {
                result += strategies[i].ToString() + (i < strategies.Count - 1 ? "|" : "");
            }

            this.Close();
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            this.result = null;
            this.Close();
        }
    }
}
