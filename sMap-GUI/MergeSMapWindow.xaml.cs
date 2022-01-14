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
    public class MergeSMapWindow : Window
    {
        public MergeSMapWindow()
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

        List<(SerializedRun run, string fileName)> Runs = new List<(SerializedRun, string)>();
        List<ScrollViewer> ScrollViewers = new List<ScrollViewer>();

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
                    SerializedRun run = null;

                    EventWaitHandle startHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

                    RenderingProgressWindow win = new RenderingProgressWindow(startHandle);

                    win.ProgressText = "Reading input file " + (i + 1).ToString() + " / " + fileNames.Length.ToString() + "...";

                    Thread thr = new Thread(async () =>
                    {
                        startHandle.WaitOne();

                        run = SerializedRun.Deserialize(fileNames[i]);

                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            win.Close();
                        });
                    });

                    thr.Start();

                    await win.ShowDialog(this);

                    Runs.Add((run, fileNames[i]));

                    ScrollViewer sv = new ScrollViewer() { HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled, Padding = new Thickness(0, 5, 0, 5) };
                    StackPanel sp = new StackPanel() { Orientation = Avalonia.Layout.Orientation.Horizontal };

                    for (int j = 0; j < run.AllPossibleStates[0].Split(',').Length; j++)
                    {
                        CheckBox cb = new CheckBox() { Content = j.ToString(), Tag = j, Margin = new Thickness(0, 0, 10, 0), IsChecked = true };
                        cb.Click += (s, ex) =>
                            {
                                UpdateTotalChars();
                            };
                        sp.Children.Add(cb);
                    }

                    sv.Content = sp;

                    Grid.SetRow(sv, i);
                    Grid.SetColumn(sv, 1);

                    ScrollViewers.Add(sv);

                }
                RebuildFileList();
            }
        }

        private int[][] GetCharacters()
        {
            return (from el in ScrollViewers select (from el2 in ((StackPanel)el.Content).Children where ((CheckBox)el2).IsChecked == true select (int)((CheckBox)el2).Tag).ToArray()).ToArray();
        }

        private void RebuildFileList()
        {
            int[][] characters = GetCharacters();

            Grid container = this.FindControl<Grid>("sMapFileContainer");

            container.RowDefinitions.Clear();

            container.Children.Clear();

            for (int i = 0; i < Runs.Count; i++)
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
                sv.Content = new TextBlock() { Text = System.IO.Path.GetFileName(Runs[i].fileName) };

                Grid.SetRow(sv, i);
                container.Children.Add(sv);

                ScrollViewer ud = ScrollViewers[i];
                Grid.SetRow(ud, i);
                Grid.SetColumn(ud, 1);

                container.Children.Add(ud);

                AddButton ad = new AddButton() { Type = AddButton.ButtonTypes.Remove };
                Grid.SetRow(ad, i);
                Grid.SetColumn(ad, 2);

                int j = i;

                ad.PointerPressed += (s, ex) =>
                {
                    Runs.RemoveAt(j);
                    ScrollViewers.RemoveAt(j);
                    RebuildFileList();
                    GC.Collect();
                };

                container.Children.Add(ad);
            }

            UpdateTotalChars();
        }

        private void UpdateTotalChars()
        {
            int totalChars = (from el in GetCharacters() select el.Length).Sum();

            this.FindControl<TextBlock>("TotalCharsBlock").Text = totalChars.ToString();
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            for (int i = 0; i < Runs.Count; i++)
            {
                Runs[i].run.CloseStream();
            }
        }

        private async void SaveSMapClicked(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog() { Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Name = "sMap file", Extensions = new List<string>() { "bin" } }, new FileDialogFilter() { Name = "Phytools-compatible file", Extensions = new List<string>() { "tre" } } }, Title = "Save merged sMap" };

            string result = await dialog.ShowAsync(this);

            if (!string.IsNullOrEmpty(result))
            {
                bool isPhytools = true;

                if (result.EndsWith(".bin"))
                {
                    isPhytools = false;
                }

                int[][] activeChars = GetCharacters();
                int samples = (int)this.FindControl<NumericUpDown>("FinalCountBox").Value;
                int seed = (int)this.FindControl<NumericUpDown>("SeedBox").Value;


                bool renameStates = this.FindControl<CheckBox>("RenameStatesBox").IsChecked == true;

                EventWaitHandle startHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

                PlotProgressWindow win = new PlotProgressWindow(startHandle);

                win.ProgressText = "Reading input files...";

                Thread thr = new Thread(async () =>
                {
                    startHandle.WaitOne();

                    await Merge(activeChars, samples, isPhytools ? null : result, isPhytools ? result : null, renameStates, seed, async (s, v) =>
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

        private async Task Merge(int[][] activeCharacters, int samples, string outputsMap, string outputPhytools, bool renameStates, int seed, Action<string, double> progressAction, string priorTempFolder)
        {
            SerializedRun[] runs = (from el in Runs select el.run).ToArray();

            double ageScale = -1;
            string summaryTreeString = "";
            int[][] meanNodeCorresp = null;
            LikelihoodModel[] LikelihoodModels = null;
            bool treesClockLike = false;

            for (int i = 0; i < runs.Length; i++)
            {

                if (i == 0)
                {
                    treesClockLike = runs[i].TreesClockLike;
                    summaryTreeString = runs[i].SummaryTree.ToString();
                    meanNodeCorresp = runs[i].SummaryNodeCorresp;
                    ageScale = runs[i].AgeScale;
                    LikelihoodModels = runs[i].LikelihoodModels;
                }
                else
                {
                    if (ageScale != runs[i].AgeScale)
                    {
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            MessageBox box = new MessageBox("Error", "Cannot proceed: not all runs use the same age scale!");

                            await box.ShowDialog(this);
                        });
                        return;
                    }

                    if (summaryTreeString != runs[i].SummaryTree.ToString())
                    {
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            MessageBox box = new MessageBox("Error", "The sMap runs use different summary trees!");

                            await box.ShowDialog(this);
                        });
                        return;
                    }


                    if (ageScale != runs[i].AgeScale)
                    {
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            MessageBox box = new MessageBox("Error", "The sMap runs use different tree age normalisation constants!");

                            await box.ShowDialog(this);
                        });
                        return;
                    }
                }
            }

            List<string[]> allStates = new List<string[]>();

            for (int i = 0; i < runs.Length; i++)
            {
                for (int j = 0; j < activeCharacters[i].Length; j++)
                {
                    HashSet<string> charStates = new HashSet<string>();

                    for (int k = 0; k < runs[i].AllPossibleStates.Length; k++)
                    {
                        charStates.Add(runs[i].AllPossibleStates[k].Split(',')[activeCharacters[i][j]]);
                    }

                    allStates.Add(charStates.ToArray());
                }
            }

            string[] allPossibleStates = (from el in Utils.Utils.GetCombinations(allStates.ToArray()) select Utils.Utils.StringifyArray(el)).ToArray();

            Dictionary<string, string> renamedStates = null;

            if (renameStates)
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    RenameStatesWindow rNwin = new RenameStatesWindow(allPossibleStates);
                    await rNwin.ShowDialog(this);
                    if (rNwin.Result)
                    {
                        renamedStates = rNwin.RenamedStates;
                    }
                    else
                    {
                        renameStates = false;
                    }
                });
            }


            
            progressAction("Computing priors and posteriors...", -1);

            double[][] meanPosterior;
            double[][] meanPrior;

            meanPosterior = new double[runs[0].MeanPosterior.Length][];
            meanPrior = new double[runs[0].MeanPrior.Length][];

            for (int i = 0; i < meanPosterior.Length; i++)
            {
                double[][] prior = new double[allStates.Count][];
                double[][] posterior = new double[allStates.Count][];

                int charInd = 0;

                for (int k = 0; k < runs.Length; k++)
                {
                    for (int l = 0; l < activeCharacters[k].Length; l++)
                    {
                        prior[charInd] = new double[allStates[charInd].Length];
                        posterior[charInd] = new double[allStates[charInd].Length];

                        for (int j = 0; j < runs[k].AllPossibleStates.Length; j++)
                        {
                            int ind = allStates[charInd].IndexOf(runs[k].AllPossibleStates[j].Split(',')[activeCharacters[k][l]]);
                            posterior[charInd][ind] += runs[k].MeanPosterior[i][j];
                            prior[charInd][ind] += runs[k].MeanPrior[i][j];
                        }

                        charInd++;
                    }
                }

                meanPosterior[i] = new double[allPossibleStates.Length];
                meanPrior[i] = new double[allPossibleStates.Length];

                for (int k = 0; k < allPossibleStates.Length; k++)
                {
                    string[] states = allPossibleStates[k].Split(',');
                    double statePost = 1;
                    double statePrior = 1;

                    for (int j = 0; j < states.Length; j++)
                    {
                        int ind = allStates[j].IndexOf(states[j]);
                        statePost *= posterior[j][ind];
                        statePrior *= prior[j][ind];
                    }

                    meanPosterior[i][k] = statePost;
                    meanPrior[i][k] = statePrior;
                }
            }

            string[] States = allPossibleStates;

            if (renameStates)
            {
                for (int i = 0; i < States.Length; i++)
                {
                    States[i] = renamedStates[States[i]];
                }
            }

            bool includePriorHistories = !(from el in runs select el.revision >= 2).Contains(false);

            TreeNode summaryTree = runs[0].SummaryTree;

            List<TaggedHistory> histories = new List<TaggedHistory>();
            Stream priorHistories = null;
            int priorMultiplicity = 0;
            List<long> priorOffsets = new List<long>(samples);
            string prefix = Guid.NewGuid().ToString().Replace("-", "");

            if (includePriorHistories)
            {
                priorMultiplicity = (from el in runs select el.PriorMultiplicity).Min();

                if (priorTempFolder == "memory")
                {
                    priorHistories = new MemoryStream();
                }
                else
                {
                    if (priorTempFolder == null)
                    {
                        priorTempFolder = Path.GetDirectoryName(string.IsNullOrEmpty(outputsMap) ? outputPhytools : outputsMap);
                    }

                    priorHistories = new FileStream(Path.Combine(priorTempFolder, prefix + ".prior"), FileMode.Create);
                }
            }

            TaggedHistory[][][] runsHistories = new TaggedHistory[runs.Length][][];
            Func<int, TaggedHistory[]>[][] runsPriorHistories = new Func<int, TaggedHistory[]>[runs.Length][];

            List<int> allTreeSamples = new List<int>();

            progressAction("Extracting marginal histories...", -1);

            for (int i = 0; i < runs.Length; i++)
            {
                runsHistories[i] = new TaggedHistory[activeCharacters[i].Length][];
                if (includePriorHistories)
                {
                    runsPriorHistories[i] = new Func<int, TaggedHistory[]>[activeCharacters[i].Length];
                }

                for (int j = 0; j < activeCharacters[i].Length; j++)
                {
                    runsHistories[i][j] = Utils.Simulation.GetMarginalHistory(runs[i], activeCharacters[i][j]);
                    if (includePriorHistories)
                    {
                        int localI = i;
                        int localJ = j;
                        runsPriorHistories[i][j] = new Func<int, TaggedHistory[]>(k => Utils.Simulation.GetMarginalPriorHistory(runs[localI], activeCharacters[localI][localJ], k));
                    }
                }

                allTreeSamples.AddRange(runs[i].TreeSamples);
            }

            Random rnd = new Random();

            if (seed > 0)
            {
                rnd = new Random(seed);
            }

            int[] treeSamples = new int[samples];

            for (int i = 0; i < treeSamples.Length; i++)
            {
                treeSamples[i] = allTreeSamples[rnd.Next(allTreeSamples.Count)];
            }

            progressAction("Merging histories...", -1);

            for (int i = 0; i < treeSamples.Length; i++)
            {
                TaggedHistory[] currHistories = new TaggedHistory[allStates.Count];
                TaggedHistory[][] currPriorHistories = new TaggedHistory[allStates.Count][];

                bool resampleTree = true;

                while (resampleTree)
                {
                    resampleTree = false;

                    int charInd = 0;

                    for (int k = 0; k < runs.Length; k++)
                    {
                        int[] correspHistories = (from el in Utils.Utils.Range(0, runs[k].TreeSamples.Length) where runs[k].TreeSamples[el] == treeSamples[i] select el).ToArray();

                        if (correspHistories.Length > 0)
                        {
                            int histInd = correspHistories[rnd.Next(correspHistories.Length)];
                            for (int j = 0; j < activeCharacters[k].Length; j++)
                            {
                                currHistories[charInd] = runsHistories[k][j][histInd];
                                if (includePriorHistories)
                                {
                                    currPriorHistories[charInd] = runsPriorHistories[k][j](histInd);
                                }
                                charInd++;
                            }
                        }
                        else
                        {
                            resampleTree = true;
                            break;
                        }
                    }

                    if (resampleTree)
                    {
                        treeSamples[i] = allTreeSamples[rnd.Next(allTreeSamples.Count)];
                    }
                }

                TaggedHistory mergedHistory = Simulation.MergeHistories(currHistories, LikelihoodModels[treeSamples[i]], renameStates ? renamedStates : null);
                mergedHistory.Tag = histories.Count;
                histories.Add(mergedHistory);

                if (includePriorHistories)
                {
                    long position = priorHistories.Position;
                    long[] lengths = new long[priorMultiplicity];

                    for (int j = 0; j < priorMultiplicity; j++)
                    {
                        TaggedHistory mergedPriorHistory = Simulation.MergeHistories((from el in currPriorHistories select el[j]).ToArray(), LikelihoodModels[treeSamples[i]], renameStates ? renamedStates : null);
                        mergedPriorHistory.Tag = histories.Count - 1;
                        lengths[j] = mergedPriorHistory.WriteToStream(priorHistories);
                    }

                    priorHistories.Flush();

                    priorOffsets.Add(priorHistories.Position);
                    priorHistories.WriteLong(position);
                    for (int k = 0; k < priorMultiplicity; k++)
                    {
                        priorHistories.WriteLong(lengths[k]);
                    }

                    priorHistories.Flush();
                }

                progressAction("Merging histories...", (double)(i + 1) / treeSamples.Length);
            }

            if (includePriorHistories)
            {
                long pos = priorHistories.Position;

                for (int i = 0; i < treeSamples.Length; i++)
                {
                    priorHistories.WriteLong(priorOffsets[i]);
                }

                priorHistories.WriteLong(pos);
                priorHistories.WriteInt(priorMultiplicity);
                priorHistories.Flush();
                priorHistories.Seek(0, SeekOrigin.Begin);
            }

            SerializedRun mergedRun;

            if (!includePriorHistories)
            {
                mergedRun = new SerializedRun(summaryTree, histories.ToArray(), States, meanPosterior, meanPrior, meanNodeCorresp, treeSamples, LikelihoodModels, allPossibleStates, ageScale, treesClockLike);
            }
            else
            {
                mergedRun = new SerializedRun(summaryTree, histories.ToArray(), priorHistories, States, meanPosterior, meanPrior, meanNodeCorresp, treeSamples, LikelihoodModels, allPossibleStates, ageScale, null, null, treesClockLike);
            }

            progressAction("Saving output...", -1);

            if (!string.IsNullOrEmpty(outputsMap))
            {
                mergedRun.Serialize(outputsMap);
            }

            if (!string.IsNullOrEmpty(outputPhytools))
            {
                using (StreamWriter sw = new StreamWriter(outputPhytools))
                {
                    foreach (TaggedHistory history in histories)
                    {
                        sw.WriteLine(Utils.Utils.GetSmapString(LikelihoodModels[treeSamples[history.Tag]], history.History, ageScale));
                    }
                }
            }

            if (includePriorHistories)
            {
                if (priorTempFolder != "memory")
                {
                    priorHistories.Close();
                    File.Delete(Path.Combine(priorTempFolder, prefix + ".prior"));
                }
            }
        }
    }
}
