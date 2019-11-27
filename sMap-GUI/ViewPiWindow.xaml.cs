using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Linq;
using System.Collections.Generic;
using Utils;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using MathNet.Numerics.Distributions;
using Avalonia.Interactivity;

namespace sMap_GUI
{
    public class ViewPiWindow : Window
    {
        public ViewPiWindow()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        public IBrush HeaderBrush
        {
            get
            {
                return this.FindControl<Path>("PrePath").Stroke;
            }

            set
            {
                this.FindControl<Path>("PrePath").Stroke = value;
                this.FindControl<Path>("PostPath").Stroke = value;
                this.FindControl<Path>("BgPath1").Fill = value;
                this.FindControl<Path>("BgPath2").Fill = value;
                this.FindControl<Canvas>("BgCanvas").Background = value;
                this.FindControl<TextBlock>("HeaderBlock").Foreground = value;
            }
        }

        public string Header
        {
            get
            {
                return this.FindControl<TextBlock>("HeaderBlock").Text;
            }

            set
            {
                this.FindControl<TextBlock>("HeaderBlock").Text = value;
            }
        }






        public ViewPiWindow(string[][] states, CharacterDependency[][] dependencies, Dictionary<string, Parameter>[] pi, bool isCondProbs = false, bool isEditable = false)
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            HeaderBrush = new SolidColorBrush(Color.FromArgb(255, 163, 73, 164));

            Dependencies = dependencies;
            States = states;
            Pi = pi;

            IsEditable = isEditable;
            IsCondProbs = isCondProbs;

            if (IsEditable)
            {
                this.Width = 800;
                this.FindControl<Grid>("MainGrid").ColumnDefinitions[1].Width = new GridLength(0.75, GridUnitType.Star);
                this.FindControl<Grid>("SourceGrid").IsVisible = true;
            }

            BuildWindow();


        }



        CharacterDependency[][] Dependencies;
        string[][] States;
        public Dictionary<string, Parameter>[] Pi;
        bool IsEditable = false;
        bool IsCondProbs = false;

        private void BuildWindow()
        {
            ThreadSafeRandom rand = new ThreadSafeRandom();

            Grid mainContainer = this.FindControl<Grid>("PisContainer");

            mainContainer.Children.Clear();
            mainContainer.RowDefinitions.Clear();

            if (this.IsEditable)
            {
                if (!IsCondProbs)
                {
                    this.FindControl<TextBox>("SourceBox").Text = Utils.Utils.GetPisSource(Dependencies, Pi).Replace("\t", "    "); ;
                }
                else
                {
                    this.FindControl<TextBox>("SourceBox").Text = Utils.Utils.GetDependencySource(Dependencies, false, false, true);
                }
            }

            for (int depInd = 0; depInd < Dependencies.Length; depInd++)
            {
                mainContainer.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));

                {
                    TextBlock blk = new TextBlock() { Text = "Set " + depInd.ToString() + ":", FontWeight = FontWeight.Bold, FontSize = 20, Margin = new Thickness(0, 20, 0, 0) };
                    Grid.SetRow(blk, mainContainer.RowDefinitions.Count - 1);
                    Grid.SetColumnSpan(blk, 4);
                    mainContainer.Children.Add(blk);
                }

                for (int i = 0; i < Dependencies[depInd].Length; i++)
                {

                    if (Dependencies[depInd][i].Type != CharacterDependency.Types.Conditioned && !IsCondProbs)
                    {

                        mainContainer.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));

                        {
                            TextBlock blk = new TextBlock() { Text = "Character" + (Dependencies[depInd][i].InputDependencyName.Contains(",") ? "s" : "") + " " + Dependencies[depInd][i].InputDependencyName + ":", FontWeight = FontWeight.Bold, FontSize = 18, Margin = new Thickness(20, 5, 0, 0) };
                            Grid.SetRow(blk, mainContainer.RowDefinitions.Count - 1);
                            Grid.SetColumnSpan(blk, 4);
                            mainContainer.Children.Add(blk);
                        }

                        double lastAngle = 0;

