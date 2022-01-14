using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using SlimTreeNode;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace sMap_GUI
{
    public class BlendSMapWindow : Window
    {
        public BlendSMapWindow()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        List<string> FileNames = new List<string>();
        List<NumericUpDown> WeightBoxes = new List<NumericUpDown>();

        private async void AddFileClicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog;

            if (!Program.IsMac)
            {
                dialog = new OpenFileDialog() { Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Extensions = Program.IsMac ? new List<string>() { "*" } : new List<string>() { "bin" }, Name = "sMap output file" }, new FileDialogFilter() { Extensions = new List<string>() { "*" }, Name = "All files" } }, AllowMultiple = true, Title = "Choose sMap file" };
            }
            else
            {
                dialog = new OpenFileDialog() { AllowMultiple = true, Title = "Choose sMap file" };
            }


            string[] fileNames = await dialog.ShowAsync(this);

            if (fileNames != null && fileNames.Length > 0)
            {
                for (int i = 0; i < fileNames.Length; i++)
                {
                    FileNames.Add(fileNames[i]);

                    NumericUpDown ud = new NumericUpDown() { VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Value = 1, Padding = new Thickness(0), Margin = new Thickness(0, 5, 0, 5), Minimum = 0 };
                    Grid.SetRow(ud, i);
                    Grid.SetColumn(ud, 1);

                    ud.ValueChanged += (s, ex) =>
                    {
                        UpdateTotalWeight();
                    };

                    WeightBoxes.Add(ud);
                }
                RebuildFileList();
                UpdateTotalWeight();
            }
        }

        private void RebuildFileList()
        {
            double[] weights = (from el in WeightBoxes select el.Value).ToArray();

            Grid container = this.FindControl<Grid>("sMapFileContainer");

            container.RowDefinitions.Clear();

            container.Children.Clear();

            for (int i = 0; i < FileNames.Count; i++)
            {
                container.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));

                if (i % 2 == 0)
                {
                    Canvas can = new Canvas() { Background = new SolidColorBrush(Color.FromArgb(255, 240, 240, 240)) };
                    Grid.SetRow(can, i);
                    Grid.SetColumnSpan(can, 3);
                    container.Children.Add(can);
                }

                ScrollViewer sv = new ScrollViewer() { HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled, Padding = new Thickness(0, 5, 0, 5), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                sv.Content = new TextBlock() { Text = System.IO.Path.GetFileName(FileNames[i]) };

                Grid.SetRow(sv, i);
                container.Children.Add(sv);

                NumericUpDown ud = WeightBoxes[i];
                Grid.SetRow(ud, i);
                Grid.SetColumn(ud, 1);

                container.Children.Add(ud);

                AddButton ad = new AddButton() { Type = AddButton.ButtonTypes.Remove };
                Grid.SetRow(ad, i);
                Grid.SetColumn(ad, 2);

                int j = i;

                ad.PointerPressed += (s, ex) =>
                {
                    FileNames.RemoveAt(j);
                    WeightBoxes.RemoveAt(j);
                    RebuildFileList();
                };

                container.Children.Add(ad);
            }

            UpdateTotalWeight();
        }

        private void UpdateTotalWeight()
        {
            double totalWeight = (from el in WeightBoxes select el.Value).Sum();

            this.FindControl<TextBlock>("TotalWeightBlock").Text = totalWeight.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private async void SaveSMapClicked(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog() { Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Name = "sMap file", Extensions = new List<string>() { "bin" } }, new FileDialogFilter() { Name = "All files", Extensions = new List<string>() { "*" } } }, Title = "Save blended sMap" };

            string result = await dialog.ShowAsync(this);

            if (!string.IsNullOrEmpty(result))
            {
                EventWaitHandle startHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

                PlotProgressWindow win = new PlotProgressWindow(startHandle);

                win.ProgressText = "Reading input files...";

                Thread thr = new Thread(async () =>
                {
                    startHandle.WaitOne();

                    await Blend(result, async (s, v) =>
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            win.ProgressText = s;

                            if (v >= 0)
                            {
                                win.IsIndeterminate = false;
                                win.Progress = v;
                            }
                            else
                            {
                                win.IsIndeterminate = true;
                            }
                        });
                    }, null);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        win.Close();
                    });
                });

                thr.Start();

                await win.ShowDialog(this);

                await new MessageBox("Task completed", "Task completed!", MessageBox.MessageBoxButtonTypes.OK, MessageBox.MessageBoxIconTypes.Tick).ShowDialog(this);
            }
        }


        private async Task Blend(string outputFile, Action<string, double> progressAction, string priorTempFolder)
        {

            string summaryTreeString = null;
            double ageScale = -1;
            string[] allPossibleStates = null;
            string[] states = null;
            int[][] summaryNodeCorresp = null;
            LikelihoodModel[] likModels = null;

            bool treesClockLike = false;

            progressAction("Reading input files...", 0);

            List<SerializedRun> runs = new List<SerializedRun>();

            for (int i = 0; i < FileNames.Count; i++)
            {
                SerializedRun run;

                try
                {
                    run = SerializedRun.Deserialize(FileNames[i]);
                }
                catch (Exception e)
                {
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        MessageBox box = new MessageBox("Error", "Error while parsing input file: " + e.Message);

                        await box.ShowDialog(this);
                    });
                    return;
                }

                if (i == 0)
                {
                    treesClockLike = run.TreesClockLike;
                    summaryTreeString = run.SummaryTree.ToString();
                    ageScale = run.AgeScale;
                    allPossibleStates = run.AllPossibleStates;
                    states = run.States;
                    summaryNodeCorresp = run.SummaryNodeCorresp;
                    likModels = run.LikelihoodModels;
                }
                else
                {
                    if (summaryTreeString != run.SummaryTree.ToString())
                    {
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            MessageBox box = new MessageBox("Error", "The sMap runs use different summary trees!");
                            await box.ShowDialog(this);
                        });

                        return;
                    }

                    if (ageScale != run.AgeScale)
                    {
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            MessageBox box = new MessageBox("Error", "The sMap runs use different tree age normalisation constants!");
                            await box.ShowDialog(this);
                        });
                        return;
                    }

                    if (!allPossibleStates.SequenceEqual(run.AllPossibleStates) || !states.SequenceEqual(run.States))
                    {
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            MessageBox box = new MessageBox("Error", "The sMap runs use different data!");
                            await box.ShowDialog(this);
                        });
                        return;
                    }
                }

                runs.Add(run);

                progressAction("Reading input files...", (double)(i + 1) / FileNames.Count);
            }

            progressAction("Blending character histories...", -1);

            double[] weights = null;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                weights = (from el in WeightBoxes select el.Value).ToArray();
            });

            double sum = weights.Sum();

            int finalCount = -1;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                finalCount = (int)this.FindControl<NumericUpDown>("FinalCountBox").Value;
            });

            int[] sampleCount = Utils.Utils.RoundInts((from el in weights select el * finalCount / sum).ToArray());

            double[][] blendedMeanPosterior = new double[runs[0].MeanPosterior.Length][];
            double[][] blendedMeanPrior = new double[runs[0].MeanPrior.Length][];

            for (int i = 0; i < blendedMeanPosterior.Length; i++)
            {
                blendedMeanPosterior[i] = new double[runs[0].MeanPosterior[i].Length];
                blendedMeanPrior[i] = new double[runs[0].MeanPrior[i].Length];

                for (int j = 0; j < blendedMeanPosterior[i].Length; j++)
                {
                    for (int k = 0; k < runs.Count; k++)
                    {
                        blendedMeanPosterior[i][j] += runs[k].MeanPosterior[i][j] * weights[k];
                        blendedMeanPrior[i][j] += runs[k].MeanPrior[i][j] * weights[k];
                    }
                    blendedMeanPosterior[i][j] /= sum;
                    blendedMeanPrior[i][j] /= sum;
                }
            }

            bool includeDStats = !(from el in runs select el.revision == 2 && el.AllDStats != null).Contains(false);
            bool includePriorHistories = !(from el in runs select el.revision == 3 && el.HasPriorHistories).Contains(false);

            List<TaggedHistory> blendedHistories = new List<TaggedHistory>(finalCount);
            List<DStats[][][]> allDStats = new List<DStats[][][]>(finalCount);
            Stream blendedPriorHistories = null;
            int priorMultiplicity = 0;
            List<long> priorOffsets = new List<long>(finalCount);
            string prefix = Guid.NewGuid().ToString().Replace("-", "");

            if (includePriorHistories)
            {
                priorMultiplicity = (from el in runs select el.PriorMultiplicity).Min();

                if (priorTempFolder == "memory")
                {
                    blendedPriorHistories = new MemoryStream();
                }
                else
                {
                    if (priorTempFolder == null)
                    {
                        priorTempFolder = Path.GetDirectoryName(outputFile);
                    }

                    blendedPriorHistories = new FileStream(Path.Combine(priorTempFolder, prefix + ".prior"), FileMode.Create);
                }
            }

            List<int> blendedTreeSamples = new List<int>(finalCount);

            ThreadSafeRandom rnd = new ThreadSafeRandom();

            int total = sampleCount.Sum();
            int current = 0;

            for (int i = 0; i < sampleCount.Length; i++)
            {
                int[] samples = getSamples(runs[i].Histories.Length, sampleCount[i], rnd);

                for (int j = 0; j < sampleCount[i]; j++)
                {
                    runs[i].Histories[samples[j]].Tag = blendedHistories.Count;
                    blendedHistories.Add(runs[i].Histories[samples[j]]);

                    if (includeDStats)
                    {
                        allDStats.Add(runs[i].AllDStats[samples[j]]);
                    }

                    if (includePriorHistories)
                    {
                        TaggedHistory[] sampledPriorHistories = runs[i].GetPriorHistories(samples[j]);

                        long position = blendedPriorHistories.Position;
                        long[] lengths = new long[priorMultiplicity];

                        for (int k = 0; k < priorMultiplicity; k++)
                        {
                            sampledPriorHistories[k].Tag = blendedHistories.Count - 1;
                            lengths[k] = sampledPriorHistories[k].WriteToStream(blendedPriorHistories);
                        }

                        blendedPriorHistories.Flush();

                        priorOffsets.Add(blendedPriorHistories.Position);
                        blendedPriorHistories.WriteLong(position);
                        for (int k = 0; k < priorMultiplicity; k++)
                        {
                            blendedPriorHistories.WriteLong(lengths[k]);
                        }

                        blendedPriorHistories.Flush();
                    }

                    blendedTreeSamples.Add(runs[i].TreeSamples[samples[j]]);
                    current++;
                    progressAction("Blending character histories...", (double)current / total);
                }
            }

            if (includePriorHistories)
            {
                long pos = blendedPriorHistories.Position;

                for (int i = 0; i < finalCount; i++)
                {
                    blendedPriorHistories.WriteLong(priorOffsets[i]);
                }

                blendedPriorHistories.WriteLong(pos);
                blendedPriorHistories.WriteInt(priorMultiplicity);
                blendedPriorHistories.Flush();
                blendedPriorHistories.Seek(0, SeekOrigin.Begin);
            }

            progressAction("Saving output...", -1);

            SerializedRun tbr;

            
            if (includeDStats)
            {
                tbr = new SerializedRun(TreeNode.Parse(summaryTreeString, null), blendedHistories.ToArray(), allDStats.ToArray(), states, blendedMeanPosterior, blendedMeanPrior, summaryNodeCorresp, blendedTreeSamples.ToArray(), likModels, allPossibleStates, ageScale, null, null, treesClockLike);
            }
            else if (includePriorHistories)
            {
                tbr = new SerializedRun(TreeNode.Parse(summaryTreeString, null), blendedHistories.ToArray(), blendedPriorHistories, states, blendedMeanPosterior, blendedMeanPrior, summaryNodeCorresp, blendedTreeSamples.ToArray(), likModels, allPossibleStates, ageScale, null, null, treesClockLike);
            }
            else
            {
                tbr = new SerializedRun(TreeNode.Parse(summaryTreeString, null), blendedHistories.ToArray(), states, blendedMeanPosterior, blendedMeanPrior, summaryNodeCorresp, blendedTreeSamples.ToArray(), likModels, allPossibleStates, ageScale, treesClockLike);
            }

            tbr.Serialize(outputFile);

            for (int i = 0; i < runs.Count; i++)
            {
                runs[i].CloseStream();
            }

            if (includePriorHistories)
            {
                if (priorTempFolder != "memory")
                {
                    blendedPriorHistories.Close();
                    File.Delete(Path.Combine(priorTempFolder, prefix + ".prior"));
                }
            }
        }

        static int[] getSamples(int max, int count, Random rnd)
        {
            if (count == max)
            {
                return Utils.Utils.Range(0, max);
            }
            else if (count > max)
            {
                List<int> tbr = new List<int>(count);

                while (count - tbr.Count > max)
                {
                    tbr.AddRange(Utils.Utils.Range(0, max));
                }

                List<int> pool = new List<int>(Utils.Utils.Range(0, max));

                int remaining = count - tbr.Count;

                for (int i = 0; i < remaining; i++)
                {
                    int ind = rnd.Next(0, pool.Count);
                    tbr.Add(pool[ind]);
                    pool.RemoveAt(ind);
                }

                return tbr.ToArray();
            }
            else
            {
                List<int> tbr = new List<int>(count);

                List<int> pool = new List<int>(Utils.Utils.Range(0, max));

                int remaining = count - tbr.Count;

                for (int i = 0; i < remaining; i++)
                {
                    int ind = rnd.Next(0, pool.Count);
                    tbr.Add(pool[ind]);
                    pool.RemoveAt(ind);
                }

                return tbr.ToArray();
            }
        }
    }
}
