using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System.Collections.Generic;
using Utils;
using System.Linq;
using System;
using Avalonia.Controls.Shapes;
using MathNet.Numerics.Distributions;
using System.Threading;
using Avalonia.Interactivity;

namespace sMap_GUI
{
    public class ViewRatesWindow : Window
    {
        public ViewRatesWindow()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        bool IsEditable;

        CharacterDependency[][] Dependencies;
        string[][] States;

        public Dictionary<string, Parameter>[] Rates;

        public ViewRatesWindow(string[][] states, CharacterDependency[][] dependencies, Dictionary<string, Parameter>[] rates, IBrush headerBrush, bool isEditable = false)
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            this.FindControl<Path>("PrePath").Stroke = headerBrush;
            this.FindControl<Path>("PostPath").Stroke = headerBrush;
            this.FindControl<Path>("BGPath").Fill = headerBrush;
            this.FindControl<TextBlock>("Label").Foreground = headerBrush;

            IsEditable = isEditable;
            States = states;
            Dependencies = dependencies;
            Rates = rates;

            if (IsEditable)
            {
                this.FindControl<Grid>("SourceGrid").IsVisible = true;
                this.FindControl<Grid>("MainGrid").ColumnDefinitions[1].Width = new GridLength(0.45, GridUnitType.Star);
            }

            BuildWindow();

            //Workaround Avalonia bug
            async void resize()
            {
                await System.Threading.Tasks.Task.Delay(100);
                this.Height = this.Height + 1;
            };

            resize();
        }

        void BuildWindow()
        {
            ThreadSafeRandom rand = new ThreadSafeRandom();

            StackPanel mainContainer = this.FindControl<StackPanel>("RatesContainer");

            this.FindControl<TextBox>("SourceBox").Text = Utils.Utils.GetRatesSource(Dependencies, Rates).Replace("\t", "    ");

            mainContainer.Children.Clear();

            for (int depInd = 0; depInd < Dependencies.Length; depInd++)
            {
                mainContainer.Children.Add(new TextBlock() { Text = "Set " + depInd.ToString() + ":", FontWeight = FontWeight.Bold, FontSize = 20, Margin = new Thickness(0, 20, 0, 0) });

                for (int i = 0; i < Dependencies[depInd].Length; i++)
                {
                    if (Dependencies[depInd][i].Type != CharacterDependency.Types.Conditioned)
                    {
                        mainContainer.Children.Add(new TextBlock() { Text = "Character" + (Dependencies[depInd][i].InputDependencyName.Contains(",") ? "s" : "") + " " + Dependencies[depInd][i].InputDependencyName + ":", FontWeight = FontWeight.Bold, FontSize = 18, Margin = new Thickness(20, 5, 0, 0) });

                        Grid grd = new Grid();
                        grd.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));
                        grd.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));

                        int rateCount = (from el in States[Dependencies[depInd][i].Index] select (from el2 in States[Dependencies[depInd][i].Index] where el2 != el && Rates[Dependencies[depInd][i].Index][el + ">" + el2].Action != Parameter.ParameterAction.Equal select 1).Sum()).Sum();

                        int colInd = 0;

                        Dictionary<string, int> colInds = new Dictionary<string, int>();

                        double[] rateRange = new double[] { double.MaxValue, double.MinValue };

                        for (int j = 0; j < States[Dependencies[depInd][i].Index].Length; j++)
                        {
                            for (int k = 0; k < States[Dependencies[depInd][i].Index].Length; k++)
                            {
                                if (j != k && Rates[Dependencies[depInd][i].Index][States[Dependencies[depInd][i].Index][j] + ">" + States[Dependencies[depInd][i].Index][k]].Action == Parameter.ParameterAction.Bayes)
                                {
                                    double[] range = Rates[Dependencies[depInd][i].Index][States[Dependencies[depInd][i].Index][j] + ">" + States[Dependencies[depInd][i].Index][k]].PriorDistribution.GetRange(0.95);
                                    rateRange = new double[] { Math.Min(range[0], rateRange[0]), Math.Max(range[1], rateRange[1]) };
                                }
                            }
                        }

                        Dictionary<string, Parameter> currRates = Rates[Dependencies[depInd][i].Index];

