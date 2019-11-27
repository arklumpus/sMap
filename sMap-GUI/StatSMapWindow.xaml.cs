using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Utils;
using VectSharp;
using VectSharp.Canvas;

namespace sMap_GUI
{
    public class StatSMapWindow : Window
    {
        public StatSMapWindow()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        SerializedRun Run;

        public StatSMapWindow(SerializedRun run)
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            Run = run;

            Dictionary<string, int> totalTransitions = new Dictionary<string, int>();

            for (int i = 0; i < run.AllPossibleStates.Length; i++)
            {
                for (int j = 0; j < run.AllPossibleStates.Length; j++)
                {
                    if (i != j)
                    {
                        totalTransitions.Add(run.AllPossibleStates[i] + ">" + run.AllPossibleStates[j], 0);
                    }
                }
            }

            Dictionary<string, double> totalTimes = new Dictionary<string, double>();

            for (int i = 0; i < run.AllPossibleStates.Length; i++)
            {
                totalTimes.Add(run.AllPossibleStates[i], 0);
            }

            for (int i = 0; i < run.Histories.Length; i++)
            {
                Dictionary<string, double> historyTimes = run.Histories[i].GetTimes(run.AllPossibleStates);
                foreach (KeyValuePair<string, double> kvp in historyTimes)
                {
                    totalTimes[kvp.Key] += kvp.Value;
                }
            }

            for (int i = 0; i < run.Histories.Length; i++)
            {
                Dictionary<string, int> historyTransitions = run.Histories[i].GetTransitions(run.AllPossibleStates);
                foreach (KeyValuePair<string, int> kvp in historyTransitions)
                {
                    totalTransitions[kvp.Key] += kvp.Value;
                }
            }

            double totalTime = (from el in totalTimes select el.Value).Sum();

            string[][] allStates = new string[run.AllPossibleStates[0].Split(',').Length][];

            Grid totalTransitionsGrid = this.FindControl<Grid>("TransitionsGrid");
            totalTransitionsGrid.RowDefinitions.Clear();
            totalTransitionsGrid.ColumnDefinitions.Clear();