                        for (int j = 0; j < States[Dependencies[depInd][i].Index].Length; j++)
                        {
                            mainContainer.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));

                            {
                                TextBlock blk = new TextBlock() { Text = States[Dependencies[depInd][i].Index][j], Margin = new Thickness(40, 0, 10, 0), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                                Grid.SetRow(blk, mainContainer.RowDefinitions.Count - 1);
                                mainContainer.Children.Add(blk);
                            }

                            Parameter currPi = Pi[Dependencies[depInd][i].Index][States[Dependencies[depInd][i].Index][j]];

                            Dictionary<string, Parameter> currPis = Pi[Dependencies[depInd][i].Index];

                            if (currPi.Action == Parameter.ParameterAction.Fix)
                            {
                                Canvas can = new Canvas() { Width = 24, Height = 24, Margin = new Thickness(20, 5, 20, 5) };

                                can.Children.Add(new Ellipse() { Fill = new SolidColorBrush(Color.FromArgb(255, 230, 230, 230)), Width = 24, Height = 24 });

                                if (currPi.Value >= 0.01 && currPi.Value <= 0.99)
                                {
                                    PathFigure fig = new PathFigure() { StartPoint = new Point(12, 12) };
                                    fig.Segments.Add(new LineSegment() { Point = new Point(12 + 12 * Math.Cos(lastAngle * 2 * Math.PI), 12 + 12 * Math.Sin(lastAngle * 2 * Math.PI)) });
                                    fig.Segments.Add(new ArcSegment() { Point = new Point(12 + 12 * Math.Cos((lastAngle + currPi.Value) * 2 * Math.PI), 12 + 12 * Math.Sin((lastAngle + currPi.Value) * 2 * Math.PI)), Size = new Size(12, 12), SweepDirection = SweepDirection.Clockwise, IsLargeArc = currPi.Value > 0.5 });
                                    fig.IsClosed = true;

                                    PathGeometry geo = new PathGeometry();
                                    geo.Figures.Add(fig);

                                    Path pth = new Path() { Data = geo, Fill = Program.GetBrush(Plotting.GetColor(j, 1, States[Dependencies[depInd][i].Index].Length)) };
                                    can.Children.Add(pth);

                                }
                                else if (currPi.Value > 0.99)
                                {
                                    can.Children.Add(new Ellipse() { Fill = Program.GetBrush(Plotting.GetColor(j, 1, States[Dependencies[depInd][i].Index].Length)), Width = 24, Height = 24 });
                                }

                                can.Children.Add(new Ellipse() { Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 1.5, Width = 24, Height = 24 });

                                Grid.SetRow(can, mainContainer.RowDefinitions.Count - 1);
                                Grid.SetColumn(can, 1);

                                mainContainer.Children.Add(can);

                                {
                                    TextBlock blk = new TextBlock() { Text = currPi.Value.ToString("0.#%", System.Globalization.CultureInfo.InvariantCulture), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                                    Grid.SetRow(blk, mainContainer.RowDefinitions.Count - 1);
                                    Grid.SetColumn(blk, 2);
                                    mainContainer.Children.Add(blk);
                                }


                                if (IsEditable)
                                {
                                    Button btn = new Button() { Content = "...", Padding = new Thickness(5, 5, 5, 5), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(15, 5, 5, 5) };
                                    Grid.SetRow(btn, mainContainer.RowDefinitions.Count - 1);
                                    Grid.SetColumn(btn, 3);
                                    mainContainer.Children.Add(btn);

                                    btn.Click += (s, e) =>
                                    {
                                        EditPi(btn, currPi, currPis);
                                    };
                                }

                                lastAngle += currPi.Value;
                            }
                            else if (currPi.Action == Parameter.ParameterAction.Dirichlet)
                            {
                                Canvas can = new Canvas() { Width = 100, Height = 48, Margin = new Thickness(20, 5, 20, 5) };

                                double[] computedYs = new double[100];

                                Beta beta = new Beta(currPi.DistributionParameter, (from el in Pi[Dependencies[depInd][i].Index] where el.Value.Action == Parameter.ParameterAction.Dirichlet select el.Value.DistributionParameter).Sum() - currPi.DistributionParameter, rand);

                                for (int x = 0; x < 100; x++)
                                {
                                    double realX = x / 99.0;

                                    computedYs[x] = beta.Density(realX);
                                }

                                PathFigure fig = new PathFigure();
                                PathFigure fillFig = new PathFigure() { StartPoint = new Point(0, 48) };

                                double maxY = Math.Min(computedYs.Max(), 10);

                                for (int x = 0; x < 100; x++)
                                {
                                    if (x == 0)
                                    {
                                        fig.StartPoint = new Point(x, 48 - 48 * Math.Min(computedYs[x] / maxY, 1));
                                    }
                                    else
                                    {
                                        fig.Segments.Add(new LineSegment() { Point = new Point(x, 48 - 48 * Math.Min(computedYs[x] / maxY, 1)) });
                                    }

                                    fillFig.Segments.Add(new LineSegment() { Point = new Point(x, 48 - 48 * Math.Min(computedYs[x] / maxY, 1)) });
                                }

                                fillFig.Segments.Add(new LineSegment() { Point = new Point(99, 48) });

                                fig.IsClosed = false;
                                fillFig.IsClosed = true;

                                PathGeometry geo = new PathGeometry();
                                geo.Figures.Add(fig);

                                PathGeometry fillGeo = new PathGeometry();
                                fillGeo.Figures.Add(fillFig);

                                can.Children.Add(new Path() { Data = fillGeo, Fill = Program.GetBrush(Plotting.GetColor(j, 0.25, States[Dependencies[depInd][i].Index].Length)) });
                                can.Children.Add(new Path() { Data = geo, Stroke = Program.GetBrush(Plotting.GetColor(j, 1, States[Dependencies[depInd][i].Index].Length)), StrokeThickness = 2 });

                                Grid.SetRow(can, mainContainer.RowDefinitions.Count - 1);
                                Grid.SetColumn(can, 1);

                                mainContainer.Children.Add(can);

                                {
                                    TextBlock blk = new TextBlock() { Text = "Dirichlet(" + currPi.DistributionParameter.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                                    Grid.SetRow(blk, mainContainer.RowDefinitions.Count - 1);
                                    Grid.SetColumn(blk, 2);
                                    mainContainer.Children.Add(blk);
                                }

                                if (IsEditable)
                                {
                                    Button btn = new Button() { Content = "...", Padding = new Thickness(5, 5, 5, 5), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(15, 5, 5, 5) };
                                    Grid.SetRow(btn, mainContainer.RowDefinitions.Count - 1);
                                    Grid.SetColumn(btn, 3);
                                    mainContainer.Children.Add(btn);

                                    btn.Click += (s, e) =>
                                    {
                                        EditPi(btn, currPi, currPis);
                                    };
                                }

                                lastAngle += currPi.Value;
                            }
                            else if (currPi.Action == Parameter.ParameterAction.Equal)
                            {
                                Canvas can = new Canvas() { Width = 24, Height = 24, Margin = new Thickness(20, 5, 20, 5) };

                                PathFigure fig = new PathFigure() { StartPoint = new Point(24, 12) };

                                for (int k = 1; k <= 6; k++)
                                {
                                    fig.Segments.Add(new LineSegment() { Point = new Point(12 + 12 * Math.Cos(Math.PI / 3 * k), 12 + 12 * Math.Sin(Math.PI / 3 * k)) });
                                }

                                PathGeometry geo = new PathGeometry();
                                geo.Figures.Add(fig);
                                can.Children.Add(new Path() { Data = geo, Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 1.5, Fill = Program.GetTransparentBrush(Plotting.GetColor(j, 0.5, States[Dependencies[depInd][i].Index].Length)) });

                                can.Children.Add(new Viewbox() { Width = 16, Height = 16, Child = new PiMenuIcon() { IconType = PiMenuIcon.IconTypes.Equal }, Margin = new Thickness(4) });

                                Grid.SetRow(can, mainContainer.RowDefinitions.Count - 1);
                                Grid.SetColumn(can, 1);



                                mainContainer.Children.Add(can);

                                {
                                    TextBlock blk = new TextBlock() { Text = "Equal(" + currPis.GetKey(currPi.EqualParameter) + ")", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                                    Grid.SetRow(blk, mainContainer.RowDefinitions.Count - 1);
                                    Grid.SetColumn(blk, 2);
                                    mainContainer.Children.Add(blk);
                                }


                                if (IsEditable)
                                {
                                    Button btn = new Button() { Content = "...", Padding = new Thickness(5, 5, 5, 5), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(15, 5, 5, 5) };
                                    Grid.SetRow(btn, mainContainer.RowDefinitions.Count - 1);
                                    Grid.SetColumn(btn, 3);
                                    mainContainer.Children.Add(btn);

                                    btn.Click += (s, e) =>
                                    {
                                        EditPi(btn, currPi, currPis);
                                    };
                                }
                            }
                        }
                    }
                    else if (Dependencies[depInd][i].Type == CharacterDependency.Types.Conditioned && IsCondProbs)
                    {
                        mainContainer.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));

                        {
                            TextBlock blk = new TextBlock() { Text = "Character" + (Dependencies[depInd][i].InputDependencyName.Contains(",") ? "s" : "") + " " + Dependencies[depInd][i].InputDependencyName + ":", FontWeight = FontWeight.Bold, FontSize = 18, Margin = new Thickness(20, 5, 0, 0) };
                            Grid.SetRow(blk, mainContainer.RowDefinitions.Count - 1);
                            Grid.SetColumnSpan(blk, 4);
                            mainContainer.Children.Add(blk);
                        }

                        double lastAngle = 0;

                        int colInd = 0;

                        foreach (KeyValuePair<string, Parameter> param in Dependencies[depInd][i].ConditionedProbabilities)
                        {
                            mainContainer.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));

                            {
                                string name = param.Key.Substring(param.Key.IndexOf(">") + 1) + " | " + param.Key.Substring(0, param.Key.IndexOf(">"));

                                TextBlock blk = new TextBlock() { Text = name, Margin = new Thickness(40, 0, 10, 0), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                                Grid.SetRow(blk, mainContainer.RowDefinitions.Count - 1);
                                mainContainer.Children.Add(blk);
                            }

                            Parameter currPi = param.Value;

                            Dictionary<string, Parameter> currPis = Dependencies[depInd][i].ConditionedProbabilities;

                            if (currPi.Action == Parameter.ParameterAction.Fix)
                            {
                                Canvas can = new Canvas() { Width = 24, Height = 24, Margin = new Thickness(20, 5, 20, 5) };

                                can.Children.Add(new Ellipse() { Fill = new SolidColorBrush(Color.FromArgb(255, 230, 230, 230)), Width = 24, Height = 24 });

                                if (currPi.Value >= 0.01 && currPi.Value <= 0.99)
                                {
                                    PathFigure fig = new PathFigure() { StartPoint = new Point(12, 12) };
                                    fig.Segments.Add(new LineSegment() { Point = new Point(12 + 12 * Math.Cos(lastAngle * 2 * Math.PI), 12 + 12 * Math.Sin(lastAngle * 2 * Math.PI)) });
                                    fig.Segments.Add(new ArcSegment() { Point = new Point(12 + 12 * Math.Cos((lastAngle + currPi.Value) * 2 * Math.PI), 12 + 12 * Math.Sin((lastAngle + currPi.Value) * 2 * Math.PI)), Size = new Size(12, 12), SweepDirection = SweepDirection.Clockwise, IsLargeArc = currPi.Value > 0.5 });
                                    fig.IsClosed = true;

                                    PathGeometry geo = new PathGeometry();
                                    geo.Figures.Add(fig);

                                    Path pth = new Path() { Data = geo, Fill = Program.GetBrush(Plotting.GetColor(colInd, 1, Dependencies[depInd][i].ConditionedProbabilities.Count)) };
                                    can.Children.Add(pth);

                                }
                                else if (currPi.Value > 0.99)
                                {
                                    can.Children.Add(new Ellipse() { Fill = Program.GetBrush(Plotting.GetColor(colInd, 1, Dependencies[depInd][i].ConditionedProbabilities.Count)), Width = 24, Height = 24 });
                                }

                                can.Children.Add(new Ellipse() { Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 1.5, Width = 24, Height = 24 });

                                Grid.SetRow(can, mainContainer.RowDefinitions.Count - 1);
                                Grid.SetColumn(can, 1);

                                mainContainer.Children.Add(can);

                                {
                                    TextBlock blk = new TextBlock() { Text = currPi.Value.ToString("0.#%", System.Globalization.CultureInfo.InvariantCulture), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                                    Grid.SetRow(blk, mainContainer.RowDefinitions.Count - 1);
                                    Grid.SetColumn(blk, 2);
                                    mainContainer.Children.Add(blk);
                                }


                                if (IsEditable)
                                {
                                    Button btn = new Button() { Content = "...", Padding = new Thickness(5, 5, 5, 5), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(15, 5, 5, 5) };
                                    Grid.SetRow(btn, mainContainer.RowDefinitions.Count - 1);
                                    Grid.SetColumn(btn, 3);
                                    mainContainer.Children.Add(btn);

                                    btn.Click += (s, e) =>
                                    {
                                        EditCondProbs(btn, currPi, currPis);
                                    };
                                }

                                lastAngle += currPi.Value;
                            }
                            else if (currPi.Action == Parameter.ParameterAction.Dirichlet)
                            {
                                string currCondState = currPis.GetKey(currPi).Substring(0, currPis.GetKey(currPi).IndexOf(">"));

                                Dictionary<string, Parameter> relevantProbs = new Dictionary<string, Parameter>();

                                foreach (KeyValuePair<string, Parameter> kvp in currPis)
                                {
                                    if (kvp.Key.StartsWith(currCondState + ">"))
                                    {
                                        relevantProbs.Add(kvp.Key, kvp.Value);
                                    }
                                }


                                Canvas can = new Canvas() { Width = 100, Height = 48, Margin = new Thickness(20, 5, 20, 5) };

                                double[] computedYs = new double[100];

                                Beta beta = new Beta(currPi.DistributionParameter, (from el in relevantProbs where el.Value.Action == Parameter.ParameterAction.Dirichlet select el.Value.DistributionParameter).Sum() - currPi.DistributionParameter, rand);

                                for (int x = 0; x < 100; x++)
                                {
                                    double realX = x / 99.0;

                                    computedYs[x] = beta.Density(realX);
                                }

                                PathFigure fig = new PathFigure();
                                PathFigure fillFig = new PathFigure() { StartPoint = new Point(0, 48) };

                                double maxY = Math.Min(computedYs.Max(), 10);

                                for (int x = 0; x < 100; x++)
                                {
                                    if (x == 0)
                                    {
                                        fig.StartPoint = new Point(x, 48 - 48 * Math.Min(computedYs[x] / maxY, 1));
                                    }
                                    else
                                    {
                                        fig.Segments.Add(new LineSegment() { Point = new Point(x, 48 - 48 * Math.Min(computedYs[x] / maxY, 1)) });
                                    }

                                    fillFig.Segments.Add(new LineSegment() { Point = new Point(x, 48 - 48 * Math.Min(computedYs[x] / maxY, 1)) });
                                }

                                fillFig.Segments.Add(new LineSegment() { Point = new Point(99, 48) });

                                fig.IsClosed = false;
                                fillFig.IsClosed = true;

                                PathGeometry geo = new PathGeometry();
                                geo.Figures.Add(fig);

                                PathGeometry fillGeo = new PathGeometry();
                                fillGeo.Figures.Add(fillFig);

                                can.Children.Add(new Path() { Data = fillGeo, Fill = Program.GetBrush(Plotting.GetColor(colInd, 0.25, Dependencies[depInd][i].ConditionedProbabilities.Count)) });
                                can.Children.Add(new Path() { Data = geo, Stroke = Program.GetBrush(Plotting.GetColor(colInd, 1, Dependencies[depInd][i].ConditionedProbabilities.Count)), StrokeThickness = 2 });

                                Grid.SetRow(can, mainContainer.RowDefinitions.Count - 1);
                                Grid.SetColumn(can, 1);

                                mainContainer.Children.Add(can);

                                {
                                    TextBlock blk = new TextBlock() { Text = "Dirichlet(" + currPi.DistributionParameter.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                                    Grid.SetRow(blk, mainContainer.RowDefinitions.Count - 1);
                                    Grid.SetColumn(blk, 2);
                                    mainContainer.Children.Add(blk);
                                }

                                if (IsEditable)
                                {
                                    Button btn = new Button() { Content = "...", Padding = new Thickness(5, 5, 5, 5), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(15, 5, 5, 5) };
                                    Grid.SetRow(btn, mainContainer.RowDefinitions.Count - 1);
                                    Grid.SetColumn(btn, 3);
                                    mainContainer.Children.Add(btn);

                                    btn.Click += (s, e) =>
                                    {
                                        EditCondProbs(btn, currPi, currPis);
                                    };
                                }

                                lastAngle += currPi.Value;
                            }
                            else if (currPi.Action == Parameter.ParameterAction.Multinomial)
                            {
                                string currCondState = currPis.GetKey(currPi).Substring(0, currPis.GetKey(currPi).IndexOf(">"));

                                Dictionary<string, Parameter> relevantProbs = new Dictionary<string, Parameter>();

                                List<double> ps = new List<double>();
                                int currInd = -1;

                                foreach (KeyValuePair<string, Parameter> kvp in currPis)
                                {
                                    if (kvp.Key.StartsWith(currCondState + ">"))
                                    {
                                        relevantProbs.Add(kvp.Key, kvp.Value);
                                        if (kvp.Value.Action == Parameter.ParameterAction.Multinomial)
                                        {
                                            ps.Add(kvp.Value.DistributionParameter);
                                            if (kvp.Value == currPi)
                                            {
                                                currInd = ps.Count - 1;
                                            }
                                        }
                                    }
                                }


                                Canvas can = new Canvas() { Width = 100, Height = 48, Margin = new Thickness(20, 5, 20, 5) };

                                Categorical categorical = new Categorical(ps.ToArray(), rand);

                                double prob = categorical.Probability(currInd);

                                double y0 = 48 - 48 * (1 - prob);
                                double y1 = 48 - 48 * prob;

                                PathFigure fig = new PathFigure() { StartPoint = new Point(0, y0) };
                                PathFigure fillFig = new PathFigure() { StartPoint = new Point(0, 48) };

                                fillFig.Segments.Add(new LineSegment() { Point = new Point(0, y0) });

                                fillFig.Segments.Add(new LineSegment() { Point = new Point(15, y0) });
                                fig.Segments.Add(new LineSegment() { Point = new Point(15, y0) });

                                fillFig.Segments.Add(new LineSegment() { Point = new Point(15, 48) });
                                fig.Segments.Add(new LineSegment() { Point = new Point(15, 48) });

                                fillFig.Segments.Add(new LineSegment() { Point = new Point(85, 48) });
                                fig.Segments.Add(new LineSegment() { Point = new Point(85, 48) });

                                fillFig.Segments.Add(new LineSegment() { Point = new Point(85, y1) });
                                fig.Segments.Add(new LineSegment() { Point = new Point(85, y1) });

                                fillFig.Segments.Add(new LineSegment() { Point = new Point(99, y1) });
                                fig.Segments.Add(new LineSegment() { Point = new Point(99, y1) });

                                fillFig.Segments.Add(new LineSegment() { Point = new Point(99, 48) });

                                fig.IsClosed = false;
                                fillFig.IsClosed = true;

                                PathGeometry geo = new PathGeometry();
                                geo.Figures.Add(fig);

                                PathGeometry fillGeo = new PathGeometry();
                                fillGeo.Figures.Add(fillFig);

                                can.Children.Add(new Path() { Data = fillGeo, Fill = Program.GetBrush(Plotting.GetColor(colInd, 0.25, Dependencies[depInd][i].ConditionedProbabilities.Count)) });
                                can.Children.Add(new Path() { Data = geo, Stroke = Program.GetBrush(Plotting.GetColor(colInd, 1, Dependencies[depInd][i].ConditionedProbabilities.Count)), StrokeThickness = 2 });

                                Grid.SetRow(can, mainContainer.RowDefinitions.Count - 1);
                                Grid.SetColumn(can, 1);

                                mainContainer.Children.Add(can);

                                {
                                    TextBlock blk = new TextBlock() { Text = "Multinomial(" + currPi.DistributionParameter.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                                    Grid.SetRow(blk, mainContainer.RowDefinitions.Count - 1);
                                    Grid.SetColumn(blk, 2);
                                    mainContainer.Children.Add(blk);
                                }

                                if (IsEditable)
                                {
                                    Button btn = new Button() { Content = "...", Padding = new Thickness(5, 5, 5, 5), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(15, 5, 5, 5) };
                                    Grid.SetRow(btn, mainContainer.RowDefinitions.Count - 1);
                                    Grid.SetColumn(btn, 3);
                                    mainContainer.Children.Add(btn);

                                    btn.Click += (s, e) =>
                                    {
                                        EditCondProbs(btn, currPi, currPis);
                                    };
                                }

                                lastAngle += currPi.Value;
                            }
                            else if (currPi.Action == Parameter.ParameterAction.Equal)
                            {
                                Canvas can = new Canvas() { Width = 24, Height = 24, Margin = new Thickness(20, 5, 20, 5) };

                                PathFigure fig = new PathFigure() { StartPoint = new Point(24, 12) };

                                for (int k = 1; k <= 6; k++)
                                {
                                    fig.Segments.Add(new LineSegment() { Point = new Point(12 + 12 * Math.Cos(Math.PI / 3 * k), 12 + 12 * Math.Sin(Math.PI / 3 * k)) });
                                }

                                PathGeometry geo = new PathGeometry();
                                geo.Figures.Add(fig);
                                can.Children.Add(new Path() { Data = geo, Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 1.5, Fill = Program.GetTransparentBrush(Plotting.GetColor(colInd, 0.5, Dependencies[depInd][i].ConditionedProbabilities.Count)) });

                                can.Children.Add(new Viewbox() { Width = 16, Height = 16, Child = new PiMenuIcon() { IconType = PiMenuIcon.IconTypes.Equal }, Margin = new Thickness(4) });

                                Grid.SetRow(can, mainContainer.RowDefinitions.Count - 1);
                                Grid.SetColumn(can, 1);



                                mainContainer.Children.Add(can);

                                {
                                    TextBlock blk = new TextBlock() { Text = "Equal(" + currPis.GetKey(currPi.EqualParameter) + ")", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                                    Grid.SetRow(blk, mainContainer.RowDefinitions.Count - 1);
                                    Grid.SetColumn(blk, 2);
                                    mainContainer.Children.Add(blk);
                                }


                                if (IsEditable)
                                {
                                    Button btn = new Button() { Content = "...", Padding = new Thickness(5, 5, 5, 5), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(15, 5, 5, 5) };
                                    Grid.SetRow(btn, mainContainer.RowDefinitions.Count - 1);
                                    Grid.SetColumn(btn, 3);
                                    mainContainer.Children.Add(btn);

                                    btn.Click += (s, e) =>
                                    {
                                        EditCondProbs(btn, currPi, currPis);
                                    };
                                }
                            }
                            else if (currPi.Action == Parameter.ParameterAction.ML)
                            {
                                Canvas can = new Canvas() { Width = 24, Height = 24, Margin = new Thickness(20, 5, 20, 5) };

                                can.Children.Add(new Ellipse() { Width = 24, Height = 24, Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 1.5, Fill = Program.GetTransparentBrush(Plotting.GetColor(colInd, 0.5, Dependencies[depInd][i].ConditionedProbabilities.Count)) });

                                can.Children.Add(new Viewbox() { Width = 16, Height = 16, Child = new TextBlock() { Text = "ML", FontWeight = FontWeight.Bold, Margin = new Thickness(0), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center }, Margin = new Thickness(4) });

                                Grid.SetRow(can, mainContainer.RowDefinitions.Count - 1);
                                Grid.SetColumn(can, 1);

                                mainContainer.Children.Add(can);

                                {
                                    TextBlock blk = new TextBlock() { Text = "Maximum-likelihood", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                                    Grid.SetRow(blk, mainContainer.RowDefinitions.Count - 1);
                                    Grid.SetColumn(blk, 2);
                                    mainContainer.Children.Add(blk);
                                }


                                if (IsEditable)
                                {
                                    Button btn = new Button() { Content = "...", Padding = new Thickness(5, 5, 5, 5), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(15, 5, 5, 5) };
                                    Grid.SetRow(btn, mainContainer.RowDefinitions.Count - 1);
                                    Grid.SetColumn(btn, 3);
                                    mainContainer.Children.Add(btn);

                                    btn.Click += (s, e) =>
                                    {
                                        EditCondProbs(btn, currPi, currPis);
                                    };
                                }
                            }

                            colInd++;
                        }
                    }
                }
            }
        }


        void EditPi(Button btn, Parameter pi, Dictionary<string, Parameter> charPis)
        {
            int currPiWeight = 1;

            foreach (KeyValuePair<string, Parameter> kvp in charPis)
            {
                if (kvp.Value.Action == Parameter.ParameterAction.Equal && kvp.Value.EqualParameter == pi)
                {
                    currPiWeight++;
                }
            }


            MenuItem fixItem = new MenuItem() { Header = "Fix", Icon = new PiMenuIcon() { IconType = PiMenuIcon.IconTypes.Fix } };
            NumericUpDown fixValue = new NumericUpDown() { Minimum = 0, Maximum = 1.0 / currPiWeight, Increment = 0.1, Value = pi.Value, Padding = new Thickness(5, 0, 5, 0), Width = 100, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            fixValue.PointerReleased += (s, e) =>
            {
                e.Handled = true;
            };

            fixValue.KeyDown += (s, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Return)
                {
                    e.Handled = true;
                }
            };

            MenuItem fixValueItem = new MenuItem() { Header = fixValue, Icon = new PiMenuIcon() { IconType = PiMenuIcon.IconTypes.Fix } };
            fixValueItem.PointerReleased += (s, e) =>
            {
                e.Handled = true;
            };

            MenuItem fixOkItem = new MenuItem() { Header = "OK", Icon = new Viewbox() { Child = new Octagon() { Fill = new SolidColorBrush(Color.FromArgb(255, 34, 177, 76)), IsTick = true } } };
            fixOkItem.Click += (s, e) =>
            {
                pi.Action = Parameter.ParameterAction.Fix;
                pi.Value = fixValue.Value;

                double totalPi = 0;

                foreach (KeyValuePair<string, Parameter> kvp in charPis)
                {
                    if (kvp.Value.Action == Parameter.ParameterAction.Fix)
                    {
                        totalPi += kvp.Value.Value;
                    }
                    else if (kvp.Value.Action == Parameter.ParameterAction.Equal)
                    {
                        totalPi += kvp.Value.EqualParameter.Value;
                    }
                }

                if (totalPi > 1)
                {
                    double remainingPi = 1 - pi.Value;
                    double otherPi = totalPi - pi.Value;

                    foreach (KeyValuePair<string, Parameter> kvp in charPis)
                    {
                        if (kvp.Value != pi)
                        {
                            if (kvp.Value.Action == Parameter.ParameterAction.Fix)
                            {
                                int piWeight = 1;

                                foreach (KeyValuePair<string, Parameter> kvp2 in charPis)
                                {
                                    if (kvp2.Value.Action == Parameter.ParameterAction.Equal && kvp2.Value.EqualParameter == kvp.Value)
                                    {
                                        piWeight++;
                                    }
                                }

                                kvp.Value.Value = kvp.Value.Value / (piWeight * otherPi) * remainingPi;
                            }
                        }
                    }
                }
                else if (totalPi < 1)
                {
                    bool foundDirichlet = false;
                    foreach (KeyValuePair<string, Parameter> kvp in charPis)
                    {
                        if (kvp.Value.Action == Parameter.ParameterAction.Dirichlet)
                        {
                            foundDirichlet = true;
                        }
                    }

                    if (!foundDirichlet)
                    {
                        double remainingPi = 1 - pi.Value;
                        double otherPi = totalPi - pi.Value;

                        foreach (KeyValuePair<string, Parameter> kvp in charPis)
                        {
                            if (kvp.Value != pi)
                            {
                                if (kvp.Value.Action == Parameter.ParameterAction.Fix)
                                {
                                    int piWeight = 1;

                                    foreach (KeyValuePair<string, Parameter> kvp2 in charPis)
                                    {
                                        if (kvp2.Value.Action == Parameter.ParameterAction.Equal && kvp2.Value.EqualParameter == kvp.Value)
                                        {
                                            piWeight++;
                                        }
                                    }

                                    kvp.Value.Value = kvp.Value.Value / (piWeight * otherPi) * remainingPi;
                                }
                            }
                        }
                    }
                }

                BuildWindow();
            };

            fixItem.Items = new List<MenuItem>() { fixValueItem, fixOkItem };

            MenuItem dirichletItem = new MenuItem() { Header = "Dirichlet", Icon = new PiMenuIcon() { IconType = PiMenuIcon.IconTypes.Dirichlet } };

            NumericUpDown dirichletValue = new NumericUpDown() { Minimum = 0, Increment = 0.1, Value = pi.DistributionParameter, Padding = new Thickness(5, 0, 5, 0), Width = 100, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            dirichletValue.PointerReleased += (s, e) =>
            {
                e.Handled = true;
            };


            dirichletValue.KeyDown += (s, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Return)
                {
                    e.Handled = true;
                }
            };

            MenuItem dirichletValueItem = new MenuItem() { Header = dirichletValue, Icon = new PiMenuIcon() { IconType = PiMenuIcon.IconTypes.Dirichlet } };

            dirichletValueItem.PointerReleased += (s, e) =>
            {
                e.Handled = true;
            };

            MenuItem dirichletOkItem = new MenuItem() { Header = "OK", Icon = new Viewbox() { Child = new Octagon() { Fill = new SolidColorBrush(Color.FromArgb(255, 34, 177, 76)), IsTick = true } } };
            dirichletOkItem.Click += (s, e) =>
            {
                pi.Action = Parameter.ParameterAction.Dirichlet;
                pi.DistributionParameter = dirichletValue.Value;

                BuildWindow();
            };

            dirichletItem.Items = new List<MenuItem>() { dirichletValueItem, dirichletOkItem };

            MenuItem equalItem = new MenuItem() { Header = "Equal", Icon = new PiMenuIcon() { IconType = PiMenuIcon.IconTypes.Equal } };

            List<MenuItem> equalItems = new List<MenuItem>();

            foreach (KeyValuePair<string, Parameter> kvp in charPis)
            {
                if (kvp.Value != pi && kvp.Value.Action != Parameter.ParameterAction.Equal)
                {
                    MenuItem currEqualItem = new MenuItem() { Header = kvp.Key, Icon = new PiMenuIcon() { IconType = PiMenuIcon.IconTypes.Equal } };
                    currEqualItem.Click += (s, e) =>
                    {
                        pi.Action = Parameter.ParameterAction.Equal;
                        pi.EqualParameter = kvp.Value;
                        BuildWindow();
                    };

                    equalItems.Add(currEqualItem);


                }
            }

            equalItem.Items = equalItems;

            if (equalItems.Count > 0)
            {
                ContextMenu menu = new ContextMenu() { Items = new List<MenuItem>() { fixItem, dirichletItem, equalItem } };
                menu.Open(btn);
            }
            else
            {
                ContextMenu menu = new ContextMenu() { Items = new List<MenuItem>() { fixItem, dirichletItem } };
                menu.Open(btn);
            }
        }

        void EditCondProbs(Button btn, Parameter prob, Dictionary<string, Parameter> charProbs)
        {
            string currCondState = charProbs.GetKey(prob).Substring(0, charProbs.GetKey(prob).IndexOf(">"));

            Dictionary<string, Parameter> relevantProbs = new Dictionary<string, Parameter>();

            foreach (KeyValuePair<string, Parameter> kvp in charProbs)
            {
                if (kvp.Key.StartsWith(currCondState + ">"))
                {
                    relevantProbs.Add(kvp.Key, kvp.Value);
                }
            }


            int currPiWeight = 1;

            foreach (KeyValuePair<string, Parameter> kvp in relevantProbs)
            {
                if (kvp.Value.Action == Parameter.ParameterAction.Equal && kvp.Value.EqualParameter == prob)
                {
                    currPiWeight++;
                }
            }

            MenuItem fixItem = new MenuItem() { Header = "Fix", Icon = new PiMenuIcon() { IconType = PiMenuIcon.IconTypes.Fix } };
            NumericUpDown fixValue = new NumericUpDown() { Minimum = 0, Maximum = 1.0 / currPiWeight, Increment = 0.1, Value = prob.Value, Padding = new Thickness(5, 0, 5, 0), Width = 100, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            fixValue.PointerReleased += (s, e) =>
            {
                e.Handled = true;
            };

            fixValue.KeyDown += (s, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Return)
                {
                    e.Handled = true;
                }
            };

            MenuItem fixValueItem = new MenuItem() { Header = fixValue, Icon = new PiMenuIcon() { IconType = PiMenuIcon.IconTypes.Fix } };
            fixValueItem.PointerReleased += (s, e) =>
            {
                e.Handled = true;
            };

            MenuItem fixOkItem = new MenuItem() { Header = "OK", Icon = new Viewbox() { Child = new Octagon() { Fill = new SolidColorBrush(Color.FromArgb(255, 34, 177, 76)), IsTick = true } } };
            fixOkItem.Click += (s, e) =>
            {
                prob.Action = Parameter.ParameterAction.Fix;
                prob.Value = fixValue.Value;

                double totalPi = 0;

                foreach (KeyValuePair<string, Parameter> kvp in relevantProbs)
                {
                    if (kvp.Value.Action == Parameter.ParameterAction.Fix)
                    {
                        totalPi += kvp.Value.Value;
                    }
                    else if (kvp.Value.Action == Parameter.ParameterAction.Equal)
                    {
                        totalPi += kvp.Value.EqualParameter.Value;
                    }
                }

                if (totalPi > 1)
                {
                    double remainingPi = 1 - prob.Value;
                    double otherPi = totalPi - prob.Value;

                    foreach (KeyValuePair<string, Parameter> kvp in relevantProbs)
                    {
                        if (kvp.Value != prob)
                        {
                            if (kvp.Value.Action == Parameter.ParameterAction.Fix)
                            {
                                int piWeight = 1;

                                foreach (KeyValuePair<string, Parameter> kvp2 in relevantProbs)
                                {
                                    if (kvp2.Value.Action == Parameter.ParameterAction.Equal && kvp2.Value.EqualParameter == kvp.Value)
                                    {
                                        piWeight++;
                                    }
                                }

                                kvp.Value.Value = kvp.Value.Value / (piWeight * otherPi) * remainingPi;
                            }
                        }
                    }
                }
                else if (totalPi < 1)
                {
                    bool foundDirichlet = false;
                    foreach (KeyValuePair<string, Parameter> kvp in relevantProbs)
                    {
                        if (kvp.Value.Action == Parameter.ParameterAction.Dirichlet || kvp.Value.Action == Parameter.ParameterAction.ML || kvp.Value.Action == Parameter.ParameterAction.Multinomial)
                        {
                            foundDirichlet = true;
                        }
                    }

                    if (!foundDirichlet)
                    {
                        double remainingPi = 1 - prob.Value;
                        double otherPi = totalPi - prob.Value;

                        foreach (KeyValuePair<string, Parameter> kvp in relevantProbs)
                        {
                            if (kvp.Value != prob)
                            {
                                if (kvp.Value.Action == Parameter.ParameterAction.Fix)
                                {
                                    int piWeight = 1;

                                    foreach (KeyValuePair<string, Parameter> kvp2 in relevantProbs)
                                    {
                                        if (kvp2.Value.Action == Parameter.ParameterAction.Equal && kvp2.Value.EqualParameter == kvp.Value)
                                        {
                                            piWeight++;
                                        }
                                    }

                                    kvp.Value.Value = kvp.Value.Value / (piWeight * otherPi) * remainingPi;
                                }
                            }
                        }
                    }
                }

                BuildWindow();
            };

            fixItem.Items = new List<MenuItem>() { fixValueItem, fixOkItem };

            MenuItem dirichletItem = new MenuItem() { Header = "Dirichlet", Icon = new PiMenuIcon() { IconType = PiMenuIcon.IconTypes.Dirichlet } };

            NumericUpDown dirichletValue = new NumericUpDown() { Minimum = 0, Increment = 0.1, Value = prob.DistributionParameter, Padding = new Thickness(5, 0, 5, 0), Width = 100, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            dirichletValue.PointerReleased += (s, e) =>
            {
                e.Handled = true;
            };


            dirichletValue.KeyDown += (s, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Return)
                {
                    e.Handled = true;
                }
            };

            MenuItem dirichletValueItem = new MenuItem() { Header = dirichletValue, Icon = new PiMenuIcon() { IconType = PiMenuIcon.IconTypes.Dirichlet } };

            dirichletValueItem.PointerReleased += (s, e) =>
            {
                e.Handled = true;
            };

            MenuItem dirichletOkItem = new MenuItem() { Header = "OK", Icon = new Viewbox() { Child = new Octagon() { Fill = new SolidColorBrush(Color.FromArgb(255, 34, 177, 76)), IsTick = true } } };
            dirichletOkItem.Click += (s, e) =>
            {
                prob.Action = Parameter.ParameterAction.Dirichlet;
                prob.DistributionParameter = dirichletValue.Value;

                BuildWindow();
            };

            dirichletItem.Items = new List<MenuItem>() { dirichletValueItem, dirichletOkItem };

            MenuItem equalItem = new MenuItem() { Header = "Equal", Icon = new PiMenuIcon() { IconType = PiMenuIcon.IconTypes.Equal } };

            List<MenuItem> equalItems = new List<MenuItem>();

            foreach (KeyValuePair<string, Parameter> kvp in charProbs)
            {
                if (kvp.Value != prob && kvp.Value.Action != Parameter.ParameterAction.Equal)
                {
                    MenuItem currEqualItem = new MenuItem() { Header = kvp.Key, Icon = new PiMenuIcon() { IconType = PiMenuIcon.IconTypes.Equal } };
                    currEqualItem.Click += (s, e) =>
                    {
                        prob.Action = Parameter.ParameterAction.Equal;
                        prob.EqualParameter = kvp.Value;
                        BuildWindow();
                    };

                    equalItems.Add(currEqualItem);


                }
            }

            equalItem.Items = equalItems;

            MenuItem MLItem = new MenuItem() { Header = "Maximum-Likelihood", Icon = new PiMenuIcon() { IconType = PiMenuIcon.IconTypes.ML } };

            MLItem.Click += (s, e) =>
            {
                prob.Action = Parameter.ParameterAction.ML;
                BuildWindow();
            };



            MenuItem multinomialItem = new MenuItem() { Header = "Multinomial", Icon = new PiMenuIcon() { IconType = PiMenuIcon.IconTypes.Multinomial } };

            NumericUpDown multinomialValue = new NumericUpDown() { Minimum = 0, Increment = 0.1, Value = prob.DistributionParameter, Padding = new Thickness(5, 0, 5, 0), Width = 100, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            multinomialValue.PointerReleased += (s, e) =>
            {
                e.Handled = true;
            };


            multinomialValue.KeyDown += (s, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Return)
                {
                    e.Handled = true;
                }
            };

            MenuItem multinomialValueItem = new MenuItem() { Header = multinomialValue, Icon = new PiMenuIcon() { IconType = PiMenuIcon.IconTypes.Multinomial } };

            multinomialValueItem.PointerReleased += (s, e) =>
            {
                e.Handled = true;
            };

            MenuItem multinomialOkItem = new MenuItem() { Header = "OK", Icon = new Viewbox() { Child = new Octagon() { Fill = new SolidColorBrush(Color.FromArgb(255, 34, 177, 76)), IsTick = true } } };
            multinomialOkItem.Click += (s, e) =>
            {
                prob.Action = Parameter.ParameterAction.Multinomial;
                prob.DistributionParameter = multinomialValue.Value;

                BuildWindow();
            };

            multinomialItem.Items = new List<MenuItem>() { multinomialValueItem, multinomialOkItem };

            if (equalItems.Count > 0)
            {
                ContextMenu menu = new ContextMenu() { Items = new List<MenuItem>() { fixItem, dirichletItem, multinomialItem, MLItem, equalItem } };
                menu.Open(btn);
            }
            else
            {
                ContextMenu menu = new ContextMenu() { Items = new List<MenuItem>() { fixItem, multinomialItem, MLItem, dirichletItem } };
                menu.Open(btn);
            }
        }


        public ViewPiWindow(DataMatrix data, CharacterDependency[][] dependencies, Dictionary<string, Parameter>[] pi, Dictionary<string, int>[] parameterIndices, double[][][] parameters, bool isCondProbs = false)
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            HeaderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 162, 232));

            IsCondProbs = isCondProbs;
            Dependencies = dependencies;
            Pi = pi;

            Grid mainContainer = this.FindControl<Grid>("PisContainer");

            for (int depInd = 0; depInd < dependencies.Length; depInd++)
            {
                mainContainer.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));

                {
                    TextBlock blk = new TextBlock() { Text = "Set " + depInd.ToString() + ":", FontWeight = FontWeight.Bold, FontSize = 20, Margin = new Thickness(0, 20, 0, 0) };
                    Grid.SetRow(blk, mainContainer.RowDefinitions.Count - 1);
                    Grid.SetColumnSpan(blk, 3);
                    mainContainer.Children.Add(blk);
                }

                for (int i = 0; i < dependencies[depInd].Length; i++)
                {
                    if (Dependencies[depInd][i].Type != CharacterDependency.Types.Conditioned && !IsCondProbs)
                    {
                        mainContainer.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));

                        {
                            TextBlock blk = new TextBlock() { Text = "Character" + (dependencies[depInd][i].InputDependencyName.Contains(",") ? "s" : "") + " " + dependencies[depInd][i].InputDependencyName + ":", FontWeight = FontWeight.Bold, FontSize = 18, Margin = new Thickness(20, 5, 0, 0) };
                            Grid.SetRow(blk, mainContainer.RowDefinitions.Count - 1);
                            Grid.SetColumnSpan(blk, 3);
                            mainContainer.Children.Add(blk);
                        }

                        double lastAngle = 0;

                        for (int j = 0; j < data.States[dependencies[depInd][i].Index].Length; j++)
                        {
                            mainContainer.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));

                            {
                                TextBlock blk = new TextBlock() { Text = data.States[dependencies[depInd][i].Index][j], Margin = new Thickness(40, 0, 10, 0), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                                Grid.SetRow(blk, mainContainer.RowDefinitions.Count - 1);
                                mainContainer.Children.Add(blk);
                            }

                            Parameter currPi = pi[dependencies[depInd][i].Index][data.States[dependencies[depInd][i].Index][j]];

                            if (currPi.Action == Parameter.ParameterAction.Equal)
                            {
                                currPi = currPi.EqualParameter;
                            }

                            if (currPi.Action == Parameter.ParameterAction.Fix)
                            {
                                Canvas can = new Canvas() { Width = 24, Height = 24, Margin = new Thickness(20, 5, 20, 5) };

                                can.Children.Add(new Ellipse() { Fill = new SolidColorBrush(Color.FromArgb(255, 230, 230, 230)), Width = 24, Height = 24 });

                                if (currPi.Value >= 0.01 && currPi.Value <= 0.99)
                                {
                                    PathFigure fig = new PathFigure() { StartPoint = new Point(12, 12) };
                                    fig.Segments.Add(new LineSegment() { Point = new Point(12 + 12 * Math.Cos(lastAngle * 2 * Math.PI), 12 + 12 * Math.Sin(lastAngle * 2 * Math.PI)) });
                                    fig.Segments.Add(new ArcSegment() { Point = new Point(12 + 12 * Math.Cos((lastAngle + currPi.Value) * 2 * Math.PI), 12 + 12 * Math.Sin((lastAngle + currPi.Value) * 2 * Math.PI)), Size = new Size(12, 12), SweepDirection = SweepDirection.Clockwise, IsLargeArc = currPi.Value > 0.5 });
                                    fig.IsClosed = true;

                                    PathGeometry geo = new PathGeometry();
                                    geo.Figures.Add(fig);

                                    Path pth = new Path() { Data = geo, Fill = Program.GetBrush(Plotting.GetColor(j, 1, data.States[dependencies[depInd][i].Index].Length)) };
                                    can.Children.Add(pth);

                                }
                                else if (currPi.Value > 0.99)
                                {
                                    can.Children.Add(new Ellipse() { Fill = Program.GetBrush(Plotting.GetColor(j, 1, data.States[dependencies[depInd][i].Index].Length)), Width = 24, Height = 24 });
                                }

                                can.Children.Add(new Ellipse() { Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 1.5, Width = 24, Height = 24 });

                                Grid.SetRow(can, mainContainer.RowDefinitions.Count - 1);
                                Grid.SetColumn(can, 1);

                                mainContainer.Children.Add(can);

                                {
                                    TextBlock blk = new TextBlock() { Text = currPi.Value.ToString("0.#%", System.Globalization.CultureInfo.InvariantCulture), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                                    Grid.SetRow(blk, mainContainer.RowDefinitions.Count - 1);
                                    Grid.SetColumn(blk, 2);
                                    mainContainer.Children.Add(blk);
                                }

                                lastAngle += currPi.Value;
                            }
                            else if (currPi.Action == Parameter.ParameterAction.Dirichlet)
                            {
                                int piInd = parameterIndices[depInd][dependencies[depInd][i].InputDependencyName + ":" + data.States[dependencies[depInd][i].Index][j]];

                                double[] samples = (from el in parameters select el[depInd][piInd]).ToArray();

                                Canvas can = new Canvas() { Width = 100, Height = 48, Margin = new Thickness(20, 5, 20, 5) };

                                double[] computedYs = new double[100];

                                for (int x = 0; x < 100; x++)
                                {
                                    double realX = x / 100.0;

                                    computedYs[x] = (from el in samples where el > realX && el <= realX + 0.01 select 1).Count();
                                }

                                PathFigure fig = new PathFigure();
                                PathFigure fillFig = new PathFigure() { StartPoint = new Point(0, 48) };

                                double maxY = computedYs.Max();

                                for (int x = 0; x < 100; x++)
                                {
                                    if (x == 0)
                                    {
                                        fig.StartPoint = new Point(x, 48 - 48 * computedYs[x] / maxY);
                                    }
                                    else
                                    {
                                        fig.Segments.Add(new LineSegment() { Point = new Point(x, 48 - 48 * computedYs[x] / maxY) });
                                    }

                                    fillFig.Segments.Add(new LineSegment() { Point = new Point(x, 48 - 48 * computedYs[x] / maxY) });
                                }

                                fillFig.Segments.Add(new LineSegment() { Point = new Point(99, 48) });

                                fig.IsClosed = false;
                                fillFig.IsClosed = true;

                                PathGeometry geo = new PathGeometry();
                                geo.Figures.Add(fig);

                                PathGeometry fillGeo = new PathGeometry();
                                fillGeo.Figures.Add(fillFig);

                                can.Children.Add(new Path() { Data = fillGeo, Fill = Program.GetBrush(Plotting.GetColor(j, 0.25, data.States[dependencies[depInd][i].Index].Length)) });
                                can.Children.Add(new Path() { Data = geo, Stroke = Program.GetBrush(Plotting.GetColor(j, 1, data.States[dependencies[depInd][i].Index].Length)), StrokeThickness = 2, StrokeJoin = PenLineJoin.Round });

                                Grid.SetRow(can, mainContainer.RowDefinitions.Count - 1);
                                Grid.SetColumn(can, 1);

                                mainContainer.Children.Add(can);

                                (double mean, double variance) meanAndVariance = samples.MeanAndVariance();

                                {
                                    TextBlock blk = new TextBlock() { Text = "μ = " + meanAndVariance.mean.ToString(3, false) + "    σ = " + Math.Sqrt(meanAndVariance.variance).ToString(3, false), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                                    Grid.SetRow(blk, mainContainer.RowDefinitions.Count - 1);
                                    Grid.SetColumn(blk, 2);
                                    mainContainer.Children.Add(blk);
                                }

                                lastAngle += currPi.Value;
                            }
                        }
                    }
                    else if (Dependencies[depInd][i].Type == CharacterDependency.Types.Conditioned && IsCondProbs)
                    {
                        mainContainer.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));

                        {
                            TextBlock blk = new TextBlock() { Text = "Character" + (Dependencies[depInd][i].InputDependencyName.Contains(",") ? "s" : "") + " " + Dependencies[depInd][i].InputDependencyName + ":", FontWeight = FontWeight.Bold, FontSize = 18, Margin = new Thickness(20, 5, 0, 0) };
                            Grid.SetRow(blk, mainContainer.RowDefinitions.Count - 1);
                            Grid.SetColumnSpan(blk, 4);
                            mainContainer.Children.Add(blk);
                        }

                        double lastAngle = 0;

                        int colInd = 0;

                        foreach (KeyValuePair<string, Parameter> param in Dependencies[depInd][i].ConditionedProbabilities)
                        {
                            mainContainer.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));

                            {
                                string name = param.Key.Substring(param.Key.IndexOf(">") + 1) + " | " + param.Key.Substring(0, param.Key.IndexOf(">"));

                                TextBlock blk = new TextBlock() { Text = name, Margin = new Thickness(40, 0, 10, 0), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                                Grid.SetRow(blk, mainContainer.RowDefinitions.Count - 1);
                                mainContainer.Children.Add(blk);
                            }

                            Parameter currPi = param.Value;

                            Dictionary<string, Parameter> currPis = Dependencies[depInd][i].ConditionedProbabilities;

                            if (currPi.Action == Parameter.ParameterAction.Equal)
                            {
                                currPi = currPi.EqualParameter;
                            }

                            if (currPi.Action == Parameter.ParameterAction.Fix)
                            {
                                Canvas can = new Canvas() { Width = 24, Height = 24, Margin = new Thickness(20, 5, 20, 5) };

                                can.Children.Add(new Ellipse() { Fill = new SolidColorBrush(Color.FromArgb(255, 230, 230, 230)), Width = 24, Height = 24 });

                                if (currPi.Value >= 0.01 && currPi.Value <= 0.99)
                                {
                                    PathFigure fig = new PathFigure() { StartPoint = new Point(12, 12) };
                                    fig.Segments.Add(new LineSegment() { Point = new Point(12 + 12 * Math.Cos(lastAngle * 2 * Math.PI), 12 + 12 * Math.Sin(lastAngle * 2 * Math.PI)) });
                                    fig.Segments.Add(new ArcSegment() { Point = new Point(12 + 12 * Math.Cos((lastAngle + currPi.Value) * 2 * Math.PI), 12 + 12 * Math.Sin((lastAngle + currPi.Value) * 2 * Math.PI)), Size = new Size(12, 12), SweepDirection = SweepDirection.Clockwise, IsLargeArc = currPi.Value > 0.5 });
                                    fig.IsClosed = true;

                                    PathGeometry geo = new PathGeometry();
                                    geo.Figures.Add(fig);

                                    Path pth = new Path() { Data = geo, Fill = Program.GetBrush(Plotting.GetColor(colInd, 1, Dependencies[depInd][i].ConditionedProbabilities.Count)) };
                                    can.Children.Add(pth);

                                }
                                else if (currPi.Value > 0.99)
                                {
                                    can.Children.Add(new Ellipse() { Fill = Program.GetBrush(Plotting.GetColor(colInd, 1, Dependencies[depInd][i].ConditionedProbabilities.Count)), Width = 24, Height = 24 });
                                }

                                can.Children.Add(new Ellipse() { Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 1.5, Width = 24, Height = 24 });

                                Grid.SetRow(can, mainContainer.RowDefinitions.Count - 1);
                                Grid.SetColumn(can, 1);

                                mainContainer.Children.Add(can);

                                {
                                    TextBlock blk = new TextBlock() { Text = currPi.Value.ToString("0.#%", System.Globalization.CultureInfo.InvariantCulture), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                                    Grid.SetRow(blk, mainContainer.RowDefinitions.Count - 1);
                                    Grid.SetColumn(blk, 2);
                                    mainContainer.Children.Add(blk);
                                }

                                lastAngle += currPi.Value;
                            }
                            else if (currPi.Action == Parameter.ParameterAction.Dirichlet)
                            {
                                string realKey = param.Key.Substring(param.Key.IndexOf(">") + 1) + "|" + param.Key.Substring(0, param.Key.IndexOf(">"));

                                int piInd = parameterIndices[depInd][dependencies[depInd][i].InputDependencyName + ":" + realKey];

                                double[] samples = (from el in parameters select el[depInd][piInd]).ToArray();

                                Canvas can = new Canvas() { Width = 100, Height = 48, Margin = new Thickness(20, 5, 20, 5) };

                                double[] computedYs = new double[100];

                                for (int x = 0; x < 100; x++)
                                {
                                    double realX = x / 100.0;

                                    computedYs[x] = (from el in samples where el > realX && el <= realX + 0.01 select 1).Count();
                                }

                                PathFigure fig = new PathFigure();
                                PathFigure fillFig = new PathFigure() { StartPoint = new Point(0, 48) };

                                double maxY = computedYs.Max();

                                for (int x = 0; x < 100; x++)
                                {
                                    if (x == 0)
                                    {
                                        fig.StartPoint = new Point(x, 48 - 48 * computedYs[x] / maxY);
                                    }
                                    else
                                    {
                                        fig.Segments.Add(new LineSegment() { Point = new Point(x, 48 - 48 * computedYs[x] / maxY) });
                                    }

                                    fillFig.Segments.Add(new LineSegment() { Point = new Point(x, 48 - 48 * computedYs[x] / maxY) });
                                }

                                fillFig.Segments.Add(new LineSegment() { Point = new Point(99, 48) });

                                fig.IsClosed = false;
                                fillFig.IsClosed = true;

                                PathGeometry geo = new PathGeometry();
                                geo.Figures.Add(fig);

                                PathGeometry fillGeo = new PathGeometry();
                                fillGeo.Figures.Add(fillFig);

                                can.Children.Add(new Path() { Data = fillGeo, Fill = Program.GetBrush(Plotting.GetColor(colInd, 0.25, Dependencies[depInd][i].ConditionedProbabilities.Count)) });
                                can.Children.Add(new Path() { Data = geo, Stroke = Program.GetBrush(Plotting.GetColor(colInd, 1, Dependencies[depInd][i].ConditionedProbabilities.Count)), StrokeThickness = 2, StrokeJoin = PenLineJoin.Round });

                                Grid.SetRow(can, mainContainer.RowDefinitions.Count - 1);
                                Grid.SetColumn(can, 1);

                                mainContainer.Children.Add(can);

                                (double mean, double variance) meanAndVariance = samples.MeanAndVariance();

                                {
                                    TextBlock blk = new TextBlock() { Text = "μ = " + meanAndVariance.mean.ToString(3, false) + "    σ = " + Math.Sqrt(meanAndVariance.variance).ToString(3, false), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                                    Grid.SetRow(blk, mainContainer.RowDefinitions.Count - 1);
                                    Grid.SetColumn(blk, 2);
                                    mainContainer.Children.Add(blk);
                                }

                                lastAngle += currPi.Value;
                            }

                            colInd++;
                        }
                    }
                }
            }


        }

        private async void ParseSourceClicked(object sender, RoutedEventArgs e)
        {
            if (!IsCondProbs)
            {
                Dictionary<string, Parameter>[] prevPis = Pi;
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
                            Pi = Parsing.ParsePiFile(sr, States, new ThreadSafeRandom());
                        }
                    }
                    BuildWindow();
                }
                catch (Exception ex)
                {
                    await new MessageBox("Error!", "Error: " + ex.Message).ShowDialog(this);
                    Pi = prevPis;
                    BuildWindow();
                }
            }
            else
            {
                CharacterDependency[][] prevDeps = Dependencies;

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
                            CharacterDependency[][] newDeps = Parsing.ParseDependencies(sr, States, new ThreadSafeRandom());

                            if (newDeps.Length != Dependencies.Length)
                            {
                                throw new Exception("It is not allowed to change dependencies at this stage!");
                            }

                            for (int i = 0; i < Dependencies.Length; i++)
                            {
                                if (newDeps[i].Length != Dependencies[i].Length)
                                {
                                    throw new Exception("It is not allowed to change dependencies at this stage!");
                                }

                                for (int j = 0; j < Dependencies[i].Length; j++)
                                {
                                    if (newDeps[i][j].Type != Dependencies[i][j].Type)
                                    {
                                        throw new Exception("It is not allowed to change dependencies at this stage!");
                                    }
                                }
                            }

                            for (int i = 0; i < Dependencies.Length; i++)
                            {
                                for (int j = 0; j < Dependencies[i].Length; j++)
                                {
                                    if (newDeps[i][j].Type == CharacterDependency.Types.Conditioned)
                                    {
                                        Dependencies[i][j].ConditionedProbabilities = newDeps[i][j].ConditionedProbabilities;
                                    }
                                }
                            }
                        }
                    }
                    BuildWindow();
                }
                catch (Exception ex)
                {
                    await new MessageBox("Error!", "Error: " + ex.Message).ShowDialog(this);
                    Dependencies = prevDeps;
                    BuildWindow();
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
