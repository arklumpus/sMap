using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utils;
using VectSharp;
using VectSharp.Canvas;

namespace sMap_GUI
{
    public class DDistributionWindow : Window
    {
        public DDistributionWindow()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        double bandwidth;
        DTest test;
        DStats[] allDStats;
        string[][] currStates;
        double[][] times;

        public DDistributionWindow(SerializedRun Run, int char1, int char2, double autoBandwidth)
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            test = Run.ComputeDTest(char1, char2);

            currStates = new string[][] { new HashSet<string>(from el in Run.AllPossibleStates select el.Split(',')[char1]).ToArray(), new HashSet<string>(from el in Run.AllPossibleStates select el.Split(',')[char2]).ToArray() };

            if (Run.AllDStats != null)
            {
                allDStats = new DStats[Run.AllDStats.Length];

                for (int i = 0; i < Run.AllDStats.Length; i++)
                {
                    allDStats[i] = DStats.GetAverageDStats(Run.AllDStats[i][Math.Min(char1, char2)][Math.Max(char1, char2)]);
                }
            }
            else
            {
                allDStats = new DStats[Run.Histories.Length];

                for (int i = 0; i < Run.Histories.Length; i++)
                {
                    allDStats[i] = Run.GetPriorHistories(i).ComputeAverageDStats(Run.AllPossibleStates, Math.Min(char1, char2), Math.Max(char1, char2));
                }
            }

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

            times = new double[allStates[char1].Length][];

            for (int i = 0; i < allStates[char1].Length; i++)
            {
                times[i] = new double[allStates[char2].Length];
                for (int j = 0; j < allStates[char2].Length; j++)
                {
                    times[i][j] = myTimes[allStates[char1][i] + "," + allStates[char2][j]] / totalTime;
                }
            }

            Document doc = new Document();

            bandwidth = autoBandwidth;
            double integral = Plotting.DrawDTestDistributions(doc, test, allDStats, times, currStates, ref bandwidth);

            this.FindControl<TextBlock>("PValueBlock").Text = "P = " + test.P.ToString(3, false) + " (" + integral.ToString(3, false) + ")";

            this.FindControl<Viewbox>("HistogramViewBox").Child = doc.Pages[0].PaintToCanvas();
            this.FindControl<Viewbox>("KDEViewBox").Child = doc.Pages[1].PaintToCanvas();
            this.FindControl<NumericUpDown>("BandwidthBox").Value = -Math.Log10(bandwidth);

            //Workaround Avalonia bug
            async void resize()
            {
                await Task.Delay(100);
                this.Width = this.Width + 1;
            };

            resize();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void BandwidthChanged(object sender, NumericUpDownValueChangedEventArgs e)
        {
            bandwidth = Math.Pow(10, -e.NewValue);

            Document doc = new Document();

            double integral = Plotting.DrawDTestDistributions(doc, test, allDStats, times, currStates, ref bandwidth);

            this.FindControl<TextBlock>("PValueBlock").Text = "P = " + test.P.ToString(3, false) + " (" + integral.ToString(3, false) + ")";
            this.FindControl<Viewbox>("KDEViewBox").Child = doc.Pages[1].PaintToCanvas();
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
    }
}
