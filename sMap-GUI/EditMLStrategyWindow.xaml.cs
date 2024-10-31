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
using System.IO;
using MathNet.Numerics.Distributions;
using System.Threading;
using Avalonia.Threading;
using System.Threading.Tasks;
using VectSharp;
using VectSharp.Canvas;
using Avalonia.Media.Imaging;

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

        WriteableBitmap landscapeBmp;

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
                    if (e.GetCurrentPoint(grd).Properties.PointerUpdateKind == Avalonia.Input.PointerUpdateKind.LeftButtonPressed)
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
                        if (e.GetCurrentPoint(remButt).Properties.PointerUpdateKind == Avalonia.Input.PointerUpdateKind.LeftButtonPressed)
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
                        if (e.GetCurrentPoint(moveUp).Properties.PointerUpdateKind == Avalonia.Input.PointerUpdateKind.LeftButtonPressed)
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
                        if (e.GetCurrentPoint(moveDown).Properties.PointerUpdateKind == Avalonia.Input.PointerUpdateKind.LeftButtonPressed)
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
            if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == Avalonia.Input.PointerUpdateKind.LeftButtonPressed)
            {
                strategies.Add(MaximisationStrategy.Parse("Sampling()"));
                BuildStrategyList();
            }
        }

        private void AddIterativeSampling(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == Avalonia.Input.PointerUpdateKind.LeftButtonPressed)
            {
                strategies.Add(MaximisationStrategy.Parse("IterativeSampling()"));
                BuildStrategyList();
            }
        }

        private void AddRandomWalk(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == Avalonia.Input.PointerUpdateKind.LeftButtonPressed)
            {
                strategies.Add(MaximisationStrategy.Parse("RandomWalk()"));
                BuildStrategyList();
            }
        }

        private void AddNesterovClimbing(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == Avalonia.Input.PointerUpdateKind.LeftButtonPressed)
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

        private void SetPixel(Avalonia.Platform.ILockedFramebuffer fb, int x, int y, Color col)
        {
            int address = y * fb.RowBytes + x * 4;

            byte[] pixel = new byte[4];

            if (fb.Format == Avalonia.Platform.PixelFormat.Rgba8888)
            {
                pixel[0] = col.R;
                pixel[1] = col.G;
                pixel[2] = col.B;
                pixel[3] = col.A;
            }
            else if (fb.Format == Avalonia.Platform.PixelFormat.Bgra8888)
            {
                pixel[0] = col.B;
                pixel[1] = col.G;
                pixel[2] = col.R;
                pixel[3] = col.A;
            }

            System.Runtime.InteropServices.Marshal.Copy(pixel, 0, IntPtr.Add(fb.Address, address), 4);
        }

        private void TestButtonClicked(object sender, RoutedEventArgs e)
        {
            varMax = 10;

            (Func<double, double, double> func, double[] maxPoint) likFunc = Utils.FunctionGenerator.GetFake2VariateLikelihood(varMax);

            WriteableBitmap bmp = new WriteableBitmap(new PixelSize(150, 150), new Vector(72, 72));

            double[,] computedVals = new double[bmp.PixelSize.Width, bmp.PixelSize.Height];

            double max = double.MinValue;
            double min = double.MaxValue;

            for (int x = 0; x < bmp.PixelSize.Width; x++)
            {
                for (int y = 0; y < bmp.PixelSize.Height; y++)
                {
                    double realX = (double)x / bmp.PixelSize.Width * varMax;
                    double realY = (double)y / bmp.PixelSize.Height * varMax;
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

            using (Avalonia.Platform.ILockedFramebuffer fb = bmp.Lock())
            {
                for (int x = 0; x < bmp.PixelSize.Width; x++)
                {
                    for (int y = 0; y < bmp.PixelSize.Height; y++)
                    {
                        byte col = (byte)Math.Max(0, Math.Min(255, (int)((computedVals[x, y] - min) / (max - min) * 255)));

                        SetPixel(fb, x, y, Color.FromRgb(col, col, col));
                    }
                }
            }

            this.FindControl<Avalonia.Controls.Image>("SampledValuesImage").Source = bmp;

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

            int w = -1;

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                w = (int)this.FindControl<Grid>("PlotsGrid").ColumnDefinitions[0].ActualWidth;
            });

            Page pag = new Page(w, 150);

            Graphics gpr = pag.Graphics;

            gpr.StrokePath(new GraphicsPath().MoveTo(5, 145).LineTo(w - 3, 145), Colour.FromRgb(0, 0, 0), 2);
            gpr.StrokePath(new GraphicsPath().MoveTo(5, 145).LineTo(5, 3), Colour.FromRgb(0, 0, 0), 2);



            GraphicsPath pth = new GraphicsPath();
            pth.MoveTo(0, 10);
            pth.LineTo(5, 0);
            pth.LineTo(10, 10);
            pth.Close();

            gpr.FillPath(pth, Colour.FromRgb(0, 0, 0));

            pth = new GraphicsPath();
            pth.MoveTo(w - 10, 140);
            pth.LineTo(w, 145);
            pth.LineTo(w - 10, 150);
            pth.Close();

            gpr.FillPath(pth, Colour.FromRgb(0, 0, 0));

            
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

                gpr.StrokePath(new GraphicsPath().MoveTo(5, 10 + (float)(135.0 - (maximum - minY) / (maxY - minY) * 135.0)).LineTo(w, 10 + (float)(135.0 - (maximum - minY) / (maxY - minY) * 135.0)), Colour.FromRgb(0, 162, 232));

                double maxX = localPlottedYs.Length + localSteps;

                float lastX = 0;
                float lastY = 0;

                pth = new GraphicsPath();

                for (int x = 0; x < localPlottedYs.Length; x++)
                {
                    float plotX = 5 + (float)(x / maxX * (w - 5));
                    float plotY = 10 + (float)(135.0 - (localPlottedYs[x] - minY) / (maxY - minY) * 135.0);

                    if (x == 0)
                    {
                        pth.MoveTo(plotX, plotY);
                        lastX = plotX;
                        lastY = plotY;
                    }
                    else
                    {
                        pth.LineTo(plotX, plotY);
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
                        pth.LineTo(plotX, plotY);
                    }

                    lastX = plotX;
                    lastY = plotY;
                }
                //new System.Drawing.Pen(System.Drawing.Color.FromArgb(58, 78, 124), 2) { LineJoin = LineJoin.Round }
                gpr.StrokePath(pth, Colour.FromRgb(58, 78, 124), 2, lineJoin: LineJoins.Round);
            }

            lock (plotLock)
            {
                using (Avalonia.Platform.ILockedFramebuffer fb = landscapeBmp.Lock())
                {
                    for (int i = lastSampledPlotted + 1; i < sampledValues.Count; i++)
                    {
                        int x = (int)(sampledValues[i][0] / varMax * landscapeBmp.PixelSize.Width);
                        int y = (int)(sampledValues[i][1] / varMax * landscapeBmp.PixelSize.Height);



                        if (x >= 0 && x < landscapeBmp.PixelSize.Width && y >= 0 && y < landscapeBmp.PixelSize.Height)
                        {
                            double val = -Math.Log(-currLikFunc(sampledValues[i][0], sampledValues[i][1]));

                            int col = Math.Max(0, Math.Min(1023, (int)((val - landscapeMin) / (landscapeMax - landscapeMin) * 1024)));

                            SetPixel(fb, x, y, Color.FromRgb((byte)Plotting.ViridisColorScale[col][0], (byte)Plotting.ViridisColorScale[col][1], (byte)Plotting.ViridisColorScale[col][2]));
    
                        }
                    }
                    lastSampledPlotted = sampledValues.Count;
                }
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                this.FindControl<Avalonia.Controls.Canvas>("EstimateImage").Children.Clear();
                this.FindControl<Avalonia.Controls.Canvas>("EstimateImage").Children.Add(pag.PaintToCanvas(false));
                lock (plotLock)
                {
                    this.FindControl<Avalonia.Controls.Image>("SampledValuesImage").Source = landscapeBmp;
                    this.FindControl<Avalonia.Controls.Image>("SampledValuesImage").InvalidateVisual();
                }
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
                            bestVars = Utils.Utils.AutoMaximiseFunctionRandomWalk(likelihoodFunc, bestVars, stepTypes.ToArray(), (RandomWalk)strategies[i], rnd, null, -2);
                            break;
                        case Strategies.Sampling:
                            bestVars = Utils.Utils.AutoMaximiseFunctionSampling(likelihoodFunc, bestVars, stepTypes.ToArray(), (Sampling)strategies[i], false, null, -2);
                            break;
                        case Strategies.NesterovClimbing:
                            bestVars = Utils.Utils.AutoMaximiseFunctionNesterov(likelihoodFunc, bestVars, stepTypes.ToArray(), (NesterovClimbing)strategies[i], rnd, null, -2);
                            break;
                        case Strategies.IterativeSampling:
                            bestVars = Utils.Utils.AutoMaximiseFunctionIterativeSampling((a, b) => likelihoodFunc(a), bestVars, stepTypes.ToArray(), (IterativeSampling)strategies[i], false, null, -2);
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
