using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using Utils;

namespace sMap_GUI
{
    public class ViewDependenciesWindow : Window
    {
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
                this.FindControl<Path>("BgPath").Fill = value;
                this.FindControl<TextBlock>("HeaderBlock").Foreground = value;
            }
        }

        public ViewDependenciesWindow()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        public ViewDependenciesWindow(Utils.CharacterDependency[][] dependencies, string[][] states = null, bool isEditable = false)
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            this.Dependencies = dependencies;
            this.IsEditable = isEditable;
            this.States = states;

            BuildWindow();

            //Workaround Avalonia bug
            async void resize()
            {
                await System.Threading.Tasks.Task.Delay(100);
                this.Height = this.Height + 1;
            };

            resize();
        }

        public CharacterDependency[][] Dependencies;
        bool IsEditable;
        string[][] States;

        CharacterDependency draggingDependency;
        int draggingIndex = -1;

        List<Control> dependencyBorders;

        private void BuildWindow()
        {
            StackPanel mainContainer = this.FindControl<StackPanel>("DependenciesContainer");

            mainContainer.Children.Clear();
            dependencyBorders = new List<Control>();

            mainContainer.Children.Add(new TextBlock() { Text = "Legend:", FontWeight = FontWeight.Bold, FontSize = 20 });

            Grid legendGrid = new Grid();

            legendGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            legendGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(0, GridUnitType.Auto)));
            legendGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            legendGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(0, GridUnitType.Auto)));
            legendGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            legendGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(0, GridUnitType.Auto)));
            legendGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));

            {
                StackPanel pnl = new StackPanel() { Orientation = Avalonia.Layout.Orientation.Horizontal };

                pnl.Children.Add(new TextBlock() { Text = "Independent: ", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });

                Border bord = new Border() { Margin = new Thickness(10, 0, 20, 0), CornerRadius = new CornerRadius(10), Width = 35, Height = 35, Background = Program.GetBrush(Plotting.GetColor(0, 0.5, 6)), BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new Thickness(1.5), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                bord.Child = new TextBlock() { Text = "0", FontWeight = Avalonia.Media.FontWeight.Bold, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, FontSize = 18 };
                pnl.Children.Add(bord);

                Grid.SetColumn(pnl, 1);

                legendGrid.Children.Add(pnl);
            }

            {
                StackPanel pnl = new StackPanel() { Orientation = Avalonia.Layout.Orientation.Horizontal };

                pnl.Children.Add(new TextBlock() { Text = "Dependent: ", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });



                double circRadius = 70.0 / (2 * Math.Sin(Math.PI / 3));

                Border bord = new Border() { CornerRadius = new CornerRadius(10), Background = Program.GetBrush(Plotting.GetColor(1, 0.15, 6)), BorderBrush = new SolidColorBrush(Colors.Black), Width = 2 * circRadius + 35 + 20, Height = 2 * circRadius + 35 + 20, BorderThickness = new Thickness(1.5) };

                Canvas currCanvas = new Canvas() { Width = 2 * circRadius + 35 + 20, Height = 2 * circRadius + 35 + 20 };

                bord.Child = currCanvas;

                PathGeometry geo = new PathGeometry();
                PathFigure fig = new PathFigure() { StartPoint = new Point(10 + 17.5, 10 + 17.5 + circRadius) };

                double boundLeft = (10 + 17.5 + circRadius);
                double boundRight = (10 + 17.5 + circRadius);

                double boundTop = (10 + 17.5 + circRadius);
                double boundBottom = (10 + 17.5 + circRadius);

                for (int j = 1; j <= 3; j++)
                {
                    fig.Segments.Add(new LineSegment() { Point = new Point(10 + 17.5 + circRadius - circRadius * Math.Cos(2 * Math.PI / 3 * j), 10 + 17.5 + circRadius - circRadius * Math.Sin(2 * Math.PI / 3 * j)) });

                    boundLeft = Math.Min(boundLeft, 10 + circRadius - circRadius * Math.Cos(2 * Math.PI / 3 * j));
                    boundRight = Math.Max(boundRight, 35 + 10 + circRadius - circRadius * Math.Cos(2 * Math.PI / 3 * j));

                    boundTop = Math.Min(boundTop, 10 + circRadius - circRadius * Math.Sin(2 * Math.PI / 3 * j));
                    boundBottom = Math.Max(boundBottom, 35 + 10 + circRadius - circRadius * Math.Sin(2 * Math.PI / 3 * j));
                }

                double meanX = (boundLeft + boundRight) * 0.5;
                double meanY = (boundTop + boundBottom) * 0.5;

                currCanvas.Margin = new Thickness(2 * ((10 + 17.5 + circRadius) - meanX), 2 * ((10 + 17.5 + circRadius) - meanY), 0, 0);

                geo.Figures.Add(fig);

                Path pth = new Path() { Data = geo, Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 1.5 };
                currCanvas.Children.Add(pth);

                for (int j = 0; j < 3; j++)
                {
                    currCanvas.Children.Add(new Ellipse()
                    {
                        Width = 35,
                        Height = 35,
                        Fill = Program.GetBrush(Plotting.GetColor(j + 2, 0.5, 6)),
                        Stroke = new SolidColorBrush(Colors.Black),
                        StrokeThickness = 1.5,
                        Margin = new Thickness(10 + circRadius - circRadius * Math.Cos(2 * Math.PI / 3 * j), 10 + circRadius - circRadius * Math.Sin(2 * Math.PI / 3 * j), 0, 0)
                    });

                    Grid grd = new Grid()
                    {
                        Width = 35,
                        Height = 35,
                        Margin = new Thickness(10 + circRadius - circRadius * Math.Cos(2 * Math.PI / 3 * j), 10 + circRadius - circRadius * Math.Sin(2 * Math.PI / 3 * j), 0, 0)
                    };

                    grd.Children.Add(new TextBlock()
                    {
                        Text = j.ToString(),
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                        FontSize = 18,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    });

                    currCanvas.Children.Add(grd);
                }

                Viewbox box = new Viewbox()
                {
                    Width = bord.Width / 2,
                    Height = bord.Height / 2,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Child = bord,
                    Margin = new Thickness(10, 0, 20, 0)
                };

                pnl.Children.Add(box);



                Grid.SetColumn(pnl, 3);

                legendGrid.Children.Add(pnl);
            }


            {
                StackPanel pnl = new StackPanel() { Orientation = Avalonia.Layout.Orientation.Horizontal };

                pnl.Children.Add(new TextBlock() { Text = "Conditioned: ", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });

                Grid can = new Grid() { Margin = new Thickness(10, 0, 20, 0), Width = 35, Height = 35, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };


                PathGeometry hexGeo = new PathGeometry();
                PathFigure hexFig = new PathFigure() { StartPoint = new Point(35, 17.5) };

                for (int s = 1; s < 6; s++)
                {
                    hexFig.Segments.Add(new LineSegment() { Point = new Point(17.5 + 17.5 * Math.Cos(Math.PI / 3 * s), 17.5 + 17.5 * Math.Sin(Math.PI / 3 * s)) });
                }

                hexFig.IsClosed = true;
                hexGeo.Figures.Add(hexFig);

                can.Children.Add(new Path()
                {
                    Fill = Program.GetBrush(Plotting.GetColor(5, 0.5, 6)),
                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = 1.5,
                    Data = hexGeo
                });

                can.Children.Add(new TextBlock() { Text = "0", FontWeight = Avalonia.Media.FontWeight.Bold, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, FontSize = 18 });
                pnl.Children.Add(can);

                Grid.SetColumn(pnl, 5);

                legendGrid.Children.Add(pnl);
            }

            mainContainer.Children.Add(legendGrid);

            if (IsEditable)
            {
                this.FindControl<Grid>("MainGrid").ColumnDefinitions[1].Width = new GridLength(0.4, GridUnitType.Star);
                this.FindControl<Grid>("SourceGrid").IsVisible = true;
                this.FindControl<TextBox>("SourceBox").Text = Utils.Utils.GetDependencySource(Dependencies, false, false, false).Replace("\t", "    ");
            }

            for (int depInd = 0; depInd < Dependencies.Length; depInd++)
            {
                Border brd = new Border() { BorderBrush = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200)), BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(10), Padding = new Thickness(10), Margin = new Thickness(0, 10) };
                mainContainer.Children.Add(brd);
                StackPanel depContainer = new StackPanel();
                brd.Child = depContainer;

                depContainer.Children.Add(new TextBlock() { Text = "Set " + depInd.ToString() + ":", FontWeight = FontWeight.Bold, FontSize = 20 });

                Canvas depCanvas = new Canvas() { Width = 100, Height = 100, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };

                (double[], Border)[] centerAndBottomBox = new (double[], Border)[Dependencies[depInd].Length];

                double lastRight = 0;

                int totalColoursHere = (from el in Dependencies[depInd] select (el.Type == CharacterDependency.Types.Dependent ? el.Dependencies.Length : 1)).Sum() + (from el in Dependencies[depInd] where el.Type == CharacterDependency.Types.Dependent select 1).Sum();

                int colInd = 0;

                for (int i = 0; i < Dependencies[depInd].Length; i++)
                {
                    CharacterDependency dep = Dependencies[depInd][i];

                    if (Dependencies[depInd][i].Type == Utils.CharacterDependency.Types.Independent)
                    {

                        int currCol = colInd;
                        int totalCols = totalColoursHere;

                        (double[], Border) bord = getIndependentBorder(dep, lastRight, colInd, totalColoursHere);
                        colInd++;
                        centerAndBottomBox[i] = bord;
                        bord.Item2.Tag = dep;

                        dependencyBorders.Add(bord.Item2);

                        if (IsEditable)
                        {
                            Border tempBord = getIndependentBorder(dep, 0, currCol, totalCols).Item2;
                            tempBord.Opacity = 0.5;
                            tempBord.Margin = new Thickness(0, 0, 0, 0);

                            bord.Item2.PointerPressed += (s, e) =>
                            {
                                draggingDependency = dep;
                                this.FindControl<Canvas>("OverlayCanvas").Children.Clear();
                                this.FindControl<Canvas>("OverlayCanvas").Children.Add(tempBord);
                                Point pos = e.GetPosition(this.FindControl<Canvas>("OverlayCanvas"));
                                tempBord.Margin = new Thickness(pos.X - tempBord.Width / 2, pos.Y - tempBord.Height / 2, 0, 0);
                            };
                        }

                        depCanvas.Children.Add(bord.Item2);

                        lastRight = bord.Item1[0] + bord.Item2.Width / 2;
                    }
                    else if (Dependencies[depInd][i].Type == Utils.CharacterDependency.Types.Dependent)
                    {
                        double circRadius = 70.0 / (2 * Math.Sin(Math.PI / Dependencies[depInd][i].Dependencies.Length));

                        Border bord = new Border() { CornerRadius = new CornerRadius(10), Background = Program.GetBrush(Plotting.GetColor(colInd, 0.15, totalColoursHere)), BorderBrush = new SolidColorBrush(Colors.Black), Width = 2 * circRadius + 35 + 20, Height = 2 * circRadius + 35 + 20, BorderThickness = new Thickness(1.5), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };

                        colInd++;

                        Canvas currCanvas = new Canvas() { Width = 2 * circRadius + 35 + 20, Height = 2 * circRadius + 35 + 20 };

                        bord.Child = currCanvas;

                        double x = lastRight + 10 + (2 * circRadius + 35 + 20) / 2;
                        double y = (2 * circRadius + 35 + 20) / 2;
                        double bottom = 2 * circRadius + 35 + 20;

                        centerAndBottomBox[i] = (new double[] { x, y, bottom }, bord);

                        PathGeometry geo = new PathGeometry();
                        PathFigure fig = new PathFigure() { StartPoint = new Point(10 + 17.5, 10 + 17.5 + circRadius) };

                        double boundLeft = (10 + 17.5 + circRadius);
                        double boundRight = (10 + 17.5 + circRadius);

                        double boundTop = (10 + 17.5 + circRadius);
                        double boundBottom = (10 + 17.5 + circRadius);

                        for (int j = 1; j <= Dependencies[depInd][i].Dependencies.Length; j++)
                        {
                            if (j != 2 || Dependencies[depInd][i].Dependencies.Length != 2)
                            {
                                fig.Segments.Add(new LineSegment() { Point = new Point(10 + 17.5 + circRadius - circRadius * Math.Cos(2 * Math.PI / Dependencies[depInd][i].Dependencies.Length * j), 10 + 17.5 + circRadius - circRadius * Math.Sin(2 * Math.PI / Dependencies[depInd][i].Dependencies.Length * j)) });
                            }

                            boundLeft = Math.Min(boundLeft, 10 + circRadius - circRadius * Math.Cos(2 * Math.PI / Dependencies[depInd][i].Dependencies.Length * j));
                            boundRight = Math.Max(boundRight, 35 + 10 + circRadius - circRadius * Math.Cos(2 * Math.PI / Dependencies[depInd][i].Dependencies.Length * j));

                            boundTop = Math.Min(boundTop, 10 + circRadius - circRadius * Math.Sin(2 * Math.PI / Dependencies[depInd][i].Dependencies.Length * j));
                            boundBottom = Math.Max(boundBottom, 35 + 10 + circRadius - circRadius * Math.Sin(2 * Math.PI / Dependencies[depInd][i].Dependencies.Length * j));
                        }

                        double meanX = (boundLeft + boundRight) * 0.5;
                        double meanY = (boundTop + boundBottom) * 0.5;


                        currCanvas.Margin = new Thickness(2 * ((10 + 17.5 + circRadius) - meanX), 2 * ((10 + 17.5 + circRadius) - meanY), 0, 0);

                        geo.Figures.Add(fig);

                        Path pth = new Path() { Data = geo, Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 1.5 };
                        currCanvas.Children.Add(pth);

                        for (int j = 0; j < Dependencies[depInd][i].Dependencies.Length; j++)
                        {
                            Ellipse ell = new Ellipse()
                            {
                                Width = 35,
                                Height = 35,
                                Fill = Program.GetBrush(Plotting.GetColor(colInd, 0.5, totalColoursHere)),
                                Stroke = new SolidColorBrush(Colors.Black),
                                StrokeThickness = 1.5,
                                Margin = new Thickness(10 + circRadius - circRadius * Math.Cos(2 * Math.PI / Dependencies[depInd][i].Dependencies.Length * j), 10 + circRadius - circRadius * Math.Sin(2 * Math.PI / Dependencies[depInd][i].Dependencies.Length * j), 0, 0)
                            };

                            currCanvas.Children.Add(ell);

                            Grid grd = new Grid()
                            {
                                Width = 35,
                                Height = 35,
                                Margin = new Thickness(10 + circRadius - circRadius * Math.Cos(2 * Math.PI / Dependencies[depInd][i].Dependencies.Length * j), 10 + circRadius - circRadius * Math.Sin(2 * Math.PI / Dependencies[depInd][i].Dependencies.Length * j), 0, 0),
                                IsHitTestVisible = false
                            };

                            grd.Children.Add(new TextBlock()
                            {
                                Text = Dependencies[depInd][i].Dependencies[j].ToString(),
                                FontWeight = Avalonia.Media.FontWeight.Bold,
                                FontSize = 18,
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                            });

                            currCanvas.Children.Add(grd);

                            if (IsEditable)
                            {
                                Canvas container = new Canvas() { Width = 35, Height = 35 };

                                container.Children.Add(new Ellipse()
                                {
                                    Width = 35,
                                    Height = 35,
                                    Fill = Program.GetBrush(Plotting.GetColor(colInd, 0.5, totalColoursHere)),
                                    Stroke = new SolidColorBrush(Colors.Black),
                                    StrokeThickness = 1.5
                                });

                                Grid grd2 = new Grid()
                                {
                                    Width = 35,
                                    Height = 35
                                };

                                grd2.Children.Add(new TextBlock()
                                {
                                    Text = Dependencies[depInd][i].Dependencies[j].ToString(),
                                    FontWeight = Avalonia.Media.FontWeight.Bold,
                                    FontSize = 18,
                                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                                });

                                container.Children.Add(grd2);

                                container.Opacity = 0.5;
                                container.Margin = new Thickness(0, 0, 0, 0);

                                int k = j;

                                ell.PointerPressed += (s, e) =>
                                {
                                    draggingDependency = dep;
                                    draggingIndex = k;
                                    this.FindControl<Canvas>("OverlayCanvas").Children.Clear();
                                    this.FindControl<Canvas>("OverlayCanvas").Children.Add(container);
                                    Point pos = e.GetPosition(this.FindControl<Canvas>("OverlayCanvas"));
                                    container.Margin = new Thickness(pos.X - container.Width / 2, pos.Y - container.Height / 2, 0, 0);
                                };
                            }

                            colInd++;
                        }



                        bord.Tag = dep;

                        dependencyBorders.Add(bord);

                        depCanvas.Children.Add(bord);

                        bord.Margin = new Thickness(lastRight + 10, 0, 0, 0);

                        lastRight = x + (2 * circRadius + 35 + 20) / 2;
                    }
                }

                double overallCenter = (from el in centerAndBottomBox where el.Item1 != null select el.Item1[1]).Max();

                for (int i = 0; i < Dependencies[depInd].Length; i++)
                {
                    if (Dependencies[depInd][i].Type == Utils.CharacterDependency.Types.Independent || Dependencies[depInd][i].Type == Utils.CharacterDependency.Types.Dependent)
                    {
                        double delta = overallCenter - centerAndBottomBox[i].Item1[1];

                        centerAndBottomBox[i].Item1[1] += delta;
                        centerAndBottomBox[i].Item1[2] += delta;

                        centerAndBottomBox[i].Item2.Margin = new Thickness(centerAndBottomBox[i].Item2.Margin.Left, centerAndBottomBox[i].Item2.Margin.Top + delta, 0, 0);
                    }
                }

                List<(CharacterDependency, double[])> depCoords = new List<(CharacterDependency, double[])>();

                for (int i = 0; i < Dependencies[depInd].Length; i++)
                {
                    if (Dependencies[depInd][i].Type == Utils.CharacterDependency.Types.Conditioned)
                    {
                        int[] realDeps = GetRealDependencies(Dependencies[depInd][i].Dependencies, Dependencies[depInd]);

                        double[] xs = (from el in realDeps select centerAndBottomBox[el].Item1[0]).ToArray();

                        double x = xs.Average();

                        double allCondBottom = (from el in realDeps select centerAndBottomBox[el].Item1[2]).Max();

                        double condBottom = (from el in realDeps select centerAndBottomBox[el].Item1[2]).Max();

                        double y = allCondBottom + 75 + 17.5;

                        double bottom = y + 17.5;

                        for (int j = 0; j < realDeps.Length; j++)
                        {
                            double delta = condBottom - centerAndBottomBox[realDeps[j]].Item1[2];

                            if (centerAndBottomBox[realDeps[j]].Item2 != null)
                            {
                                centerAndBottomBox[realDeps[j]].Item1[1] += delta;
                                centerAndBottomBox[realDeps[j]].Item1[2] += delta;
                                centerAndBottomBox[realDeps[j]].Item2.Margin = new Thickness(centerAndBottomBox[realDeps[j]].Item2.Margin.Left, centerAndBottomBox[realDeps[j]].Item2.Margin.Top + delta, 0, 0);
                            }
                        }

                        double[] coords = new double[] { x, y, bottom };

                        centerAndBottomBox[i] = (coords, null);

                        depCoords.Add((Dependencies[depInd][i], coords));
                    }
                }

                depCoords.Sort((a, b) => { return Math.Sign(a.Item2[0] - b.Item2[0]); });

                for (int i = 1; i < depCoords.Count; i++)
                {
                    depCoords[i].Item2[0] = Math.Max(depCoords[i].Item2[0], depCoords[i - 1].Item2[0] + 45);

                    lastRight = Math.Max(lastRight, depCoords[i].Item2[0] + 17.5);
                }

                for (int i = 0; i < depCoords.Count; i++)
                {
                    int[] realDeps = GetRealDependencies(depCoords[i].Item1.Dependencies, Dependencies[depInd]);

                    Path pth = new Path()
                    {
                        StrokeThickness = 1.5,
                        Stroke = new SolidColorBrush(Colors.Black),
                        ZIndex = -1
                    };

                    PathGeometry geo = new PathGeometry();

                    for (int j = 0; j < realDeps.Length; j++)
                    {
                        PathFigure fig = new PathFigure() { StartPoint = new Point(centerAndBottomBox[realDeps[j]].Item1[0], centerAndBottomBox[realDeps[j]].Item1[2] - 3) };
                        fig.Segments.Add(new LineSegment() { Point = new Point(depCoords[i].Item2[0], depCoords[i].Item2[1]) });
                        geo.Figures.Add(fig);
                    }

                    pth.Data = geo;



                    Grid grd = getConditionedGrid(depCoords[i], colInd, totalColoursHere);


                    depCanvas.Children.Add(pth);

                    grd.Tag = depCoords[i].Item1;

                    dependencyBorders.Add(grd);

                    depCanvas.Children.Add(grd);

                    CharacterDependency dep = depCoords[i].Item1;

                    if (IsEditable)
                    {
                        Grid tempGrid = getConditionedGrid(depCoords[i], colInd, totalColoursHere);
                        tempGrid.Opacity = 0.5;
                        tempGrid.Margin = new Thickness(0, 0, 0, 0);

                        grd.PointerPressed += (s, e) =>
                        {
                            draggingDependency = dep;
                            this.FindControl<Canvas>("OverlayCanvas").Children.Clear();
                            this.FindControl<Canvas>("OverlayCanvas").Children.Add(tempGrid);
                            Point pos = e.GetPosition(this.FindControl<Canvas>("OverlayCanvas"));
                            tempGrid.Margin = new Thickness(pos.X - tempGrid.Width / 2, pos.Y - tempGrid.Height / 2, 0, 0);
                        };
                    }

                    colInd++;
                }

                double height = (from el in centerAndBottomBox select el.Item1[2]).Max() + 10;
                double width = lastRight + 10;

                depCanvas.Width = width;
                depCanvas.Height = height;

                depContainer.Children.Add(depCanvas);
            }
        }

        private async void ParseButtonClicked(object sender, RoutedEventArgs e)
        {
            CharacterDependency[][] prevDep = Dependencies;
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
                        Dependencies = Parsing.ParseDependencies(sr, States, new ThreadSafeRandom());
                    }
                }
                BuildWindow();
            }
            catch (Exception ex)
            {
                await new MessageBox("Error!", "Error: " + ex.Message).ShowDialog(this);
                Dependencies = prevDep;
                BuildWindow();
            }
        }

        int[] GetRealDependencies(int[] dependencies, CharacterDependency[] allDependencies)
        {
            List<int> tbr = new List<int>();

            for (int i = 0; i < allDependencies.Length; i++)
            {
                if (allDependencies[i].Type == CharacterDependency.Types.Independent || allDependencies[i].Type == CharacterDependency.Types.Conditioned)
                {
                    if (dependencies.Contains(allDependencies[i].Index) && !tbr.Contains(i))
                    {
                        tbr.Add(i);
                    }
                }
                else if (allDependencies[i].Type == CharacterDependency.Types.Dependent)
                {
                    for (int j = 0; j < allDependencies[i].Dependencies.Length; j++)
                    {
                        if (dependencies.Contains(allDependencies[i].Dependencies[j]) && !tbr.Contains(i))
                        {
                            tbr.Add(i);
                        }
                    }
                }
            }

            return tbr.ToArray();
        }

        private Grid getConditionedGrid((CharacterDependency, double[]) depCoords, int colInd, int totalColoursHere)
        {
            Grid grd = new Grid()
            {
                Width = 35,
                Height = 35,
                Margin = new Thickness(depCoords.Item2[0] - 17.5, depCoords.Item2[1] - 17.5, 0, 0)
            };

            {
                PathGeometry hexGeo = new PathGeometry();
                PathFigure hexFig = new PathFigure() { StartPoint = new Point(35, 17.5) };

                for (int s = 1; s < 6; s++)
                {
                    hexFig.Segments.Add(new LineSegment() { Point = new Point(17.5 + 17.5 * Math.Cos(Math.PI / 3 * s), 17.5 + 17.5 * Math.Sin(Math.PI / 3 * s)) });
                }

                hexFig.IsClosed = true;
                hexGeo.Figures.Add(hexFig);

                grd.Children.Add(new Path()
                {
                    Fill = Program.GetBrush(Plotting.GetColor(colInd, 0.5, totalColoursHere)),
                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = 1.5,
                    Data = hexGeo
                });
            }

            grd.Children.Add(new TextBlock()
            {
                Text = depCoords.Item1.Index.ToString(),
                FontWeight = Avalonia.Media.FontWeight.Bold,
                FontSize = 18,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            });

            return grd;
        }

        private (double[], Border) getIndependentBorder(CharacterDependency dep, double lastRight, int colInd, int totalColoursHere)
        {
            FormattedText txt = new FormattedText() { Text = dep.Index.ToString(), Typeface = new Typeface(this.FontFamily, 18, FontStyle.Normal, FontWeight.Bold) };

            double x = lastRight + 10 + (txt.Bounds.Width + 30) / 2;
            double y = (txt.Bounds.Height + 20) / 2;
            double bottom = txt.Bounds.Height + 20;

            Border bord = new Border() { CornerRadius = new CornerRadius(10), Width = txt.Bounds.Width + 30, Height = txt.Bounds.Height + 20, Background = Program.GetBrush(Plotting.GetColor(colInd, 0.5, totalColoursHere)), BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new Thickness(1.5), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
            bord.Margin = new Thickness(x - (txt.Bounds.Width + 30) / 2, 0, 0, 0);

            bord.Child = new TextBlock() { Text = dep.Index.ToString(), FontWeight = Avalonia.Media.FontWeight.Bold, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, FontSize = 18 }; ;

            return (new double[] { x, y, bottom }, bord);
        }

        private void MouseUp(object sender, PointerReleasedEventArgs e)
        {
            if (IsEditable && draggingDependency != null)
            {
                CharacterDependency targetDependency = null;

                for (int i = 0; i < dependencyBorders.Count; i++)
                {
                    Point pos = e.GetPosition(dependencyBorders[i]);
                    if (pos.X >= 0 && pos.Y >= 0 && pos.X <= dependencyBorders[i].Width && pos.Y <= dependencyBorders[i].Height)
                    {
                        targetDependency = (CharacterDependency)dependencyBorders[i].Tag;
                    }
                }

                bool done = false;

                if (targetDependency != null && targetDependency != draggingDependency)
                {
                    CharacterDependency originalDependency = draggingDependency;

                    CharacterDependency[][] prevDeps = Dependencies;

                    if (draggingDependency.Type == CharacterDependency.Types.Independent)
                    {
                        if (targetDependency.Type == CharacterDependency.Types.Independent)
                        {
                            MenuItem makeDep = new MenuItem() { Header = "Make " + draggingDependency.Index.ToString() + " and " + targetDependency.Index.ToString() + " dependent" };
                            MenuItem cond = new MenuItem() { Header = "Condition " + draggingDependency.Index.ToString() + " on " + targetDependency.Index.ToString() };

                            makeDep.Click += async (s, ev) =>
                            {
                                if (!done)
                                {
                                    List<List<CharacterDependency>> listedDependencies = (from el in Dependencies select (from el2 in el select el2).ToList()).ToList();

                                    int finalSet = listedDependencies.FindIndex(a => a.Contains(targetDependency));

                                    int prevIndex = listedDependencies[finalSet].FindIndex(a => a == targetDependency);

                                    if (listedDependencies[finalSet].Contains(originalDependency) && listedDependencies[finalSet].FindIndex(a => a == originalDependency) < prevIndex)
                                    {
                                        prevIndex--;
                                    }

                                    listedDependencies[finalSet].Remove(targetDependency);

                                    listedDependencies.Find(a => a.Contains(originalDependency)).Remove(originalDependency);

                                    CharacterDependency newDep = new CharacterDependency(targetDependency.Index, CharacterDependency.Types.Dependent, new int[] { targetDependency.Index, originalDependency.Index }, new Dictionary<string, Parameter>());

                                    listedDependencies[finalSet].Insert(Math.Min(prevIndex, listedDependencies[finalSet].Count), newDep);

                                    List<List<CharacterDependency>> toBeRemoved = new List<List<CharacterDependency>>();
                                    for (int i = 0; i < listedDependencies.Count; i++)
                                    {
                                        if (listedDependencies[i].Count == 0)
                                        {
                                            toBeRemoved.Add(listedDependencies[i]);
                                        }
                                    }

                                    for (int i = 0; i < toBeRemoved.Count; i++)
                                    {
                                        listedDependencies.Remove(toBeRemoved[i]);
                                    }

                                    Dependencies = (from el in listedDependencies select el.ToArray()).ToArray();

                                    try
                                    {
                                        BuildWindow();
                                    }
                                    catch (Exception ex)
                                    {
                                        await new MessageBox("Error!", "Error: " + ex.Message).ShowDialog(this);
                                        Dependencies = prevDeps;
                                        BuildWindow();
                                    }
                                    done = true;
                                }
                            };

                            cond.Click += async (s, ev) =>
                            {
                                if (!done)
                                {
                                    List<List<CharacterDependency>> listedDependencies = (from el in Dependencies select (from el2 in el select el2).ToList()).ToList();

                                    int finalSet = listedDependencies.FindIndex(a => a.Contains(targetDependency));

                                    int prevIndex = listedDependencies[finalSet].FindIndex(a => a == targetDependency);

                                    listedDependencies.Find(a => a.Contains(originalDependency)).Remove(originalDependency);

                                    CharacterDependency newDep = new CharacterDependency(originalDependency.Index, CharacterDependency.Types.Conditioned, new int[] { targetDependency.Index }, new Dictionary<string, Parameter>());

                                    listedDependencies[finalSet].Insert(prevIndex + 1, newDep);

                                    List<List<CharacterDependency>> toBeRemoved = new List<List<CharacterDependency>>();
                                    for (int i = 0; i < listedDependencies.Count; i++)
                                    {
                                        if (listedDependencies[i].Count == 0)
                                        {
                                            toBeRemoved.Add(listedDependencies[i]);
                                        }
                                    }

                                    for (int i = 0; i < toBeRemoved.Count; i++)
                                    {
                                        listedDependencies.Remove(toBeRemoved[i]);
                                    }

                                    Dependencies = (from el in listedDependencies select el.ToArray()).ToArray();

                                    try
                                    {
                                        BuildWindow();
                                    }
                                    catch (Exception ex)
                                    {
                                        await new MessageBox("Error!", "Error: " + ex.Message).ShowDialog(this);
                                        Dependencies = prevDeps;
                                        BuildWindow();
                                    }
                                    done = true;
                                }
                            };



                            ContextMenu menu = new ContextMenu() { Items = new List<MenuItem>() { makeDep, cond } };



                            menu.Open(this.FindControl<Canvas>("OverlayCanvas"));
                        }
                        else if (targetDependency.Type == CharacterDependency.Types.Dependent)
                        {
                            string involvedChars = "";
                            for (int i = 0; i < targetDependency.Dependencies.Length; i++)
                            {
                                involvedChars += targetDependency.Dependencies[i].ToString() + (i < targetDependency.Dependencies.Length - 2 ? ", " : i < targetDependency.Dependencies.Length - 1 ? " and " : "");
                            }

                            MenuItem makeDep = new MenuItem() { Header = "Make " + draggingDependency.Index.ToString() + ", " + involvedChars + " dependent" };
                            MenuItem cond = new MenuItem() { Header = "Condition " + draggingDependency.Index.ToString() + " on " + involvedChars };

                            makeDep.Click += async (s, ev) =>
                            {
                                if (!done)
                                {
                                    List<List<CharacterDependency>> listedDependencies = (from el in Dependencies select (from el2 in el select el2).ToList()).ToList();

                                    int finalSet = listedDependencies.FindIndex(a => a.Contains(targetDependency));

                                    int prevIndex = listedDependencies[finalSet].FindIndex(a => a == targetDependency);

                                    if (listedDependencies[finalSet].Contains(originalDependency) && listedDependencies[finalSet].FindIndex(a => a == originalDependency) < prevIndex)
                                    {
                                        prevIndex--;
                                    }

                                    listedDependencies[finalSet].Remove(targetDependency);

                                    listedDependencies.Find(a => a.Contains(originalDependency)).Remove(originalDependency);

                                    List<int> allDeps = new List<int>(targetDependency.Dependencies);
                                    allDeps.Add(originalDependency.Index);

                                    CharacterDependency newDep = new CharacterDependency(targetDependency.Index, CharacterDependency.Types.Dependent, allDeps.ToArray(), new Dictionary<string, Parameter>());

                                    listedDependencies[finalSet].Insert(Math.Min(prevIndex, listedDependencies[finalSet].Count), newDep);

                                    List<List<CharacterDependency>> toBeRemoved = new List<List<CharacterDependency>>();
                                    for (int i = 0; i < listedDependencies.Count; i++)
                                    {
                                        if (listedDependencies[i].Count == 0)
                                        {
                                            toBeRemoved.Add(listedDependencies[i]);
                                        }
                                    }

                                    for (int i = 0; i < toBeRemoved.Count; i++)
                                    {
                                        listedDependencies.Remove(toBeRemoved[i]);
                                    }

                                    Dependencies = (from el in listedDependencies select el.ToArray()).ToArray();

                                    try
                                    {
                                        BuildWindow();
                                    }
                                    catch (Exception ex)
                                    {
                                        await new MessageBox("Error!", "Error: " + ex.Message).ShowDialog(this);
                                        Dependencies = prevDeps;
                                        BuildWindow();
                                    }
                                    done = true;
                                }
                            };

                            cond.Click += async (s, ev) =>
                            {
                                if (!done)
                                {
                                    List<List<CharacterDependency>> listedDependencies = (from el in Dependencies select (from el2 in el select el2).ToList()).ToList();

                                    int finalSet = listedDependencies.FindIndex(a => a.Contains(targetDependency));

                                    int prevIndex = listedDependencies[finalSet].FindIndex(a => a == targetDependency);

                                    listedDependencies.Find(a => a.Contains(originalDependency)).Remove(originalDependency);

                                    CharacterDependency newDep = new CharacterDependency(originalDependency.Index, CharacterDependency.Types.Conditioned, targetDependency.Dependencies, new Dictionary<string, Parameter>());

                                    listedDependencies[finalSet].Insert(Math.Min(prevIndex + 1, listedDependencies[finalSet].Count), newDep);

                                    List<List<CharacterDependency>> toBeRemoved = new List<List<CharacterDependency>>();
                                    for (int i = 0; i < listedDependencies.Count; i++)
                                    {
                                        if (listedDependencies[i].Count == 0)
                                        {
                                            toBeRemoved.Add(listedDependencies[i]);
                                        }
                                    }

                                    for (int i = 0; i < toBeRemoved.Count; i++)
                                    {
                                        listedDependencies.Remove(toBeRemoved[i]);
                                    }

                                    Dependencies = (from el in listedDependencies select el.ToArray()).ToArray();

                                    try
                                    {
                                        BuildWindow();
                                    }
                                    catch (Exception ex)
                                    {
                                        await new MessageBox("Error!", "Error: " + ex.Message).ShowDialog(this);
                                        Dependencies = prevDeps;
                                        BuildWindow();
                                    }
                                    done = true;
                                }
                            };

                            ContextMenu menu = new ContextMenu() { Items = new List<MenuItem>() { makeDep, cond } };

                            menu.Open(this.FindControl<Canvas>("OverlayCanvas"));
                        }
                        else if (targetDependency.Type == CharacterDependency.Types.Conditioned)
                        {
                            if (!targetDependency.Dependencies.Contains(draggingDependency.Index))
                            {
                                MenuItem cond = new MenuItem() { Header = "Condition " + draggingDependency.Index.ToString() + " on " + targetDependency.Index.ToString() };

                                cond.Click += async (s, ev) =>
                                {
                                    if (!done)
                                    {
                                        List<List<CharacterDependency>> listedDependencies = (from el in Dependencies select (from el2 in el select el2).ToList()).ToList();

                                        int finalSet = listedDependencies.FindIndex(a => a.Contains(targetDependency));

                                        int prevIndex = listedDependencies[finalSet].FindIndex(a => a == targetDependency);

                                        if (listedDependencies[finalSet].Contains(originalDependency) && listedDependencies[finalSet].FindIndex(a => a == originalDependency) < prevIndex)
                                        {
                                            prevIndex--;
                                        }

                                        listedDependencies.Find(a => a.Contains(originalDependency)).Remove(originalDependency);

                                        CharacterDependency newDep = new CharacterDependency(originalDependency.Index, CharacterDependency.Types.Conditioned, new int[] { targetDependency.Index }, new Dictionary<string, Parameter>());

                                        listedDependencies[finalSet].Insert(Math.Min(prevIndex + 1, listedDependencies[finalSet].Count), newDep);

                                        List<List<CharacterDependency>> toBeRemoved = new List<List<CharacterDependency>>();
                                        for (int i = 0; i < listedDependencies.Count; i++)
                                        {
                                            if (listedDependencies[i].Count == 0)
                                            {
                                                toBeRemoved.Add(listedDependencies[i]);
                                            }
                                        }

                                        for (int i = 0; i < toBeRemoved.Count; i++)
                                        {
                                            listedDependencies.Remove(toBeRemoved[i]);
                                        }

                                        Dependencies = (from el in listedDependencies select el.ToArray()).ToArray();

                                        try
                                        {
                                            BuildWindow();
                                        }
                                        catch (Exception ex)
                                        {
                                            await new MessageBox("Error!", "Error: " + ex.Message).ShowDialog(this);
                                            Dependencies = prevDeps;
                                            BuildWindow();
                                        }
                                        done = true;
                                    }
                                };

                                ContextMenu menu = new ContextMenu() { Items = new List<MenuItem>() { cond } };

                                menu.Open(this.FindControl<Canvas>("OverlayCanvas"));
                            }
                            else
                            {

                                MenuItem cond = new MenuItem() { Header = "Remove conditioning on " + draggingDependency.Index.ToString() };

                                cond.Click += async (s, ev) =>
                                {
                                    if (!done)
                                    {
                                        List<List<CharacterDependency>> listedDependencies = (from el in Dependencies select (from el2 in el select el2).ToList()).ToList();

                                        int finalSet = listedDependencies.FindIndex(a => a.Contains(targetDependency));

                                        int prevIndex = listedDependencies[finalSet].FindIndex(a => a == targetDependency);

                                        if (listedDependencies[finalSet].Contains(originalDependency) && listedDependencies[finalSet].FindIndex(a => a == originalDependency) < prevIndex)
                                        {
                                            prevIndex--;
                                        }

                                        listedDependencies[finalSet].Remove(targetDependency);

                                        List<int> newDeps = new List<int>(targetDependency.Dependencies);
                                        newDeps.Remove(originalDependency.Index);

                                        if (newDeps.Count > 0)
                                        {
                                            CharacterDependency newDep = new CharacterDependency(targetDependency.Index, CharacterDependency.Types.Conditioned, newDeps.ToArray(), new Dictionary<string, Parameter>());
                                            listedDependencies[finalSet].Insert(prevIndex, newDep);
                                        }
                                        else
                                        {
                                            CharacterDependency newDep = new CharacterDependency(targetDependency.Index);
                                            listedDependencies[finalSet].Insert(prevIndex, newDep);
                                        }

                                        List<List<CharacterDependency>> toBeRemoved = new List<List<CharacterDependency>>();
                                        for (int i = 0; i < listedDependencies.Count; i++)
                                        {
                                            if (listedDependencies[i].Count == 0)
                                            {
                                                toBeRemoved.Add(listedDependencies[i]);
                                            }
                                        }

                                        for (int i = 0; i < toBeRemoved.Count; i++)
                                        {
                                            listedDependencies.Remove(toBeRemoved[i]);
                                        }

                                        Dependencies = (from el in listedDependencies select el.ToArray()).ToArray();

                                        try
                                        {
                                            BuildWindow();
                                        }
                                        catch (Exception ex)
                                        {
                                            await new MessageBox("Error!", "Error: " + ex.Message).ShowDialog(this);
                                            Dependencies = prevDeps;
                                            BuildWindow();
                                        }
                                        done = true;
                                    }
                                };

                                ContextMenu menu = new ContextMenu() { Items = new List<MenuItem>() { cond } };

                                menu.Open(this.FindControl<Canvas>("OverlayCanvas"));
                            }
                        }
                    }
                    else if (draggingDependency.Type == CharacterDependency.Types.Conditioned)
                    {
                        if (targetDependency.Type == CharacterDependency.Types.Independent)
                        {
                            MenuItem makeDep = new MenuItem() { Header = "Make " + draggingDependency.Index.ToString() + " and " + targetDependency.Index.ToString() + " dependent" };

                            MenuItem cond;

                            if (!draggingDependency.Dependencies.Contains(targetDependency.Index))
                            {
                                cond = new MenuItem() { Header = "Condition " + draggingDependency.Index.ToString() + " on " + targetDependency.Index.ToString() };
                            }
                            else
                            {
                                cond = new MenuItem() { Header = "Remove conditioning on " + targetDependency.Index.ToString() };
                            }


                            makeDep.Click += async (s, ev) =>
                            {
                                if (!done)
                                {

                                    List<List<CharacterDependency>> listedDependencies = (from el in Dependencies select (from el2 in el select el2).ToList()).ToList();

                                    int finalSet = listedDependencies.FindIndex(a => a.Contains(targetDependency));

                                    int prevIndex = listedDependencies[finalSet].FindIndex(a => a == targetDependency);

                                    if (listedDependencies[finalSet].Contains(originalDependency) && listedDependencies[finalSet].FindIndex(a => a == originalDependency) < prevIndex)
                                    {
                                        prevIndex--;
                                    }

                                    listedDependencies[finalSet].Remove(targetDependency);

                                    listedDependencies.Find(a => a.Contains(originalDependency)).Remove(originalDependency);

                                    CharacterDependency newDep = new CharacterDependency(targetDependency.Index, CharacterDependency.Types.Dependent, new int[] { targetDependency.Index, originalDependency.Index }, new Dictionary<string, Parameter>());

                                    listedDependencies[finalSet].Insert(Math.Min(prevIndex, listedDependencies[finalSet].Count), newDep);

                                    List<List<CharacterDependency>> toBeRemoved = new List<List<CharacterDependency>>();
                                    for (int i = 0; i < listedDependencies.Count; i++)
                                    {
                                        if (listedDependencies[i].Count == 0)
                                        {
                                            toBeRemoved.Add(listedDependencies[i]);
                                        }
                                    }

                                    for (int i = 0; i < toBeRemoved.Count; i++)
                                    {
                                        listedDependencies.Remove(toBeRemoved[i]);
                                    }

                                    Dependencies = (from el in listedDependencies select el.ToArray()).ToArray();

                                    try
                                    {
                                        BuildWindow();
                                    }
                                    catch (Exception ex)
                                    {
                                        await new MessageBox("Error!", "Error: " + ex.Message).ShowDialog(this);
                                        Dependencies = prevDeps;
                                        BuildWindow();
                                    }
                                    done = true;
                                }
                            };

                            cond.Click += async (s, ev) =>
                            {
                                if (!done)
                                {
                                    List<List<CharacterDependency>> listedDependencies = (from el in Dependencies select (from el2 in el select el2).ToList()).ToList();

                                    int originalSet = listedDependencies.FindIndex(a => a.Contains(originalDependency));

                                    int finalSet = listedDependencies.FindIndex(a => a.Contains(targetDependency));

                                    int prevIndex = listedDependencies[finalSet].FindIndex(a => a == targetDependency);

                                    listedDependencies.Find(a => a.Contains(originalDependency)).Remove(originalDependency);

                                    if (!originalDependency.Dependencies.Contains(targetDependency.Index))
                                    {
                                        if (originalSet != finalSet)
                                        {
                                            for (int i = 0; i < listedDependencies[originalSet].Count; i++)
                                            {
                                                listedDependencies[finalSet].Add(listedDependencies[originalSet][i]);
                                            }

                                            for (int i = listedDependencies[originalSet].Count - 1; i >= 0; i--)
                                            {
                                                listedDependencies[originalSet].RemoveAt(i);
                                            }
                                        }

                                        List<int> deps = new List<int>(originalDependency.Dependencies);
                                        deps.Add(targetDependency.Index);

                                        CharacterDependency newDep = new CharacterDependency(originalDependency.Index, CharacterDependency.Types.Conditioned, deps.ToArray(), new Dictionary<string, Parameter>());
                                        listedDependencies[finalSet].Insert(prevIndex + 1, newDep);
                                    }
                                    else
                                    {
                                        List<int> deps = new List<int>(originalDependency.Dependencies);
                                        deps.Remove(targetDependency.Index);

                                        if (deps.Count > 0)
                                        {
                                            CharacterDependency newDep = new CharacterDependency(originalDependency.Index, CharacterDependency.Types.Conditioned, deps.ToArray(), new Dictionary<string, Parameter>());
                                            listedDependencies[finalSet].Insert(prevIndex + 1, newDep);
                                        }
                                        else
                                        {
                                            CharacterDependency newDep = new CharacterDependency(originalDependency.Index);
                                            listedDependencies[finalSet].Insert(prevIndex + 1, newDep);
                                        }

                                    }

                                    List<List<CharacterDependency>> toBeRemoved = new List<List<CharacterDependency>>();
                                    for (int i = 0; i < listedDependencies.Count; i++)
                                    {
                                        if (listedDependencies[i].Count == 0)
                                        {
                                            toBeRemoved.Add(listedDependencies[i]);
                                        }
                                    }

                                    for (int i = 0; i < toBeRemoved.Count; i++)
                                    {
                                        listedDependencies.Remove(toBeRemoved[i]);
                                    }

                                    Dependencies = (from el in listedDependencies select el.ToArray()).ToArray();

                                    try
                                    {
                                        BuildWindow();
                                    }
                                    catch (Exception ex)
                                    {
                                        await new MessageBox("Error!", "Error: " + ex.Message).ShowDialog(this);
                                        Dependencies = prevDeps;
                                        BuildWindow();
                                    }
                                    done = true;
                                }
                            };



                            ContextMenu menu = new ContextMenu() { Items = new List<MenuItem>() { makeDep, cond } };

                            menu.Open(this.FindControl<Canvas>("OverlayCanvas"));
                        }
                        else if (targetDependency.Type == CharacterDependency.Types.Dependent)
                        {
                            string involvedChars = "";
                            for (int i = 0; i < targetDependency.Dependencies.Length; i++)
                            {
                                involvedChars += targetDependency.Dependencies[i].ToString() + (i < targetDependency.Dependencies.Length - 2 ? ", " : i < targetDependency.Dependencies.Length - 1 ? " and " : "");
                            }

                            MenuItem makeDep = new MenuItem() { Header = "Make " + draggingDependency.Index.ToString() + ", " + involvedChars + " dependent" };
                            MenuItem cond;

                            if (!draggingDependency.Dependencies.ContainsAny(targetDependency.Dependencies))
                            {
                                cond = new MenuItem() { Header = "Condition " + draggingDependency.Index.ToString() + " on " + involvedChars };
                            }
                            else
                            {
                                cond = new MenuItem() { Header = "Remove conditioning on " + involvedChars };
                            }


                            makeDep.Click += async (s, ev) =>
                            {
                                if (!done)
                                {
                                    List<List<CharacterDependency>> listedDependencies = (from el in Dependencies select (from el2 in el select el2).ToList()).ToList();

                                    int finalSet = listedDependencies.FindIndex(a => a.Contains(targetDependency));

                                    int prevIndex = listedDependencies[finalSet].FindIndex(a => a == targetDependency);

                                    if (listedDependencies[finalSet].Contains(originalDependency) && listedDependencies[finalSet].FindIndex(a => a == originalDependency) < prevIndex)
                                    {
                                        prevIndex--;
                                    }

                                    listedDependencies[finalSet].Remove(targetDependency);

                                    listedDependencies.Find(a => a.Contains(originalDependency)).Remove(originalDependency);

                                    List<int> allDeps = new List<int>(targetDependency.Dependencies);
                                    allDeps.Add(originalDependency.Index);

                                    CharacterDependency newDep = new CharacterDependency(targetDependency.Index, CharacterDependency.Types.Dependent, allDeps.ToArray(), new Dictionary<string, Parameter>());

                                    listedDependencies[finalSet].Insert(Math.Min(prevIndex, listedDependencies[finalSet].Count), newDep);

                                    List<List<CharacterDependency>> toBeRemoved = new List<List<CharacterDependency>>();
                                    for (int i = 0; i < listedDependencies.Count; i++)
                                    {
                                        if (listedDependencies[i].Count == 0)
                                        {
                                            toBeRemoved.Add(listedDependencies[i]);
                                        }
                                    }

                                    for (int i = 0; i < toBeRemoved.Count; i++)
                                    {
                                        listedDependencies.Remove(toBeRemoved[i]);
                                    }

                                    Dependencies = (from el in listedDependencies select el.ToArray()).ToArray();

                                    try
                                    {
                                        BuildWindow();
                                    }
                                    catch (Exception ex)
                                    {
                                        await new MessageBox("Error!", "Error: " + ex.Message).ShowDialog(this);
                                        Dependencies = prevDeps;
                                        BuildWindow();
                                    }
                                    done = true;
                                }
                            };

                            cond.Click += async (s, ev) =>
                            {
                                if (!done)
                                {
                                    List<List<CharacterDependency>> listedDependencies = (from el in Dependencies select (from el2 in el select el2).ToList()).ToList();

                                    int originalSet = listedDependencies.FindIndex(a => a.Contains(originalDependency));

                                    int finalSet = listedDependencies.FindIndex(a => a.Contains(targetDependency));

                                    int prevIndex = listedDependencies[finalSet].FindIndex(a => a == targetDependency);

                                    listedDependencies[originalSet].Remove(originalDependency);

                                    if (!originalDependency.Dependencies.ContainsAny(targetDependency.Dependencies))
                                    {
                                        if (originalSet != finalSet)
                                        {
                                            for (int i = 0; i < listedDependencies[originalSet].Count; i++)
                                            {
                                                listedDependencies[finalSet].Add(listedDependencies[originalSet][i]);
                                            }

                                            for (int i = listedDependencies[originalSet].Count - 1; i >= 0; i--)
                                            {
                                                listedDependencies[originalSet].RemoveAt(i);
                                            }
                                        }

                                        List<int> deps = new List<int>(originalDependency.Dependencies);
                                        deps.AddRange(targetDependency.Dependencies);

                                        CharacterDependency newDep = new CharacterDependency(originalDependency.Index, CharacterDependency.Types.Conditioned, deps.ToArray(), new Dictionary<string, Parameter>());

                                        listedDependencies[finalSet].Insert(Math.Min(prevIndex + 1, listedDependencies[finalSet].Count), newDep);
                                    }
                                    else
                                    {
                                        List<int> deps = new List<int>(originalDependency.Dependencies);
                                        deps.RemoveAll(a => targetDependency.Dependencies.Contains(a));

                                        if (deps.Count > 0)
                                        {
                                            CharacterDependency newDep = new CharacterDependency(originalDependency.Index, CharacterDependency.Types.Conditioned, deps.ToArray(), new Dictionary<string, Parameter>());
                                            listedDependencies[finalSet].Insert(Math.Min(prevIndex + 1, listedDependencies[finalSet].Count), newDep);
                                        }
                                        else
                                        {
                                            CharacterDependency newDep = new CharacterDependency(originalDependency.Index);
                                            listedDependencies[finalSet].Insert(Math.Min(prevIndex + 1, listedDependencies[finalSet].Count), newDep);
                                        }
                                    }

                                    List<List<CharacterDependency>> toBeRemoved = new List<List<CharacterDependency>>();
                                    for (int i = 0; i < listedDependencies.Count; i++)
                                    {
                                        if (listedDependencies[i].Count == 0)
                                        {
                                            toBeRemoved.Add(listedDependencies[i]);
                                        }
                                    }

                                    for (int i = 0; i < toBeRemoved.Count; i++)
                                    {
                                        listedDependencies.Remove(toBeRemoved[i]);
                                    }

                                    Dependencies = (from el in listedDependencies select el.ToArray()).ToArray();

                                    try
                                    {
                                        BuildWindow();
                                    }
                                    catch (Exception ex)
                                    {
                                        await new MessageBox("Error!", "Error: " + ex.Message).ShowDialog(this);
                                        Dependencies = prevDeps;
                                        BuildWindow();
                                    }
                                    done = true;
                                }
                            };

                            ContextMenu menu = new ContextMenu() { Items = new List<MenuItem>() { makeDep, cond } };

                            menu.Open(this.FindControl<Canvas>("OverlayCanvas"));
                        }
                        else if (targetDependency.Type == CharacterDependency.Types.Conditioned)
                        {
                            if (!draggingDependency.Dependencies.Contains(targetDependency.Index))
                            {
                                MenuItem cond = new MenuItem() { Header = "Condition " + draggingDependency.Index.ToString() + " on " + targetDependency.Index.ToString() };

                                cond.Click += async (s, ev) =>
                                {
                                    if (!done)
                                    {
                                        List<List<CharacterDependency>> listedDependencies = (from el in Dependencies select (from el2 in el select el2).ToList()).ToList();

                                        int originalSet = listedDependencies.FindIndex(a => a.Contains(originalDependency));

                                        int finalSet = listedDependencies.FindIndex(a => a.Contains(targetDependency));

                                        int prevIndex = listedDependencies[finalSet].FindIndex(a => a == targetDependency);

                                        if (listedDependencies[finalSet].Contains(originalDependency) && listedDependencies[finalSet].FindIndex(a => a == originalDependency) < prevIndex)
                                        {
                                            prevIndex--;
                                        }

                                        listedDependencies[originalSet].Remove(originalDependency);

                                        if (originalSet != finalSet)
                                        {
                                            for (int i = 0; i < listedDependencies[originalSet].Count; i++)
                                            {
                                                listedDependencies[finalSet].Add(listedDependencies[originalSet][i]);
                                            }

                                            for (int i = listedDependencies[originalSet].Count - 1; i >= 0; i--)
                                            {
                                                listedDependencies[originalSet].RemoveAt(i);
                                            }
                                        }


                                        List<int> newDeps = new List<int>(originalDependency.Dependencies);
                                        newDeps.Add(targetDependency.Index);

                                        CharacterDependency newDep = new CharacterDependency(originalDependency.Index, CharacterDependency.Types.Conditioned, newDeps.ToArray(), new Dictionary<string, Parameter>());

                                        listedDependencies[finalSet].Insert(Math.Min(prevIndex + 1, listedDependencies[finalSet].Count), newDep);

                                        List<List<CharacterDependency>> toBeRemoved = new List<List<CharacterDependency>>();
                                        for (int i = 0; i < listedDependencies.Count; i++)
                                        {
                                            if (listedDependencies[i].Count == 0)
                                            {
                                                toBeRemoved.Add(listedDependencies[i]);
                                            }
                                        }

                                        for (int i = 0; i < toBeRemoved.Count; i++)
                                        {
                                            listedDependencies.Remove(toBeRemoved[i]);
                                        }

                                        Dependencies = (from el in listedDependencies select el.ToArray()).ToArray();

                                        try
                                        {
                                            BuildWindow();
                                        }
                                        catch (Exception ex)
                                        {
                                            await new MessageBox("Error!", "Error: " + ex.Message).ShowDialog(this);
                                            Dependencies = prevDeps;
                                            BuildWindow();
                                        }
                                        done = true;
                                    }
                                };

                                ContextMenu menu = new ContextMenu() { Items = new List<MenuItem>() { cond } };

                                menu.Open(this.FindControl<Canvas>("OverlayCanvas"));
                            }
                            else
                            {
                                MenuItem cond = new MenuItem() { Header = "Remove conditioning on " + targetDependency.Index.ToString() };

                                cond.Click += async (s, ev) =>
                                {
                                    if (!done)
                                    {
                                        List<List<CharacterDependency>> listedDependencies = (from el in Dependencies select (from el2 in el select el2).ToList()).ToList();

                                        int finalSet = listedDependencies.FindIndex(a => a.Contains(originalDependency));

                                        int prevIndex = listedDependencies[finalSet].FindIndex(a => a == originalDependency);

                                        listedDependencies[finalSet].Remove(originalDependency);

                                        List<int> newDeps = new List<int>(originalDependency.Dependencies);
                                        newDeps.Remove(targetDependency.Index);

                                        if (newDeps.Count > 0)
                                        {
                                            CharacterDependency newDep = new CharacterDependency(originalDependency.Index, CharacterDependency.Types.Conditioned, newDeps.ToArray(), new Dictionary<string, Parameter>());
                                            listedDependencies[finalSet].Insert(prevIndex, newDep);
                                        }
                                        else
                                        {
                                            CharacterDependency newDep = new CharacterDependency(originalDependency.Index);
                                            listedDependencies[finalSet].Insert(prevIndex, newDep);
                                        }

                                        List<List<CharacterDependency>> toBeRemoved = new List<List<CharacterDependency>>();
                                        for (int i = 0; i < listedDependencies.Count; i++)
                                        {
                                            if (listedDependencies[i].Count == 0)
                                            {
                                                toBeRemoved.Add(listedDependencies[i]);
                                            }
                                        }

                                        for (int i = 0; i < toBeRemoved.Count; i++)
                                        {
                                            listedDependencies.Remove(toBeRemoved[i]);
                                        }

                                        Dependencies = (from el in listedDependencies select el.ToArray()).ToArray();

                                        try
                                        {
                                            BuildWindow();
                                        }
                                        catch (Exception ex)
                                        {
                                            await new MessageBox("Error!", "Error: " + ex.Message).ShowDialog(this);
                                            Dependencies = prevDeps;
                                            BuildWindow();
                                        }
                                        done = true;
                                    }
                                };

                                ContextMenu menu = new ContextMenu() { Items = new List<MenuItem>() { cond } };

                                menu.Open(this.FindControl<Canvas>("OverlayCanvas"));
                            }
                        }
                    }
                }
                else
                {
                    if (draggingDependency.Type == CharacterDependency.Types.Conditioned)
                    {
                        MenuItem cond = new MenuItem() { Header = "Make " + draggingDependency.Index.ToString() + " independent" };

                        CharacterDependency originalDependency = draggingDependency;

                        CharacterDependency[][] prevDeps = Dependencies;

                        cond.Click += async (s, ev) =>
                        {
                            if (!done)
                            {
                                List<List<CharacterDependency>> listedDependencies = (from el in Dependencies select (from el2 in el select el2).ToList()).ToList();

                                int finalSet = listedDependencies.FindIndex(a => a.Contains(originalDependency));

                                int prevIndex = listedDependencies[finalSet].FindIndex(a => a == originalDependency);

                                listedDependencies[finalSet].Remove(originalDependency);

                                CharacterDependency newDep = new CharacterDependency(originalDependency.Index);
                                listedDependencies[finalSet].Insert(prevIndex, newDep);

                                List<List<CharacterDependency>> toBeRemoved = new List<List<CharacterDependency>>();
                                for (int i = 0; i < listedDependencies.Count; i++)
                                {
                                    if (listedDependencies[i].Count == 0)
                                    {
                                        toBeRemoved.Add(listedDependencies[i]);
                                    }
                                }

                                for (int i = 0; i < toBeRemoved.Count; i++)
                                {
                                    listedDependencies.Remove(toBeRemoved[i]);
                                }

                                Dependencies = (from el in listedDependencies select el.ToArray()).ToArray();

                                try
                                {
                                    BuildWindow();
                                }
                                catch (Exception ex)
                                {
                                    await new MessageBox("Error!", "Error: " + ex.Message).ShowDialog(this);
                                    Dependencies = prevDeps;
                                    BuildWindow();
                                }
                                done = true;
                            }
                        };

                        ContextMenu menu = new ContextMenu() { Items = new List<MenuItem>() { cond } };

                        menu.Open(this.FindControl<Canvas>("OverlayCanvas"));
                    }
                    else if (draggingDependency.Type == CharacterDependency.Types.Dependent)
                    {
                        MenuItem cond = new MenuItem() { Header = "Make " + draggingDependency.Dependencies[draggingIndex].ToString() + " independent" };

                        CharacterDependency originalDependency = draggingDependency;
                        int originalIndex = draggingIndex;

                        CharacterDependency[][] prevDeps = Dependencies;

                        cond.Click += async (s, ev) =>
                        {
                            if (!done)
                            {
                                List<List<CharacterDependency>> listedDependencies = (from el in Dependencies select (from el2 in el select el2).ToList()).ToList();

                                int finalSet = listedDependencies.FindIndex(a => a.Contains(originalDependency));

                                int prevIndex = listedDependencies[finalSet].FindIndex(a => a == originalDependency);

                                listedDependencies[finalSet].Remove(originalDependency);

                                List<int> newDeps = new List<int>(originalDependency.Dependencies);

                                newDeps.Remove(originalDependency.Dependencies[originalIndex]);

                                if (newDeps.Count > 1)
                                {
                                    CharacterDependency newDep1 = new CharacterDependency(-1, CharacterDependency.Types.Dependent, newDeps.ToArray(), new Dictionary<string, Parameter>());
                                    listedDependencies[finalSet].Insert(prevIndex, newDep1);

                                    CharacterDependency newDep2 = new CharacterDependency(originalDependency.Dependencies[originalIndex]);
                                    listedDependencies[finalSet].Insert(prevIndex + 1, newDep2);
                                }
                                else
                                {
                                    CharacterDependency newDep1 = new CharacterDependency(newDeps[0]);
                                    listedDependencies[finalSet].Insert(prevIndex, newDep1);

                                    CharacterDependency newDep2 = new CharacterDependency(originalDependency.Dependencies[originalIndex]);
                                    listedDependencies[finalSet].Insert(prevIndex + 1, newDep2);
                                }

                                List<List<CharacterDependency>> toBeRemoved = new List<List<CharacterDependency>>();
                                for (int i = 0; i < listedDependencies.Count; i++)
                                {
                                    if (listedDependencies[i].Count == 0)
                                    {
                                        toBeRemoved.Add(listedDependencies[i]);
                                    }
                                }

                                for (int i = 0; i < toBeRemoved.Count; i++)
                                {
                                    listedDependencies.Remove(toBeRemoved[i]);
                                }

                                Dependencies = (from el in listedDependencies select el.ToArray()).ToArray();

                                try
                                {
                                    BuildWindow();
                                }
                                catch (Exception ex)
                                {
                                    await new MessageBox("Error!", "Error: " + ex.Message).ShowDialog(this);
                                    Dependencies = prevDeps;
                                    BuildWindow();
                                }
                                done = true;
                            }
                        };

                        ContextMenu menu = new ContextMenu() { Items = new List<MenuItem>() { cond } };

                        menu.Open(this.FindControl<Canvas>("OverlayCanvas"));
                    }
                }
            }

            draggingDependency = null;
            draggingIndex = -1;

            this.FindControl<Canvas>("OverlayCanvas").Children.Clear();
        }

        private void MouseMove(object sender, PointerEventArgs e)
        {
            if (IsEditable && draggingDependency != null)
            {
                Canvas overlayCanvas = this.FindControl<Canvas>("OverlayCanvas");
                Point pos = e.GetPosition(overlayCanvas);
                if (pos.X >= 0 && pos.Y >= 0 && pos.X <= overlayCanvas.Bounds.Width && pos.Y <= overlayCanvas.Bounds.Height)
                {
                    Control bord = (Control)overlayCanvas.Children[0];
                    bord.Margin = new Thickness(pos.X - bord.Width / 2, pos.Y - bord.Height / 2, 0, 0);
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