                        for (int j = 0; j < States[Dependencies[depInd][i].Index].Length; j++)
                        {

                            grd.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));
                            grd.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));

                            {
                                TextBlock blk = new TextBlock() { Text = States[Dependencies[depInd][i].Index][j], Margin = new Thickness(0, 10, 10, 10), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
                                Grid.SetRow(blk, j + 1);
                                grd.Children.Add(blk);
                            }

                            {
                                TextBlock blk = new TextBlock() { Text = States[Dependencies[depInd][i].Index][j], Margin = new Thickness(10, 0, 10, 10), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
                                Grid.SetColumn(blk, j + 1);
                                grd.Children.Add(blk);
                            }
                            {
                                Canvas can = new Canvas() { Height = 1, Background = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220)), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top, ZIndex = -1 };

                                Grid.SetRow(can, j + 1);
                                Grid.SetColumnSpan(can, States[Dependencies[depInd][i].Index].Length + 1);
                                grd.Children.Add(can);

                                Canvas can2 = new Canvas() { Width = 1, Background = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220)), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, ZIndex = -1 };

                                Grid.SetColumn(can2, j + 1);
                                Grid.SetRowSpan(can2, States[Dependencies[depInd][i].Index].Length + 1);
                                grd.Children.Add(can2);
                            }

                            for (int k = 0; k < States[Dependencies[depInd][i].Index].Length; k++)
                            {
                                if (j != k)
                                {
                                    Parameter currRate = Rates[Dependencies[depInd][i].Index][States[Dependencies[depInd][i].Index][j] + ">" + States[Dependencies[depInd][i].Index][k]];

                                    colInds.Add(States[Dependencies[depInd][i].Index][j] + ">" + States[Dependencies[depInd][i].Index][k], colInd);

                                    if (currRate.Action == Parameter.ParameterAction.ML)
                                    {
                                        Border bord = new Border() { Background = Program.GetBrush(Plotting.GetColor(colInd++, 0.25, rateCount)), BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new Thickness(1.5), Padding = new Thickness(10, 0, 10, 0), CornerRadius = new CornerRadius(17.5), MinHeight = 35, Margin = new Thickness(5) };
                                        bord.Child = new TextBlock() { Text = "ML", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };

                                        ToolTip.SetTip(bord, "Maximum-likelihood");

                                        Grid.SetRow(bord, j + 1);
                                        Grid.SetColumn(bord, k + 1);

                                        if (IsEditable)
                                        {
                                            bord.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
                                            bord.PointerPressed += async (s, e) =>
                                            {
                                                EditRateWindow win = new EditRateWindow(currRate, currRates);
                                                await win.ShowDialog(this);

                                                BuildWindow();
                                            };
                                        }

                                        grd.Children.Add(bord);
                                    }
                                    else if (currRate.Action == Parameter.ParameterAction.Fix)
                                    {
                                        Border bord = new Border() { Background = Program.GetBrush(Plotting.GetColor(colInd++, 0.25, rateCount)), BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new Thickness(1.5), Padding = new Thickness(10, 0, 10, 0), MinHeight = 35, Margin = new Thickness(5) };
                                        bord.Child = new TextBlock() { Text = currRate.Value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };

                                        ToolTip.SetTip(bord, "Fixed(" + currRate.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")");

                                        Grid.SetRow(bord, j + 1);
                                        Grid.SetColumn(bord, k + 1);

                                        if (IsEditable)
                                        {
                                            bord.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
                                            bord.PointerPressed += async (s, e) =>
                                            {
                                                EditRateWindow win = new EditRateWindow(currRate, currRates);
                                                await win.ShowDialog(this);

                                                BuildWindow();
                                            };
                                        }

                                        grd.Children.Add(bord);
                                    }
                                    else if (currRate.Action == Parameter.ParameterAction.Bayes)
                                    {
                                        Canvas can = new Canvas() { Width = 50, Height = 35, Margin = new Thickness(10), Background = new SolidColorBrush(Colors.White) };

                                        double[] computedYs = new double[100];

                                        double minX = rateRange[0];
                                        double maxX = rateRange[1];

                                        for (int x = 0; x < 100; x++)
                                        {
                                            double realX = x / 100.0 * (maxX - minX) + minX;
                                            computedYs[x] = currRate.PriorDistribution.Density(realX);
                                        }

                                        double maxY = computedYs.Max();

                                        PathFigure fig = new PathFigure();
                                        PathFigure fillFig = new PathFigure() { StartPoint = new Point(0, 35) };

                                        bool startedFig = false;

                                        for (int x = 0; x < 100; x++)
                                        {
                                            if (!double.IsNaN(computedYs[x]))
                                            {
                                                if (!startedFig)
                                                {
                                                    fig.StartPoint = new Point(x * 0.5, 35 - computedYs[x] / maxY * 35);
                                                    startedFig = true;
                                                }
                                                else
                                                {
                                                    fig.Segments.Add(new LineSegment() { Point = new Point(x * 0.5, 35 - computedYs[x] / maxY * 35) });
                                                }

                                                fillFig.Segments.Add(new LineSegment() { Point = new Point(x * 0.5, 35 - computedYs[x] / maxY * 35) });
                                            }
                                            else
                                            {
                                                if (!startedFig)
                                                {
                                                    fillFig.StartPoint = new Point(x * 0.5, 35);
                                                }
                                            }
                                        }

                                        fillFig.Segments.Add(new LineSegment() { Point = new Point(49.5, 35) });
                                        fillFig.IsClosed = true;

                                        fig.IsClosed = false;

                                        PathGeometry geo = new PathGeometry();
                                        geo.Figures.Add(fig);

                                        PathGeometry fillGeo = new PathGeometry();
                                        fillGeo.Figures.Add(fillFig);

                                        can.Children.Add(new Path() { Data = fillGeo, Fill = Program.GetBrush(Plotting.GetColor(colInd, 0.25, rateCount)) });
                                        can.Children.Add(new Path() { Data = geo, Stroke = Program.GetBrush(Plotting.GetColor(colInd++, 1, rateCount)), StrokeThickness = 2, StrokeJoin = PenLineJoin.Round });

                                        Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

                                        ToolTip.SetTip(can, currRate.PriorDistribution.ToString());

                                        Grid.SetRow(can, j + 1);
                                        Grid.SetColumn(can, k + 1);

                                        if (IsEditable)
                                        {
                                            can.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
                                            can.PointerPressed += async (s, e) =>
                                            {
                                                EditRateWindow win = new EditRateWindow(currRate, currRates);
                                                await win.ShowDialog(this);

                                                BuildWindow();
                                            };
                                        }

                                        grd.Children.Add(can);
                                    }
                                }
                            }
                        }

                        for (int j = 0; j < States[Dependencies[depInd][i].Index].Length; j++)
                        {
                            for (int k = 0; k < States[Dependencies[depInd][i].Index].Length; k++)
                            {
                                if (j != k)
                                {
                                    Parameter currRate = Rates[Dependencies[depInd][i].Index][States[Dependencies[depInd][i].Index][j] + ">" + States[Dependencies[depInd][i].Index][k]];
                                    if (currRate.Action == Parameter.ParameterAction.Equal)
                                    {
                                        string equalName = (from el in Rates[Dependencies[depInd][i].Index] where el.Value == currRate.EqualParameter select el.Key).FirstOrDefault();

                                        FormattedText txt = new FormattedText() { Text = equalName.Replace(">", " > "), Typeface = new Typeface(this.FontFamily, this.FontSize) };

                                        PathFigure fig = new PathFigure() { StartPoint = new Point(0, 17.5) };

                                        fig.Segments.Add(new LineSegment() { Point = new Point(10, 0) });
                                        fig.Segments.Add(new LineSegment() { Point = new Point(txt.Bounds.Width + 20 - 10, 0) });
                                        fig.Segments.Add(new LineSegment() { Point = new Point(txt.Bounds.Width + 20, 17.5) });
                                        fig.Segments.Add(new LineSegment() { Point = new Point(txt.Bounds.Width + 20 - 10, 35) });
                                        fig.Segments.Add(new LineSegment() { Point = new Point(10, 35) });

                                        fig.IsClosed = true;

                                        PathGeometry geo = new PathGeometry();
                                        geo.Figures.Add(fig);

                                        Canvas can = new Canvas() { Width = txt.Bounds.Width + 20, Height = 35, Margin = new Thickness(5) };

                                        can.Children.Add(new Path() { Data = geo, Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 1.5, Fill = Program.GetBrush(Plotting.GetColor(colInds[equalName], 0.25, rateCount)) });

                                        can.Children.Add(new TextBlock() { Text = equalName.Replace(">", " > "), Margin = new Thickness(10, (35 - txt.Bounds.Height) / 2, 0, 0) });

                                        ToolTip.SetTip(can, "Equal(" + equalName + ")");

                                        Grid.SetRow(can, j + 1);
                                        Grid.SetColumn(can, k + 1);

                                        if (IsEditable)
                                        {
                                            can.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
                                            can.PointerPressed += async (s, e) =>
                                            {
                                                EditRateWindow win = new EditRateWindow(currRate, currRates);
                                                await win.ShowDialog(this);

                                                BuildWindow();
                                            };
                                        }

                                        grd.Children.Add(can);
                                    }
                                }
                            }
                        }

                        mainContainer.Children.Add(grd);
                    }
                }
            }

        }


        public ViewRatesWindow(DataMatrix data, CharacterDependency[][] dependencies, Dictionary<string, Parameter>[] rates, IBrush headerBrush, Dictionary<string, int>[] parameterIndices, double[][][] parameters)
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            this.FindControl<Path>("PrePath").Stroke = headerBrush;
            this.FindControl<Path>("PostPath").Stroke = headerBrush;
            this.FindControl<Path>("BGPath").Fill = headerBrush;
            this.FindControl<TextBlock>("Label").Foreground = headerBrush;

            Dependencies = dependencies;
            Rates = rates;

            ThreadSafeRandom rand = new ThreadSafeRandom();

            StackPanel mainContainer = this.FindControl<StackPanel>("RatesContainer");

            for (int depInd = 0; depInd < dependencies.Length; depInd++)
            {
                mainContainer.Children.Add(new TextBlock() { Text = "Set " + depInd.ToString() + ":", FontWeight = FontWeight.Bold, FontSize = 20, Margin = new Thickness(0, 20, 0, 0) });

                for (int i = 0; i < dependencies[depInd].Length; i++)
                {
                    if (dependencies[depInd][i].Type != CharacterDependency.Types.Conditioned)
                    {
                        mainContainer.Children.Add(new TextBlock() { Text = "Character" + (dependencies[depInd][i].InputDependencyName.Contains(",") ? "s" : "") + " " + dependencies[depInd][i].InputDependencyName + ":", FontWeight = FontWeight.Bold, FontSize = 18, Margin = new Thickness(20, 5, 0, 0) });

                        Grid grd = new Grid();
                        grd.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));
                        grd.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));

                        int rateCount = (from el in data.States[dependencies[depInd][i].Index] select (from el2 in data.States[dependencies[depInd][i].Index] where el2 != el && rates[dependencies[depInd][i].Index][el + ">" + el2].Action != Parameter.ParameterAction.Equal select 1).Sum()).Sum();

                        int colInd = 0;

                        Dictionary<string, int> colInds = new Dictionary<string, int>();

                        double[] rateRange = new double[] { double.MaxValue, double.MinValue };

                        for (int j = 0; j < data.States[dependencies[depInd][i].Index].Length; j++)
                        {
                            for (int k = 0; k < data.States[dependencies[depInd][i].Index].Length; k++)
                            {
                                string rateName = data.States[dependencies[depInd][i].Index][j] + ">" + data.States[dependencies[depInd][i].Index][k];
                                if (j != k && rates[dependencies[depInd][i].Index][rateName].Action == Parameter.ParameterAction.Bayes)
                                {
                                    int parInd = parameterIndices[dependencies[depInd][i].Index][dependencies[depInd][i].InputDependencyName + ":" + rateName];

                                    double min = (from el in parameters select el[dependencies[depInd][i].Index][parInd]).Min();
                                    double max = (from el in parameters select el[dependencies[depInd][i].Index][parInd]).Max();

                                    rateRange = new double[] { Math.Min(min, rateRange[0]), Math.Max(max, rateRange[1]) };
                                }
                            }
                        }

                        rateRange[0] *= 0.999;

                        for (int j = 0; j < data.States[dependencies[depInd][i].Index].Length; j++)
                        {

                            grd.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));
                            grd.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));

                            {
                                TextBlock blk = new TextBlock() { Text = data.States[dependencies[depInd][i].Index][j], Margin = new Thickness(0, 10, 10, 10), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
                                Grid.SetRow(blk, j + 1);
                                grd.Children.Add(blk);
                            }

                            {
                                TextBlock blk = new TextBlock() { Text = data.States[dependencies[depInd][i].Index][j], Margin = new Thickness(10, 0, 10, 10), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
                                Grid.SetColumn(blk, j + 1);
                                grd.Children.Add(blk);
                            }
                            {
                                Canvas can = new Canvas() { Height = 1, Background = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220)), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top, ZIndex = -1 };

                                Grid.SetRow(can, j + 1);
                                Grid.SetColumnSpan(can, data.States[dependencies[depInd][i].Index].Length + 1);
                                grd.Children.Add(can);

                                Canvas can2 = new Canvas() { Width = 1, Background = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220)), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, ZIndex = -1 };

                                Grid.SetColumn(can2, j + 1);
                                Grid.SetRowSpan(can2, data.States[dependencies[depInd][i].Index].Length + 1);
                                grd.Children.Add(can2);
                            }



                            for (int k = 0; k < data.States[dependencies[depInd][i].Index].Length; k++)
                            {
                                if (j != k)
                                {
                                    Parameter currRate = rates[dependencies[depInd][i].Index][data.States[dependencies[depInd][i].Index][j] + ">" + data.States[dependencies[depInd][i].Index][k]];

                                    colInds.Add(data.States[dependencies[depInd][i].Index][j] + ">" + data.States[dependencies[depInd][i].Index][k], colInd);

                                    if (currRate.Action == Parameter.ParameterAction.ML)
                                    {
                                        Border bord = new Border() { Background = Program.GetBrush(Plotting.GetColor(colInd++, 0.25, rateCount)), BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new Thickness(1.5), Padding = new Thickness(10, 0, 10, 0), CornerRadius = new CornerRadius(17.5), MinHeight = 35, Margin = new Thickness(5) };
                                        bord.Child = new TextBlock() { Text = "ML", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };

                                        ToolTip.SetTip(bord, "Maximum-likelihood");

                                        Grid.SetRow(bord, j + 1);
                                        Grid.SetColumn(bord, k + 1);
                                        grd.Children.Add(bord);
                                    }
                                    else if (currRate.Action == Parameter.ParameterAction.Fix)
                                    {
                                        Border bord = new Border() { Background = Program.GetBrush(Plotting.GetColor(colInd++, 0.25, rateCount)), BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new Thickness(1.5), Padding = new Thickness(10, 0, 10, 0), MinHeight = 35, Margin = new Thickness(5) };
                                        bord.Child = new TextBlock() { Text = currRate.Value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };

                                        ToolTip.SetTip(bord, "Fixed(" + currRate.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")");

                                        Grid.SetRow(bord, j + 1);
                                        Grid.SetColumn(bord, k + 1);
                                        grd.Children.Add(bord);
                                    }
                                    else if (currRate.Action == Parameter.ParameterAction.Bayes)
                                    {
                                        int parInd = parameterIndices[dependencies[depInd][i].Index][dependencies[depInd][i].InputDependencyName + ":" + data.States[dependencies[depInd][i].Index][j] + ">" + data.States[dependencies[depInd][i].Index][k]];

                                        double binWidth = (rateRange[1] - rateRange[0]) / 50;

                                        Canvas can = new Canvas() { Width = 50, Height = 35, Margin = new Thickness(10), Background = new SolidColorBrush(Colors.White) };

                                        double[] computedYs = new double[50];

                                        double[] values = (from el in parameters select el[dependencies[depInd][i].Index][parInd]).ToArray();

                                        (double mean, double variance) meanAndVariance = values.MeanAndVariance();

                                        for (int x = 0; x < 50; x++)
                                        {
                                            computedYs[x] = (from el in values where el > rateRange[0] + x * binWidth && el <= rateRange[0] + (x + 1) * binWidth select 1).Count();
                                        }

                                        double maxY = computedYs.Max();

                                        PathFigure fig = new PathFigure();
                                        PathFigure fillFig = new PathFigure() { StartPoint = new Point(0, 35) };

                                        bool startedFig = false;

                                        for (int x = 0; x < 50; x++)
                                        {
                                            if (!double.IsNaN(computedYs[x]))
                                            {
                                                if (!startedFig)
                                                {
                                                    fig.StartPoint = new Point(x, 35 - computedYs[x] / maxY * 35);
                                                    startedFig = true;
                                                }
                                                else
                                                {
                                                    fig.Segments.Add(new LineSegment() { Point = new Point(x, 35 - computedYs[x] / maxY * 35) });
                                                }

                                                fillFig.Segments.Add(new LineSegment() { Point = new Point(x, 35 - computedYs[x] / maxY * 35) });
                                            }
                                            else
                                            {
                                                if (!startedFig)
                                                {
                                                    fillFig.StartPoint = new Point(x, 35);
                                                }
                                            }
                                        }

                                        fillFig.Segments.Add(new LineSegment() { Point = new Point(49, 35) });
                                        fillFig.IsClosed = true;

                                        fig.IsClosed = false;

                                        PathGeometry geo = new PathGeometry();
                                        geo.Figures.Add(fig);

                                        PathGeometry fillGeo = new PathGeometry();
                                        fillGeo.Figures.Add(fillFig);

                                        can.Children.Add(new Path() { Data = fillGeo, Fill = Program.GetBrush(Plotting.GetColor(colInd, 0.25, rateCount)) });
                                        can.Children.Add(new Path() { Data = geo, Stroke = Program.GetBrush(Plotting.GetColor(colInd++, 1, rateCount)), StrokeThickness = 2, StrokeJoin = PenLineJoin.Round });

                                        Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

                                        ToolTip.SetTip(can, "μ = " + meanAndVariance.mean.ToString(3, false) + "    σ = " + Math.Sqrt(meanAndVariance.variance).ToString(3, false));

                                        Grid.SetRow(can, j + 1);
                                        Grid.SetColumn(can, k + 1);
                                        grd.Children.Add(can);
                                    }
                                }
                            }
                        }

                        for (int j = 0; j < data.States[dependencies[depInd][i].Index].Length; j++)
                        {
                            for (int k = 0; k < data.States[dependencies[depInd][i].Index].Length; k++)
                            {
                                if (j != k)
                                {
                                    Parameter currRate = rates[dependencies[depInd][i].Index][data.States[dependencies[depInd][i].Index][j] + ">" + data.States[dependencies[depInd][i].Index][k]];
                                    if (currRate.Action == Parameter.ParameterAction.Equal)
                                    {
                                        string equalName = (from el in rates[dependencies[depInd][i].Index] where el.Value == currRate.EqualParameter select el.Key).FirstOrDefault();

                                        FormattedText txt = new FormattedText() { Text = equalName.Replace(">", " > "), Typeface = new Typeface(this.FontFamily, this.FontSize) };

                                        PathFigure fig = new PathFigure() { StartPoint = new Point(0, 17.5) };

                                        fig.Segments.Add(new LineSegment() { Point = new Point(10, 0) });
                                        fig.Segments.Add(new LineSegment() { Point = new Point(txt.Bounds.Width + 20 - 10, 0) });
                                        fig.Segments.Add(new LineSegment() { Point = new Point(txt.Bounds.Width + 20, 17.5) });
                                        fig.Segments.Add(new LineSegment() { Point = new Point(txt.Bounds.Width + 20 - 10, 35) });
                                        fig.Segments.Add(new LineSegment() { Point = new Point(10, 35) });

                                        fig.IsClosed = true;

                                        PathGeometry geo = new PathGeometry();
                                        geo.Figures.Add(fig);

                                        Canvas can = new Canvas() { Width = txt.Bounds.Width + 20, Height = 35, Margin = new Thickness(5) };

                                        can.Children.Add(new Path() { Data = geo, Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 1.5, Fill = Program.GetBrush(Plotting.GetColor(colInds[equalName], 0.25, rateCount)) });

                                        can.Children.Add(new TextBlock() { Text = equalName.Replace(">", " > "), Margin = new Thickness(10, (35 - txt.Bounds.Height) / 2, 0, 0) });

                                        ToolTip.SetTip(can, "Equal(" + equalName + ")");

                                        Grid.SetRow(can, j + 1);
                                        Grid.SetColumn(can, k + 1);
                                        grd.Children.Add(can);
                                    }
                                }
                            }
                        }

                        mainContainer.Children.Add(grd);
                    }
                }
            }


        }

        private async void ParseSourceClicked(object sender, RoutedEventArgs e)
        {
            Dictionary<string, Parameter>[] prevRates = Rates;
            try
            {
                System.IO.Stream stream = new System.IO.MemoryStream();
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(stream))
                {
                    sw.Write("#NEXUS\n");
                    sw.Write(this.FindControl<TextBox>("SourceBox").Text);
                    sw.Flush();

                    stream.Position = 0;
                    using (System.IO.StreamReader sr = new System.IO.StreamReader(stream))
                    {
                        Rates = Parsing.ParseRateFile(sr, States, new ThreadSafeRandom());
                    }
                }
                BuildWindow();
            }
            catch (Exception ex)
            {
                await new MessageBox("Error!", "Error: " + ex.Message).ShowDialog(this);
                Rates = prevRates;
                BuildWindow();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