            totalTransitionsGrid.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));
            totalTransitionsGrid.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));

            Grid totalTimesGrid = this.FindControl<Grid>("TimesGrid");
            totalTimesGrid.RowDefinitions.Clear();

            for (int i = 0; i < run.AllPossibleStates.Length; i++)
            {
                totalTransitionsGrid.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));
                totalTransitionsGrid.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));

                totalTimesGrid.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));

                Border timesBrd = new Border() { Background = Program.GetTransparentBrush(Utils.Plotting.GetColor(i, 0.25, run.AllPossibleStates.Length)), BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Margin = new Thickness(1.5) };
                TextBlock timesBlock = new TextBlock() { Text = run.AllPossibleStates[i], Margin = new Thickness(7.5), FontWeight = Avalonia.Media.FontWeight.Bold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
                Grid.SetRow(timesBrd, i);
                timesBrd.Child = timesBlock;
                totalTimesGrid.Children.Add(timesBrd);

                TextBlock timesValBlk = new TextBlock() { Text = ((double)totalTimes[run.AllPossibleStates[i]] / run.Histories.Length * run.AgeScale).ToString(4, false), Margin = new Thickness(10), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
                Grid.SetRow(timesValBlk, i);
                ToolTip.SetTip(timesValBlk, run.AllPossibleStates[i] + ": " + ((double)totalTimes[run.AllPossibleStates[i]] / run.Histories.Length * run.AgeScale).ToString(System.Globalization.CultureInfo.InvariantCulture));
                Grid.SetColumn(timesValBlk, 1);
                totalTimesGrid.Children.Add(timesValBlk);

                Grid timesGrd = new Grid() { Background = new SolidColorBrush(Color.FromArgb(255, 236, 250, 255)), Margin = new Thickness(2.5), Width = 150, Height = 36 };

                {
                    Canvas can = new Canvas() { Height = 1, Width = 150 };
                    can.Children.Add(new Canvas() { Background = new SolidColorBrush(Color.FromArgb(255, 198, 238, 255)), Height = 1, Width = 150 * totalTimes[run.AllPossibleStates[i]] / totalTime, Margin = new Thickness(0, 0, 100 - 100 * totalTimes[run.AllPossibleStates[i]] / totalTime, 0) });

                    Viewbox vb = new Viewbox() { Stretch = Stretch.Fill };
                    vb.Child = can;

                    timesGrd.Children.Add(vb);

                    TextBlock blk = new TextBlock() { Text = (totalTimes[run.AllPossibleStates[i]] / totalTime).ToString("0.0%", System.Globalization.CultureInfo.InvariantCulture), Margin = new Thickness(7.5), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                    ToolTip.SetTip(timesGrd, run.AllPossibleStates[i] + ": " + (totalTimes[run.AllPossibleStates[i]] / totalTime).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    Grid.SetRow(timesGrd, i);
                    Grid.SetColumn(timesGrd, 2);
                    timesGrd.Children.Add(blk);
                }

                totalTimesGrid.Children.Add(timesGrd);



                Border rowBrd = new Border() { Background = Program.GetTransparentBrush(Utils.Plotting.GetColor(i, 0.25, run.AllPossibleStates.Length)), BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Margin = new Thickness(1.5) };
                TextBlock rowBlock = new TextBlock() { Text = run.AllPossibleStates[i], Margin = new Thickness(7.5), FontWeight = Avalonia.Media.FontWeight.Bold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
                Grid.SetColumn(rowBrd, i + 1);
                rowBrd.Child = rowBlock;
                totalTransitionsGrid.Children.Add(rowBrd);

                Border columnBrd = new Border() { Background = Program.GetTransparentBrush(Utils.Plotting.GetColor(i, 0.25, run.AllPossibleStates.Length)), BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Margin = new Thickness(1.5) };
                TextBlock columnBlock = new TextBlock() { Text = run.AllPossibleStates[i], Margin = new Thickness(10), FontWeight = Avalonia.Media.FontWeight.Bold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
                Grid.SetRow(columnBrd, i + 1);
                columnBrd.Child = columnBlock;
                totalTransitionsGrid.Children.Add(columnBrd);

                double maxTransitions = (double)(from el in totalTransitions select el.Value).Max() / run.Histories.Length;

                for (int j = 0; j < run.AllPossibleStates.Length; j++)
                {
                    if (i != j)
                    {
                        double transVal = ((double)totalTransitions[run.AllPossibleStates[i] + ">" + run.AllPossibleStates[j]] / run.Histories.Length);

                        string trans = transVal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

                        Grid grd = new Grid() { Background = new SolidColorBrush(Color.FromArgb(255, 236, 250, 255)), Margin = new Thickness(2.5), Width = 75, Height = 36 };

                        Canvas can = new Canvas() { Width = 1, Height = 100 };
                        can.Children.Add(new Canvas() { Background = new SolidColorBrush(Color.FromArgb(255, 198, 238, 255)), Width = 1, Height = 100 * transVal / maxTransitions, Margin = new Thickness(0, 100 - 100 * transVal / maxTransitions, 0, 0) });

                        Viewbox vb = new Viewbox() { Stretch = Stretch.Fill };
                        vb.Child = can;

                        grd.Children.Add(vb);

                        TextBlock blk = new TextBlock() { Text = trans, Margin = new Thickness(7.5), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                        ToolTip.SetTip(grd, run.AllPossibleStates[i] + " > " + run.AllPossibleStates[j] + ": " + transVal.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        Grid.SetRow(grd, i + 1);
                        Grid.SetColumn(grd, j + 1);
                        grd.Children.Add(blk);

                        totalTransitionsGrid.Children.Add(grd);
                    }
                }
            }

            if (allStates.Length > 1)
            {
                for (int i = 0; i < allStates.Length; i++)
                {
                    allStates[i] = new HashSet<string>(from el in run.AllPossibleStates select el.Split(',')[i]).ToArray();
                }

                Dictionary<string, int>[] marginalTransitions = new Dictionary<string, int>[allStates.Length];

                for (int i = 0; i < allStates.Length; i++)
                {
                    marginalTransitions[i] = new Dictionary<string, int>();

                    for (int j = 0; j < allStates[i].Length; j++)
                    {
                        for (int k = 0; k < allStates[i].Length; k++)
                        {
                            if (j != k)
                            {
                                marginalTransitions[i].Add(allStates[i][j] + ">" + allStates[i][k], 0);
                            }
                        }
                    }

                    foreach (KeyValuePair<string, int> kvp in totalTransitions)
                    {
                        string leftState = kvp.Key.Substring(0, kvp.Key.IndexOf(">")).Split(',')[i];
                        string rightState = kvp.Key.Substring(kvp.Key.IndexOf(">") + 1).Split(',')[i];

                        if (leftState != rightState)
                        {
                            marginalTransitions[i][leftState + ">" + rightState] += kvp.Value;
                        }
                    }
                }


                Dictionary<string, double>[] marginalTimes = new Dictionary<string, double>[allStates.Length];

                for (int i = 0; i < allStates.Length; i++)
                {
                    marginalTimes[i] = new Dictionary<string, double>();

                    for (int j = 0; j < allStates[i].Length; j++)
                    {
                        marginalTimes[i].Add(allStates[i][j], 0);
                    }

                    foreach (KeyValuePair<string, double> kvp in totalTimes)
                    {
                        string state = kvp.Key.Split(',')[i];
                        marginalTimes[i][state] += kvp.Value;
                    }
                }

                StackPanel margPanel = this.FindControl<StackPanel>("MarginalTransitionsPanel");
                margPanel.Children.Clear();

                StackPanel margTimesPanel = this.FindControl<StackPanel>("MarginalTimesPanel");
                margTimesPanel.Children.Clear();

                for (int k = 0; k < marginalTransitions.Length; k++)
                {
                    TextBlock timesCharBlock = new TextBlock() { Text = "Character " + k.ToString() + ":", FontWeight = FontWeight.Bold, Margin = new Thickness(5) };
                    margTimesPanel.Children.Add(timesCharBlock);

                    Grid marginalTimeGrid = new Grid() { Margin = new Thickness(0, 0, 0, 20) };
                    marginalTimeGrid.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));
                    marginalTimeGrid.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));
                    marginalTimeGrid.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));

                    TextBlock charBlock = new TextBlock() { Text = "Character " + k.ToString() + ":", FontWeight = FontWeight.Bold, Margin = new Thickness(5) };
                    margPanel.Children.Add(charBlock);

                    Grid marginalTransitionsGrid = new Grid() { Margin = new Thickness(0, 0, 0, 20) };
                    marginalTransitionsGrid.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));
                    marginalTransitionsGrid.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));

                    for (int i = 0; i < allStates[k].Length; i++)
                    {
                        marginalTimeGrid.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));

                        Border timesBrd = new Border() { Background = Program.GetTransparentBrush(Utils.Plotting.GetColor(i, 0.25, allStates[k].Length)), BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Margin = new Thickness(1.5) };
                        TextBlock timesBlock = new TextBlock() { Text = allStates[k][i], Margin = new Thickness(7.5), FontWeight = Avalonia.Media.FontWeight.Bold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
                        Grid.SetRow(timesBrd, i);
                        timesBrd.Child = timesBlock;
                        marginalTimeGrid.Children.Add(timesBrd);

                        TextBlock timesValBlk = new TextBlock() { Text = ((double)marginalTimes[k][allStates[k][i]] / run.Histories.Length * run.AgeScale).ToString(4, false), Margin = new Thickness(10), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
                        Grid.SetRow(timesValBlk, i);
                        ToolTip.SetTip(timesValBlk, allStates[k][i] + ": " + ((double)marginalTimes[k][allStates[k][i]] / run.Histories.Length * run.AgeScale).ToString(System.Globalization.CultureInfo.InvariantCulture));
                        Grid.SetColumn(timesValBlk, 1);
                        marginalTimeGrid.Children.Add(timesValBlk);

                        Grid timesGrd = new Grid() { Background = new SolidColorBrush(Color.FromArgb(255, 236, 250, 255)), Margin = new Thickness(2.5), Width = 150, Height = 36 };

                        {
                            Canvas can = new Canvas() { Height = 1, Width = 150 };
                            can.Children.Add(new Canvas() { Background = new SolidColorBrush(Color.FromArgb(255, 198, 238, 255)), Height = 1, Width = 150 * marginalTimes[k][allStates[k][i]] / totalTime, Margin = new Thickness(0, 0, 100 - 100 * marginalTimes[k][allStates[k][i]] / totalTime, 0) });

                            Viewbox vb = new Viewbox() { Stretch = Stretch.Fill };
                            vb.Child = can;

                            timesGrd.Children.Add(vb);

                            TextBlock blk = new TextBlock() { Text = (marginalTimes[k][allStates[k][i]] / totalTime).ToString("0.0%", System.Globalization.CultureInfo.InvariantCulture), Margin = new Thickness(7.5), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                            ToolTip.SetTip(timesGrd, allStates[k][i] + ": " + (marginalTimes[k][allStates[k][i]] / totalTime).ToString(System.Globalization.CultureInfo.InvariantCulture));
                            Grid.SetRow(timesGrd, i);
                            Grid.SetColumn(timesGrd, 2);
                            timesGrd.Children.Add(blk);
                        }

                        marginalTimeGrid.Children.Add(timesGrd);



                        marginalTransitionsGrid.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));
                        marginalTransitionsGrid.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));


                        Border rowBrd = new Border() { Background = Program.GetTransparentBrush(Utils.Plotting.GetColor(i, 0.25, allStates[k].Length)), BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Margin = new Thickness(1.5) };
                        TextBlock rowBlock = new TextBlock() { Text = allStates[k][i], Margin = new Thickness(7.5), FontWeight = Avalonia.Media.FontWeight.Bold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
                        Grid.SetColumn(rowBrd, i + 1);
                        rowBrd.Child = rowBlock;
                        marginalTransitionsGrid.Children.Add(rowBrd);

                        Border columnBrd = new Border() { Background = Program.GetTransparentBrush(Utils.Plotting.GetColor(i, 0.25, allStates[k].Length)), BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Margin = new Thickness(1.5) };
                        TextBlock columnBlock = new TextBlock() { Text = allStates[k][i], Margin = new Thickness(10), FontWeight = Avalonia.Media.FontWeight.Bold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
                        Grid.SetRow(columnBrd, i + 1);
                        columnBrd.Child = columnBlock;
                        marginalTransitionsGrid.Children.Add(columnBrd);

                        double maxTransitions = (double)(from el in marginalTransitions[k] select el.Value).Max() / run.Histories.Length;

                        for (int j = 0; j < allStates[k].Length; j++)
                        {
                            if (i != j)
                            {
                                double transVal = ((double)marginalTransitions[k][allStates[k][i] + ">" + allStates[k][j]] / run.Histories.Length);

                                string trans = transVal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

                                Grid grd = new Grid() { Background = new SolidColorBrush(Color.FromArgb(255, 236, 250, 255)), Margin = new Thickness(2.5), Width = 75, Height = 36 };

                                Canvas can = new Canvas() { Width = 1, Height = 100 };
                                can.Children.Add(new Canvas() { Background = new SolidColorBrush(Color.FromArgb(255, 192, 238, 255)), Width = 1, Height = 100 * transVal / maxTransitions, Margin = new Thickness(0, 100 - 100 * transVal / maxTransitions, 0, 0) });

                                Viewbox vb = new Viewbox() { Stretch = Stretch.Fill };
                                vb.Child = can;

                                grd.Children.Add(vb);

                                TextBlock blk = new TextBlock() { Text = trans, Margin = new Thickness(7.5), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                                ToolTip.SetTip(grd, allStates[k][i] + " > " + allStates[k][j] + ": " + transVal.ToString(System.Globalization.CultureInfo.InvariantCulture));
                                Grid.SetRow(grd, i + 1);
                                Grid.SetColumn(grd, j + 1);
                                grd.Children.Add(blk);

                                marginalTransitionsGrid.Children.Add(grd);
                            }
                        }
                    }

                    margPanel.Children.Add(marginalTransitionsGrid);
                    margTimesPanel.Children.Add(marginalTimeGrid);
                }

                if ((Run.revision == 2 && Run.AllDStats != null && Run.AllDStats.Length > 0) || Run.revision == 3)
                {
                    this.FindControl<ComboBox>("CharacterBox1").Items = new List<ComboBoxItem>(from el in Utils.Utils.Range(0, allStates.Length) select new ComboBoxItem() { Content = el.ToString() });
                    this.FindControl<ComboBox>("CharacterBox1").SelectedIndex = 0;
                    this.FindControl<ComboBox>("CharacterBox2").Items = new List<ComboBoxItem>(from el in Utils.Utils.Range(0, allStates.Length) select new ComboBoxItem() { Content = el.ToString() });
                    this.FindControl<ComboBox>("CharacterBox2").SelectedIndex = 1;
                }
                else
                {
                    this.FindControl<StackPanel>("DTestPanel").IsVisible = false;
                }
            }
            else
            {
                this.FindControl<Grid>("MainGrid").RowDefinitions[4] = new RowDefinition(0, GridUnitType.Pixel);
                this.FindControl<Grid>("MainGrid").ColumnDefinitions[1] = new ColumnDefinition(0, GridUnitType.Pixel);
            }
        }

        private async void ShowDTestClicked(object sender, RoutedEventArgs e)
        {
            int char1 = this.FindControl<ComboBox>("CharacterBox1").SelectedIndex;
            int char2 = this.FindControl<ComboBox>("CharacterBox2").SelectedIndex;

            if (char1 == char2)
            {
                await new MessageBox("Attention", "You need to specify two different characters to perform the D-test!").ShowDialog(this);
                return;
            }

            EventWaitHandle startHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

            PlotProgressWindow win = new PlotProgressWindow(startHandle);

            win.ProgressText = "Performing D-test...";

            DTest test = null;

            Thread thr = new Thread(async () =>
            {
                startHandle.WaitOne();

                test = Run.ComputeDTest(char1, char2, async v =>
                {

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        win.Progress = v * 0.5;
                    });
                });

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    win.Close();
                });
            });

            thr.Start();

            await win.ShowDialog(this);

            string[][] currStates = new string[][] { new HashSet<string>(from el in Run.AllPossibleStates select el.Split(',')[char1]).ToArray(), new HashSet<string>(from el in Run.AllPossibleStates select el.Split(',')[char2]).ToArray() };

            DStats[] allDStats = null;


            startHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

            win = new PlotProgressWindow(startHandle);

            win.ProgressText = "Performing D-test...";

            thr = new Thread(async () =>
            {
                startHandle.WaitOne();

                if (Run.AllDStats != null)
                {
                    allDStats = new DStats[Run.AllDStats.Length];

                    double lastProgress = -1;

                    for (int i = 0; i < Run.AllDStats.Length; i++)
                    {
                        allDStats[i] = DStats.GetAverageDStats(Run.AllDStats[i][Math.Min(char1, char2)][Math.Max(char1, char2)]);

                        double progress = (double)(i + 1) / Run.AllDStats.Length;
                        
                        if (Math.Round(progress * 100) > Math.Round(lastProgress * 100))
                        {
                            lastProgress = progress;
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                win.Progress = 0.5 + progress * 0.5;
                            });
                        }
                    }
                }
                else
                {
                    allDStats = new DStats[Run.Histories.Length];

                    double lastProgress = -1;

                    for (int i = 0; i < Run.Histories.Length; i++)
                    {
                        allDStats[i] = Run.GetPriorHistories(i).ComputeAverageDStats(Run.AllPossibleStates, Math.Min(char1, char2), Math.Max(char1, char2));

                        double progress = (double)(i + 1) / Run.Histories.Length;

                        if (Math.Round(progress * 100) > Math.Round(lastProgress * 100))
                        {
                            lastProgress = progress;
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                win.Progress = 0.5 + progress * 0.5;
                            });
                        }
                    }
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    win.Close();
                });
            });

            thr.Start();

            await win.ShowDialog(this);

            Dictionary<string, double> totalTimes = new Dictionary<string, double>();

            for (int i = 0; i < Run.AllPossibleStates.Length; i++)
            {
                totalTimes.Add(Run.AllPossibleStates[i], 0);
            }

            for (int i = 0; i < Run.Histories.Length; i++)
            {
                Dictionary<string, double> historyTimes = Run.Histories[i].GetTimes(Run.AllPossibleStates);
                foreach (KeyValuePair<string, double> kvp in historyTimes)
                {
                    totalTimes[kvp.Key] += kvp.Value;
                }
            }

            string[][] allStates = new string[Run.AllPossibleStates[0].Split(',').Length][];

            for (int i = 0; i < allStates.Length; i++)
            {
                allStates[i] = new HashSet<string>(from el in Run.AllPossibleStates select el.Split(',')[i]).ToArray();
            }

            Dictionary<string, double> myTimes = new Dictionary<string, double>();

            string[] myStates = new HashSet<string>(from el in Run.AllPossibleStates select el.Split(',')[char1] + "," + el.Split(',')[char2]).ToArray();

            for (int i = 0; i < myStates.Length; i++)
            {
                myTimes.Add(myStates[i], 0);
            }

            foreach (KeyValuePair<string, double> kvp in totalTimes)
            {
                string state = kvp.Key.Split(',')[char1] + "," + kvp.Key.Split(',')[char2];
                myTimes[state] += kvp.Value;
            }

            double totalTime = (from el in totalTimes select el.Value).Sum();

            double[][] times = new double[allStates[char1].Length][];

            for (int i = 0; i < allStates[char1].Length; i++)
            {
                times[i] = new double[allStates[char2].Length];
                for (int j = 0; j < allStates[char2].Length; j++)
                {
                    times[i][j] = myTimes[allStates[char1][i] + "," + allStates[char2][j]] / totalTime;
                }
            }



            Document doc = new Document();

            lastBandwidth = -1;
            double integral = Plotting.DrawDTestDistributions(doc, test, allDStats, times, currStates, ref lastBandwidth);

            this.FindControl<Viewbox>("HistogramViewBox").Child = doc.Pages[0].PaintToCanvas();
            this.FindControl<Viewbox>("KDEViewBox").Child = doc.Pages[1].PaintToCanvas();

            this.FindControl<Grid>("MainGrid").RowDefinitions[5] = new RowDefinition(1, GridUnitType.Star);

            this.FindControl<TextBlock>("InvolvedCharactersBlock").Text = "Character " + char1.ToString() + " vs " + char2.ToString();

            this.FindControl<TextBlock>("DvalueBlock").Text = "D = " + test.DStats.D.ToString(System.Globalization.CultureInfo.InvariantCulture);
            this.FindControl<TextBlock>("PvalueBlock").Text = "P = " + test.P.ToString(3, false) + " (" + integral.ToString(3, false) + ")";
            ToolTip.SetTip(this.FindControl<TextBlock>("PvalueBlock"), test.P.ToString(System.Globalization.CultureInfo.InvariantCulture) + " (" + integral.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")");

            Grid dijGrid = this.FindControl<Grid>("dijGrid");
            dijGrid.Children.Clear();
            dijGrid.RowDefinitions.Clear();
            dijGrid.ColumnDefinitions.Clear();

            dijGrid.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));
            dijGrid.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));

            for (int i = 0; i < currStates[0].Length; i++)
            {
                dijGrid.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));

                dijGrid.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));
                Border columnBrd = new Border() { Background = Program.GetTransparentBrush(Utils.Plotting.GetColor(i, 0.25, currStates[0].Length + currStates[1].Length)), BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Margin = new Thickness(1.5) };
                TextBlock columnBlock = new TextBlock() { Text = currStates[0][i], Margin = new Thickness(10), FontWeight = Avalonia.Media.FontWeight.Bold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
                Grid.SetRow(columnBrd, i + 1);
                columnBrd.Child = columnBlock;
                dijGrid.Children.Add(columnBrd);

                for (int j = 0; j < currStates[1].Length; j++)
                {
                    if (i == 0)
                    {
                        dijGrid.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));

                        Border rowBrd = new Border() { Background = Program.GetTransparentBrush(Utils.Plotting.GetColor(currStates[0].Length + j, 0.25, currStates[1].Length)), BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Margin = new Thickness(1.5) };
                        TextBlock rowBlock = new TextBlock() { Text = currStates[1][j], Margin = new Thickness(7.5), FontWeight = Avalonia.Media.FontWeight.Bold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
                        Grid.SetColumn(rowBrd, j + 1);
                        rowBrd.Child = rowBlock;
                        dijGrid.Children.Add(rowBrd);
                    }

                    TextBlock blk = new TextBlock() { Text = test.DStats.dij[i][j].ToString("0.##%", System.Globalization.CultureInfo.InvariantCulture), Margin = new Thickness(10), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                    ToolTip.SetTip(blk, test.DStats.dij[i][j].ToString(System.Globalization.CultureInfo.InvariantCulture));
                    Grid.SetRow(blk, i + 1);
                    Grid.SetColumn(blk, j + 1);
                    dijGrid.Children.Add(blk);
                }
            }



            Grid PijGrid = this.FindControl<Grid>("PijGrid");
            PijGrid.Children.Clear();
            PijGrid.RowDefinitions.Clear();
            PijGrid.ColumnDefinitions.Clear();

            PijGrid.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));
            PijGrid.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));

            for (int i = 0; i < currStates[0].Length; i++)
            {
                PijGrid.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));

                PijGrid.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));
                Border columnBrd = new Border() { Background = Program.GetTransparentBrush(Utils.Plotting.GetColor(i, 0.25, currStates[0].Length + currStates[1].Length)), BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Margin = new Thickness(1.5) };
                TextBlock columnBlock = new TextBlock() { Text = currStates[0][i], Margin = new Thickness(10), FontWeight = Avalonia.Media.FontWeight.Bold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
                Grid.SetRow(columnBrd, i + 1);
                columnBrd.Child = columnBlock;
                PijGrid.Children.Add(columnBrd);

                for (int j = 0; j < currStates[1].Length; j++)
                {
                    if (i == 0)
                    {
                        PijGrid.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));

                        Border rowBrd = new Border() { Background = Program.GetTransparentBrush(Utils.Plotting.GetColor(currStates[0].Length + j, 0.25, currStates[1].Length)), BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Margin = new Thickness(1.5) };
                        TextBlock rowBlock = new TextBlock() { Text = currStates[1][j], Margin = new Thickness(7.5), FontWeight = Avalonia.Media.FontWeight.Bold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
                        Grid.SetColumn(rowBrd, j + 1);
                        rowBrd.Child = rowBlock;
                        PijGrid.Children.Add(rowBrd);
                    }

                    TextBlock blk = new TextBlock() { Text = test.Pij[i][j].ToString(2, false), Margin = new Thickness(10), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                    ToolTip.SetTip(blk, test.Pij[i][j].ToString(System.Globalization.CultureInfo.InvariantCulture));
                    Grid.SetRow(blk, i + 1);
                    Grid.SetColumn(blk, j + 1);
                    PijGrid.Children.Add(blk);
                }
            }
        }

        private void RadioClicked(object sender, RoutedEventArgs e)
        {
            if (this.FindControl<RadioButton>("HistogramRadioButton").IsChecked == true)
            {
                this.FindControl<Viewbox>("HistogramViewBox").IsVisible = true;
                this.FindControl<Viewbox>("KDEViewBox").IsVisible = false;
            }
            else
            {
                this.FindControl<Viewbox>("HistogramViewBox").IsVisible = false;
                this.FindControl<Viewbox>("KDEViewBox").IsVisible = true;
            }
        }

        double lastBandwidth = -1;

        private async void PlotNewWindowClicked(object sender, RoutedEventArgs e)
        {
            int char1 = this.FindControl<ComboBox>("CharacterBox1").SelectedIndex;
            int char2 = this.FindControl<ComboBox>("CharacterBox2").SelectedIndex;
            DDistributionWindow win = new DDistributionWindow(Run, char1, char2, lastBandwidth);

            await win.ShowDialog(this);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
