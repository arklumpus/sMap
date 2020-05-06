using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using Utils;
using System.Linq;

namespace sMap_GUI
{
    public class ViewDataWindow : Window
    {
        public ViewDataWindow()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        public ViewDataWindow(DataMatrix dat)
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            Grid mainGrid = this.FindControl<Grid>("MainGrid");

            mainGrid.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));

            mainGrid.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));
            mainGrid.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));

            {
                TextBlock blk = new TextBlock() { Text = "Characters:", FontWeight = FontWeight.Bold, Margin = new Thickness(10, 5, 10, 5) };
                mainGrid.Children.Add(blk);
            }

            {
                TextBlock blk = new TextBlock() { Text = "States:", FontWeight = FontWeight.Bold, Margin = new Thickness(10, 5, 10, 5), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                Grid.SetRow(blk, 1);
                mainGrid.Children.Add(blk);
            }

            int stateInd = 0;

            int totalStates = (from el in dat.States select el.Length).Sum();

            for (int i = 0; i < dat.States.Length; i++)
            {
                mainGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));

                if (i < dat.States.Length - 1)
                {
                    mainGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Pixel));
                    Canvas cn = new Canvas() { Background = new SolidColorBrush(Colors.Black) };
                    Grid.SetColumn(cn, i * 2 + 2);
                    Grid.SetRowSpan(cn, 2 + dat.Data.Count);
                    mainGrid.Children.Add(cn);
                }

                TextBlock blk = new TextBlock() { Text = i.ToString(), FontWeight = FontWeight.Bold, Margin = new Thickness(10, 5, 10, 5), TextAlignment = TextAlignment.Center, FontSize = 20 };
                Grid.SetColumn(blk, i * 2 + 1);
                mainGrid.Children.Add(blk);

                Grid charGrid = new Grid();
                charGrid.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
                int cols = (int)Math.Ceiling(Math.Sqrt(dat.States[i].Length));
                int rows = (int)Math.Ceiling((double)dat.States[i].Length / cols);

                for (int j = 0; j < rows; j++)
                {
                    charGrid.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));
                }

                for (int j = 0; j < cols; j++)
                {
                    charGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
                }

                for (int j = 0; j < dat.States[i].Length; j++)
                {
                    StackPanel pnl = new StackPanel() { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Margin = new Thickness(5) };
                    pnl.Children.Add(new TextBlock() { Text = dat.States[i][j], FontWeight = FontWeight.Bold, Margin = new Thickness(5) });
                    (int, int, int, double) col = Utils.Plotting.GetColor(stateInd, 1, totalStates);

                    pnl.Children.Add(new Ellipse() { Width = 24, Height = 24, Fill = new SolidColorBrush(Color.FromArgb(255, (byte)col.Item1, (byte)col.Item2, (byte)col.Item3)), Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 1.5, Margin = new Thickness(5) });

                    Grid.SetRow(pnl, j / cols);
                    Grid.SetColumn(pnl, j % cols);

                    charGrid.Children.Add(pnl);

                    stateInd++;
                }

                Grid.SetRow(charGrid, 1);
                Grid.SetColumn(charGrid, i * 2 + 1);

                mainGrid.Children.Add(charGrid);
            }

            int ind = 2;

            foreach (KeyValuePair<string, double[][]> kvp in dat.Data)
            {
                if (ind % 2 == 0)
                {
                    Canvas can = new Canvas() { Background = new SolidColorBrush(Color.FromArgb(255, 240, 240, 240)), ZIndex = -1 };
                    Grid.SetRow(can, ind);
                    Grid.SetColumnSpan(can, 1 + 2 * kvp.Value.Length);
                    mainGrid.Children.Add(can);
                }

                stateInd = 0;

                mainGrid.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));

                TextBlock blk = new TextBlock() { Text = kvp.Key, Margin = new Thickness(10, 5, 10, 5) };
                Grid.SetRow(blk, ind);

                mainGrid.Children.Add(blk);

                Random rnd = new Random();

                double[][] adjustedValue = new double[kvp.Value.Length][];

                for (int i = 0; i < kvp.Value.Length; i++)
                {
                    double sum = kvp.Value[i].Sum();

                    adjustedValue[i] = (from el in kvp.Value[i] select el / sum).ToArray();
                }


                for (int i = 0; i < adjustedValue.Length; i++)
                {
                    Canvas ell = new Canvas() { Width = 24, Height = 24 };

                    double lastAngle = 0;

                    for (int j = 0; j < adjustedValue[i].Length; j++)
                    {
                        if (adjustedValue[i][j] >= 0.0001 && adjustedValue[i][j] <= 0.9999)
                        {
                            double finalAngle = lastAngle + adjustedValue[i][j];

                            (int, int, int, double) col = Utils.Plotting.GetColor(stateInd, 1, totalStates);

                            Path pth = new Path() { Fill = new SolidColorBrush(Color.FromArgb(255, (byte)col.Item1, (byte)col.Item2, (byte)col.Item3)) };

                            PathGeometry geo = new PathGeometry();
                            PathFigure fig = new PathFigure();
                            fig.StartPoint = new Point(12, 12);
                            fig.StartPoint = new Point(12 + 12 * Math.Cos(lastAngle * 2 * Math.PI), 12 + 12 * Math.Sin(lastAngle * 2 * Math.PI));
                            fig.Segments.Add(new ArcSegment() { Point = new Point(12 + 12 * Math.Cos(finalAngle * 2 * Math.PI), 12 + 12 * Math.Sin(finalAngle * 2 * Math.PI)), SweepDirection = SweepDirection.Clockwise, Size = new Size(12, 12), IsLargeArc = adjustedValue[i][j] > 0.5 });
                            fig.Segments.Add(new LineSegment() { Point = new Point(12, 12) });

                            geo.Figures.Add(fig);
                            pth.Data = geo;

                            ell.Children.Add(pth);

                            Ellipse border = new Ellipse() { Width = 24, Height = 24, Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 1.5 };

                            ell.Children.Add(border);

                            lastAngle = finalAngle;
                        }
                        else if (adjustedValue[i][j] >= 0.9999)
                        {
                            (int, int, int, double) col = Utils.Plotting.GetColor(stateInd, 1, totalStates);

                            Ellipse pth = new Ellipse() { Width = 24, Height = 24, Fill = new SolidColorBrush(Color.FromArgb(255, (byte)col.Item1, (byte)col.Item2, (byte)col.Item3)), Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 1.5 };

                            ell.Children.Add(pth);
                        }

                        stateInd++;
                    }

                    Grid.SetRow(ell, ind);
                    Grid.SetColumn(ell, i * 2 + 1);

                    mainGrid.Children.Add(ell);
                }

                ind++;
            }

            //Workaround Avalonia bug
            async void resize()
            {
                await System.Threading.Tasks.Task.Delay(100);
                this.Height = this.Height + 1;
            };

            resize();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
