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
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace sMap_GUI
{
    public class WizardWindow : Window
    {
        public WizardWindow()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            this.FindControl<NumericUpDown>("MaxCoVBox").Value = -1.0 / 16.0 * Math.Log⁡(1.0 - 1.0 / 3.0 * (7.0 / 3.0 - 2.0 / (2.0 * ((int)this.FindControl<NumericUpDown>("NumRunsBox").Value) - 1.0)));
            this.FindControl<NumericUpDown>("ThreadsBox").Value = System.Environment.ProcessorCount;
            this.FindControl<NumericUpDown>("ParallelMLEBox").Value = Math.Min(5, System.Environment.ProcessorCount);
            this.FindControl<TipContainer>("Tips").SetTip(TipContainer.Tips.InputDataFile);
        }

        DataMatrix data;
        List<TreeNode> trees;
        HashSet<string> namesFromTrees;
        bool treesClockLike;
        TreeNode summaryTree;
        bool dataConfirmed = false;

        CharacterDependency[][] dependencies;
        bool dependenciesConfirmed;

        CharacterDependency[][] realDependencies;
        string[][] realStates;
        Dictionary<string, Parameter>[] pis;
        Dictionary<string, Parameter>[] rates;
        bool parametersConfirmed;

        bool advancedVisible = false;

        string outputPrefix = null;

        private async void ChooseDataFileClicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog;

            if (!Program.IsMac)
            {
                dialog = new OpenFileDialog() { AllowMultiple = false, Title = "Open character state data file", Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Extensions = new List<string>() { "phy", "txt" }, Name = "Relaxed PHYLYP files" }, new FileDialogFilter() { Extensions = new List<string>() { "*" }, Name = "All files" } } };
            }
            else
            {
                dialog = new OpenFileDialog() { AllowMultiple = false, Title = "Open character state data file" };
            }


            string[] result = await dialog.ShowAsync(this);

            if (result != null && result.Length > 0)
            {
                try
                {
                    data = new DataMatrix(result[0]);
                }
                catch (Exception ex)
                {
                    MessageBox box = new MessageBox("Error!", "Error: " + ex.Message);
                    await box.ShowDialog(this);
                    data = null;
                }
            }

            this.FindControl<Button>("ViewDataButton").IsVisible = data != null;
            this.FindControl<Octagon>("CharacterDataOctagon").IsTick = data != null;

            if (data != null)
            {
                this.FindControl<StackPanel>("TreesPanel").IsVisible = true;
                this.FindControl<TipContainer>("Tips").SetTip(TipContainer.Tips.TreeFile);
            }
            else
            {
                this.FindControl<TipContainer>("Tips").SetTip(TipContainer.Tips.InputDataFile);
            }

            this.FindControl<Button>("ConfirmDataButton").IsVisible = (data != null && trees != null && (trees.Count == 1 || this.FindControl<CheckBox>("UseConsensusTreeBox").IsChecked == true || summaryTree != null));
        }

        private async void ViewDataClicked(object sender, RoutedEventArgs e)
        {
            ViewDataWindow win = new ViewDataWindow(data);
            await win.ShowDialog(this);
        }

        private async void ChooseTreeFileClicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog;

            if (!Program.IsMac)
            {
                dialog = new OpenFileDialog() { AllowMultiple = false, Title = "Open tree file", Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Extensions = Program.IsMac ? new List<string>() { "*" } : new List<string>() { "nwk", "tre", "treedist" }, Name = "NEWICK tree files" }, new FileDialogFilter() { Extensions = new List<string>() { "*" }, Name = "All files" } } };
            }
            else
            {
                dialog = new OpenFileDialog() { AllowMultiple = false, Title = "Open tree file" };
            }

            string[] result = await dialog.ShowAsync(this);

            if (result != null && result.Length > 0)
            {
                EventWaitHandle readyHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

                PlotProgressWindow win = new PlotProgressWindow(readyHandle);
                win.ProgressText = "Reading tree file...";
                int totalTrees = File.ReadLines(result[0]).Count();

                Thread thr = new Thread(async () =>
                {
                    readyHandle.WaitOne();

                    try
                    {
                        trees = new List<TreeNode>();
                        treesClockLike = true;

                        namesFromTrees = new HashSet<string>();

                        using (StreamReader sr = new StreamReader(result[0]))
                        {
                            while (!sr.EndOfStream)
                            {
                                string line = sr.ReadLine();
                                if (!string.IsNullOrEmpty(line))
                                {
                                    TreeNode tree = TreeNode.Parse(line, null);
                                    tree.SortNodes(false);
                                    tree.Length = -1;

                                    if (namesFromTrees.Count == 0)
                                    {
                                        foreach (string s in tree.GetLeafNames())
                                        {
                                            namesFromTrees.Add(s);
                                        }
                                    }
                                    else
                                    {
                                        List<string> treeLeaveNames = tree.GetLeafNames();
                                        foreach (string s in treeLeaveNames)
                                        {
                                            if (!namesFromTrees.Contains(s))
                                            {
                                                throw new Exception("The trees contain different taxa!");
                                            }
                                        }

                                        foreach (string s in namesFromTrees)
                                        {
                                            if (!treeLeaveNames.Contains(s))
                                            {
                                                throw new Exception("The trees contain different taxa!");
                                            }
                                        }
                                    }

                                    trees.Add(tree);
                                    if (!tree.IsClocklike())
                                    {
                                        treesClockLike = false;
                                    }
                                }

                                await Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    win.Progress = (double)trees.Count / (double)totalTrees;
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            MessageBox box = new MessageBox("Error!", "Error: " + ex.Message);
                            await box.ShowDialog(this);
                        });

                        trees = null;
                    }

                    await Dispatcher.UIThread.InvokeAsync(() =>
                   {
                       win.Close();
                   });
                });

                thr.Start();

                await win.ShowDialog(this);
            }

            this.FindControl<Button>("ViewTreesButton").IsVisible = trees != null;

            this.FindControl<Octagon>("TreesOctagon").IsTick = trees != null;

            if (trees != null)
            {
                this.FindControl<StackPanel>("SummaryTreePanel").IsVisible = trees.Count > 1;
                if (trees.Count > 1)
                {
                    this.FindControl<TipContainer>("Tips").SetTip(TipContainer.Tips.SummaryTree);
                }
            }

            this.FindControl<Button>("ConfirmDataButton").IsVisible = (data != null && trees != null && (trees.Count == 1 || this.FindControl<CheckBox>("UseConsensusTreeBox").IsChecked == true || summaryTree != null));
        }

        private async void ViewTreesClicked(object sender, RoutedEventArgs e)
        {
            ViewTreeWindow win = new ViewTreeWindow(trees) { Header = trees.Count > 1 ? "Trees" : "Tree" };

            await win.ShowDialog(this);
        }

        private void UseConsensusClicked(object sender, RoutedEventArgs e)
        {
            if (this.FindControl<CheckBox>("UseConsensusTreeBox").IsChecked == true)
            {
                this.FindControl<StackPanel>("ConsensusOptions").IsVisible = true;
                this.FindControl<NumericUpDown>("ConsensusThreshold").IsVisible = true;
                this.FindControl<Button>("ChooseSummaryTreeFileButton").IsVisible = false;
                this.FindControl<Button>("ViewSummaryTreeButton").IsVisible = true;
                this.FindControl<Octagon>("SummaryTreeOctagon").IsTick = true;
            }
            else
            {
                this.FindControl<StackPanel>("ConsensusOptions").IsVisible = false;
                this.FindControl<NumericUpDown>("ConsensusThreshold").IsVisible = false;
                this.FindControl<Button>("ChooseSummaryTreeFileButton").IsVisible = true;
                this.FindControl<Button>("ViewSummaryTreeButton").IsVisible = false;
                summaryTree = null;
                this.FindControl<Octagon>("SummaryTreeOctagon").IsTick = false;
            }

            this.FindControl<Button>("ConfirmDataButton").IsVisible = (data != null && trees != null && (trees.Count == 1 || this.FindControl<CheckBox>("UseConsensusTreeBox").IsChecked == true || summaryTree != null));
        }


        private async void ViewSummaryTreeClicked(object sender, RoutedEventArgs e)
        {
            if (this.FindControl<CheckBox>("UseConsensusTreeBox").IsChecked == true && !dataConfirmed)
            {
                if (trees != null && trees.Count > 1)
                {
                    EventWaitHandle consensusReadyHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

                    RenderingProgressWindow consensusWin = new RenderingProgressWindow(consensusReadyHandle) { ProgressText = "Computing consensus..." };

                    bool useMedian = this.FindControl<ComboBox>("ConsensusBranchLengths").SelectedIndex == 0;

                    double threshold = this.FindControl<NumericUpDown>("ConsensusThreshold").Value;

                    Thread conThread = new Thread(() =>
                    {
                        consensusReadyHandle.WaitOne();

                        summaryTree = trees.GetRootedTreeConsensus(treesClockLike, useMedian, threshold);

                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            consensusWin.Close();
                        });

                    });

                    conThread.Start();

                    await consensusWin.ShowDialog(this);

                    ViewTreeWindow win = new ViewTreeWindow(summaryTree) { Header = "Summary tree" };

                    await win.ShowDialog(this);
                }
            }
            else
            {
                ViewTreeWindow win = new ViewTreeWindow(summaryTree) { Header = "Summary tree" };

                await win.ShowDialog(this);
            }
        }

        private async void ChooseSummaryTreeFileClicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog;

            if (!Program.IsMac)
            {
                dialog = new OpenFileDialog() { AllowMultiple = false, Title = "Open tree file", Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Extensions = Program.IsMac ? new List<string>() { "*" } : new List<string>() { "nwk", "tre" }, Name = "NEWICK tree files" }, new FileDialogFilter() { Extensions = new List<string>() { "*" }, Name = "All files" } } };
            }
            else
            {
                dialog = new OpenFileDialog() { AllowMultiple = false, Title = "Open tree file" };
            }


            string[] result = await dialog.ShowAsync(this);

            if (result != null && result.Length > 0)
            {
                try
                {
                    string treeText = File.ReadAllText(result[0]);
                    summaryTree = TreeNode.Parse(treeText, null);

                    List<string> summaryTreeLeaveNames = summaryTree.GetLeafNames();

                    foreach (string s in summaryTreeLeaveNames)
                    {
                        if (!namesFromTrees.Contains(s))
                        {
                            throw new Exception("The summary tree contains different taxa than the trees!");
                        }
                    }

                    foreach (string s in namesFromTrees)
                    {
                        if (!summaryTreeLeaveNames.Contains(s))
                        {
                            throw new Exception("The summary tree contains different taxa than the trees!");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox box = new MessageBox("Error!", "Error: " + ex.Message);
                    await box.ShowDialog(this);
                    summaryTree = null;
                }
            }

            this.FindControl<Octagon>("SummaryTreeOctagon").IsTick = summaryTree != null;
            this.FindControl<Button>("ViewSummaryTreeButton").IsVisible = summaryTree != null;

            this.FindControl<Button>("ConfirmDataButton").IsVisible = (data != null && trees != null && (trees.Count == 1 || this.FindControl<CheckBox>("UseConsensusTreeBox").IsChecked == true || summaryTree != null));
        }

        private async void ConfirmDataClicked(object sender, RoutedEventArgs e)
        {
            if (data == null)
            {
                await new MessageBox("Attention!", "Invalid data selected!").ShowDialog(this);
                return;
            }

            if (trees == null)
            {
                await new MessageBox("Attention!", "Invalid trees selected!").ShowDialog(this);
                return;
            }

            if (trees.Count == 1)
            {
                summaryTree = trees[0].Clone();
            }
            else if (this.FindControl<CheckBox>("UseConsensusTreeBox").IsChecked == true)
            {
                EventWaitHandle consensusReadyHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

                RenderingProgressWindow consensusWin = new RenderingProgressWindow(consensusReadyHandle) { ProgressText = "Computing consensus..." };

                bool useMedian = this.FindControl<ComboBox>("ConsensusBranchLengths").SelectedIndex == 0;

                double consensusThreshold = this.FindControl<NumericUpDown>("ConsensusThreshold").Value;

                Thread conThread = new Thread(() =>
                {
                    consensusReadyHandle.WaitOne();

                    summaryTree = trees.GetRootedTreeConsensus(treesClockLike, useMedian, consensusThreshold);

                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        consensusWin.Close();
                    });

                });

                conThread.Start();

                await consensusWin.ShowDialog(this);
            }

            if (summaryTree == null)
            {
                await new MessageBox("Attention!", "Invalid summary tree selected!").ShowDialog(this);
                return;
            }

            int missingInData = 0;
            int missingInTree = 0;

            foreach (string s in namesFromTrees)
            {
                if (!data.Data.ContainsKey(s))
                {
                    missingInData++;
                }
            }

            foreach (string s in data.Data.Keys)
            {
                if (!namesFromTrees.Contains(s))
                {
                    missingInTree++;
                }
            }

            if (missingInData > 0)
            {
                await new MessageBox("Attention!", "Missing data for " + missingInData.ToString() + (missingInData == 1 ? " taxon" : " taxa") + " from the tree(s)!").ShowDialog(this);
                return;
            }
            else if (missingInTree > 0)
            {
                MessageBox box = new MessageBox("Attention!", "There is data for " + missingInTree.ToString() + (missingInTree == 1 ? " taxon that is" : " taxa that are") + " not present in the tree(s). This will NOT cause problems to the analysis, but it may indicate a problem with your files. Do you wish to proceed?", MessageBox.MessageBoxButtonTypes.YesNo);
                await box.ShowDialog(this);
                if (box.Result != MessageBox.Results.Yes)
                {
                    return;
                }
            }


            this.FindControl<Button>("ChooseDataFileButton").IsVisible = false;
            this.FindControl<Button>("ChooseTreeFileButton").IsVisible = false;
            this.FindControl<Button>("ChooseSummaryTreeFileButton").IsVisible = false;
            this.FindControl<CheckBox>("UseConsensusTreeBox").IsEnabled = false;
            this.FindControl<NumericUpDown>("ConsensusThreshold").IsEnabled = false;
            this.FindControl<ComboBox>("ConsensusBranchLengths").IsEnabled = false;
            this.FindControl<Button>("ConfirmDataButton").IsVisible = false;

            dataConfirmed = true;
            dependencies = Parsing.GetDefaultDependencies(data.States);


            string depName = Utils.Utils.GetDependencyName(dependencies);

            this.FindControl<Viewbox>("DefineModelHeader").IsVisible = true;

            if (data.States.Length > 1)
            {
                this.FindControl<StackPanel>("DependencyModelPanel").IsVisible = true;
                this.FindControl<Button>("ConfirmDependenciesButton").IsVisible = true;
                this.FindControl<TextBlock>("DependencyNameBlock").Text = depName;
                this.FindControl<TipContainer>("Tips").SetTip(TipContainer.Tips.DependencyModel);
            }
            else
            {
                dependenciesConfirmed = true;
                this.FindControl<Button>("ChooseDependencyFileButton").IsVisible = false;
                this.FindControl<Button>("EditDependencyButton").Content = "View...";
                this.FindControl<StackPanel>("DependencyModelPanel").IsVisible = true;
                this.FindControl<TextBlock>("DependencyNameBlock").Text = depName;

                this.FindControl<TipContainer>("Tips").SetTip(TipContainer.Tips.PisRatesCondProbs);

                (CharacterDependency[][] dependencies, Dictionary<string, Parameter>[] pi, Dictionary<string, Parameter>[] rates, DataMatrix data) deps = Utils.Utils.GetRealDependencies(dependencies, data);

                realStates = deps.data.States;
                realDependencies = deps.dependencies;
                pis = deps.pi;
                rates = deps.rates;

                string piName = Utils.Utils.GetPisName(realDependencies, pis);
                this.FindControl<TextBlock>("PisNameBlock").Text = piName;

                string ratesName = Utils.Utils.GetRatesName(realDependencies, rates);
                this.FindControl<TextBlock>("RatesNameBlock").Text = ratesName;

                this.FindControl<StackPanel>("DefineParametersHeader").IsVisible = true;
                this.FindControl<StackPanel>("PisModelPanel").IsVisible = true;
                this.FindControl<StackPanel>("RatesModelPanel").IsVisible = true;
                this.FindControl<Button>("ConfirmParametersButton").IsVisible = true;

                bool hasConditioned = false;

                for (int i = 0; i < realDependencies.Length; i++)
                {
                    for (int j = 0; j < realDependencies[i].Length; j++)
                    {
                        if (realDependencies[i][j].Type == CharacterDependency.Types.Conditioned)
                        {
                            hasConditioned = true;
                        }
                    }
                }

                if (hasConditioned)
                {
                    this.FindControl<StackPanel>("CondProbsModelPanel").IsVisible = true;
                    string condName = Utils.Utils.GetCondProbsName(realDependencies);
                    this.FindControl<TextBlock>("CondProbsNameBlock").Text = condName;
                }
            }

            new Thread(() =>
            {
                Thread.Sleep(10);
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<ScrollViewer>("MainScrollViewer").ScrollToBottom();
                });
            }).Start();
        }

        private void ConfirmDependenciesClicked(object sender, RoutedEventArgs e)
        {
            dependenciesConfirmed = true;
            this.FindControl<Button>("ChooseDependencyFileButton").IsVisible = false;
            this.FindControl<Button>("EditDependencyButton").Content = "View...";
            this.FindControl<Button>("ConfirmDependenciesButton").IsVisible = false;

            string depSource = Utils.Utils.GetDependencySource(dependencies, true, true, true);

            Stream stream = new MemoryStream();
            using (StreamWriter sw = new StreamWriter(stream))
            {
                sw.Write("#NEXUS\n");
                sw.Write(depSource);
                sw.Flush();

                stream.Position = 0;
                using (StreamReader sr = new StreamReader(stream))
                {
                    dependencies = Parsing.ParseDependencies(sr, data.States, new ThreadSafeRandom());
                }
            }

            (CharacterDependency[][] dependencies, Dictionary<string, Parameter>[] pi, Dictionary<string, Parameter>[] rates, DataMatrix data) deps = Utils.Utils.GetRealDependencies(dependencies, data);

            realStates = deps.data.States;
            realDependencies = deps.dependencies;
            pis = deps.pi;
            rates = deps.rates;

            string piName = Utils.Utils.GetPisName(realDependencies, pis);
            this.FindControl<TextBlock>("PisNameBlock").Text = piName;

            string ratesName = Utils.Utils.GetRatesName(realDependencies, rates);
            this.FindControl<TextBlock>("RatesNameBlock").Text = ratesName;

            this.FindControl<StackPanel>("DefineParametersHeader").IsVisible = true;
            this.FindControl<StackPanel>("PisModelPanel").IsVisible = true;
            this.FindControl<StackPanel>("RatesModelPanel").IsVisible = true;
            this.FindControl<Button>("ConfirmParametersButton").IsVisible = true;

            bool hasConditioned = false;

            for (int i = 0; i < realDependencies.Length; i++)
            {
                for (int j = 0; j < realDependencies[i].Length; j++)
                {
                    if (realDependencies[i][j].Type == CharacterDependency.Types.Conditioned)
                    {
                        hasConditioned = true;
                    }
                }
            }

            this.FindControl<TipContainer>("Tips").SetTip(TipContainer.Tips.PisRatesCondProbs);

            if (hasConditioned)
            {
                this.FindControl<StackPanel>("CondProbsModelPanel").IsVisible = true;
                string condName = Utils.Utils.GetCondProbsName(realDependencies);
                this.FindControl<TextBlock>("CondProbsNameBlock").Text = condName;
            }

            if (AreEstimatedPisAvailable())
            {
                this.FindControl<CheckBox>("EstimatedPisBox").IsEnabled = true;
            }
            else
            {
                this.FindControl<CheckBox>("EstimatedPisBox").IsEnabled = false;
                this.FindControl<CheckBox>("EstimatedPisBox").IsChecked = false;
            }


            new Thread(() =>
            {
                Thread.Sleep(10);
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<ScrollViewer>("MainScrollViewer").ScrollToBottom();
                });
            }).Start();
        }

        private bool AreEstimatedPisAvailable()
        {
            for (int i = 0; i < realDependencies.Length; i++)
            {
                if (realDependencies[i].Length != 1 || realDependencies[i][0].Type != CharacterDependency.Types.Independent)
                {
                    return false;
                }
            }

            for (int i = 0; i < rates.Length; i++)
            {
                foreach (KeyValuePair<string, Parameter> rate in rates[i])
                {
                    if (rate.Value.Action == Parameter.ParameterAction.ML)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private async void LoadParametersClicked(object sender, RoutedEventArgs e)
        {
            LoadParametersFromRunWindow win = new LoadParametersFromRunWindow(realDependencies, rates, pis);

            await win.ShowDialog(this);

            string ratesName = Utils.Utils.GetRatesName(realDependencies, rates);
            this.FindControl<TextBlock>("RatesNameBlock").Text = ratesName;
            string piName = Utils.Utils.GetPisName(realDependencies, pis);
            this.FindControl<TextBlock>("PisNameBlock").Text = piName;
            string condName = Utils.Utils.GetCondProbsName(realDependencies);
            this.FindControl<TextBlock>("CondProbsNameBlock").Text = condName;

            if (AreEstimatedPisAvailable())
            {
                this.FindControl<CheckBox>("EstimatedPisBox").IsEnabled = true;
            }
            else
            {
                this.FindControl<CheckBox>("EstimatedPisBox").IsEnabled = false;
                this.FindControl<CheckBox>("EstimatedPisBox").IsChecked = false;
            }
        }

        private async void EditDependencyClicked(object sender, RoutedEventArgs e)
        {
            ViewDependenciesWindow win = new ViewDependenciesWindow(dependencies, data.States, !dependenciesConfirmed) { HeaderBrush = new SolidColorBrush(Color.FromArgb(255, 63, 72, 204)) };

            await win.ShowDialog(this);

            dependencies = win.Dependencies;

            string depName = Utils.Utils.GetDependencyName(dependencies);
            this.FindControl<TextBlock>("DependencyNameBlock").Text = depName;
        }

        private async void ChooseDependencyFileClicked(object sender, RoutedEventArgs e)
        {

            OpenFileDialog dialog;

            if (!Program.IsMac)
            {
                dialog = new OpenFileDialog() { AllowMultiple = false, Title = "Open dependency file", Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Extensions = Program.IsMac ? new List<string>() { "*" } : new List<string>() { "nex" }, Name = "NEXUS files" }, new FileDialogFilter() { Extensions = new List<string>() { "*" }, Name = "All files" } } };
            }
            else
            {
                dialog = new OpenFileDialog() { AllowMultiple = false, Title = "Open dependency file" };
            }

            string[] result = await dialog.ShowAsync(this);

            if (result != null && result.Length > 0)
            {
                CharacterDependency[][] prevDep = dependencies;

                try
                {
                    dependencies = Parsing.ParseDependencies(result[0], data.States, new ThreadSafeRandom());

                }
                catch (Exception ex)
                {
                    await new MessageBox("Error!", "Error: " + ex.Message).ShowDialog(this);
                    dependencies = prevDep;
                }

                string depName = Utils.Utils.GetDependencyName(dependencies);
                this.FindControl<TextBlock>("DependencyNameBlock").Text = depName;
            }
        }

        private async void EditPisClicked(object sender, RoutedEventArgs e)
        {
            ViewPiWindow win = new ViewPiWindow(realStates, realDependencies, pis, false, !parametersConfirmed);
            win.HeaderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 162, 232));

            await win.ShowDialog(this);

            pis = win.Pi;
            string piName = Utils.Utils.GetPisName(realDependencies, pis);
            this.FindControl<TextBlock>("PisNameBlock").Text = piName;

            if (AreEstimatedPisAvailable())
            {
                this.FindControl<CheckBox>("EstimatedPisBox").IsEnabled = true;
            }
            else
            {
                this.FindControl<CheckBox>("EstimatedPisBox").IsEnabled = false;
                this.FindControl<CheckBox>("EstimatedPisBox").IsChecked = false;
            }
        }

        private async void EditCondProbsClicked(object sender, RoutedEventArgs e)
        {
            ViewPiWindow win = new ViewPiWindow(realStates, realDependencies, pis, true, !parametersConfirmed);
            win.HeaderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 162, 232));

            win.Header = "Conditioned probabilities";
            win.Title = "View conditioned probabilities";

            await win.ShowDialog(this);

            string condName = Utils.Utils.GetCondProbsName(realDependencies);
            this.FindControl<TextBlock>("CondProbsNameBlock").Text = condName;

            if (AreEstimatedPisAvailable())
            {
                this.FindControl<CheckBox>("EstimatedPisBox").IsEnabled = true;
            }
            else
            {
                this.FindControl<CheckBox>("EstimatedPisBox").IsEnabled = false;
                this.FindControl<CheckBox>("EstimatedPisBox").IsChecked = false;
            }
        }

        private async void ChoosePiFileClicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog;

            if (!Program.IsMac)
            {
                dialog = new OpenFileDialog() { AllowMultiple = false, Title = "Open pis file", Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Extensions = Program.IsMac ? new List<string>() { "*" } : new List<string>() { "nex" }, Name = "NEXUS files" }, new FileDialogFilter() { Extensions = new List<string>() { "*" }, Name = "All files" } } };
            }
            else
            {
                dialog = new OpenFileDialog() { AllowMultiple = false, Title = "Open pis file" };
            }


            string[] result = await dialog.ShowAsync(this);

            if (result != null && result.Length > 0)
            {
                try
                {
                    Dictionary<string, Parameter>[] inputPis = Parsing.ParsePiFile(result[0], data.States, new ThreadSafeRandom());

                    (CharacterDependency[][] dependencies, Dictionary<string, Parameter>[] pi, Dictionary<string, Parameter>[] rates, DataMatrix data) tempDeps = Utils.Utils.GetRealDependencies(dependencies, data, inputPis);

                    pis = tempDeps.pi;

                }
                catch (Exception ex)
                {
                    await new MessageBox("Error!", "Error: " + ex.Message).ShowDialog(this);
                }

                string piName = Utils.Utils.GetPisName(realDependencies, pis);
                this.FindControl<TextBlock>("PisNameBlock").Text = piName;
            }

            if (AreEstimatedPisAvailable())
            {
                this.FindControl<CheckBox>("EstimatedPisBox").IsEnabled = true;
            }
            else
            {
                this.FindControl<CheckBox>("EstimatedPisBox").IsEnabled = false;
                this.FindControl<CheckBox>("EstimatedPisBox").IsChecked = false;
            }
        }


        private async void EditRatesClicked(object sender, RoutedEventArgs e)
        {
            ViewRatesWindow win = new ViewRatesWindow(realStates, realDependencies, rates, new SolidColorBrush(Color.FromArgb(255, 0, 162, 232)), !parametersConfirmed);
            await win.ShowDialog(this);

            rates = win.Rates;
            string ratesName = Utils.Utils.GetRatesName(realDependencies, rates);
            this.FindControl<TextBlock>("RatesNameBlock").Text = ratesName;

            if (AreEstimatedPisAvailable())
            {
                this.FindControl<CheckBox>("EstimatedPisBox").IsEnabled = true;
            }
            else
            {
                this.FindControl<CheckBox>("EstimatedPisBox").IsEnabled = false;
                this.FindControl<CheckBox>("EstimatedPisBox").IsChecked = false;
            }
        }

        private async void ChooseRatesFileClicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog;

            if (!Program.IsMac)
            {
                dialog = new OpenFileDialog() { AllowMultiple = false, Title = "Open rates file", Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Extensions = Program.IsMac ? new List<string>() { "*" } : new List<string>() { "nex" }, Name = "NEXUS files" }, new FileDialogFilter() { Extensions = new List<string>() { "*" }, Name = "All files" } } };
            }
            else
            {
                dialog = new OpenFileDialog() { AllowMultiple = false, Title = "Open rates file" };
            }

            string[] result = await dialog.ShowAsync(this);

            if (result != null && result.Length > 0)
            {
                try
                {
                    Dictionary<string, Parameter>[] inputRates = Parsing.ParseRateFile(result[0], data.States, new ThreadSafeRandom());

                    (CharacterDependency[][] dependencies, Dictionary<string, Parameter>[] pi, Dictionary<string, Parameter>[] rates, DataMatrix data) tempDeps = Utils.Utils.GetRealDependencies(dependencies, data, null, inputRates);

                    rates = tempDeps.rates;

                }
                catch (Exception ex)
                {
                    await new MessageBox("Error!", "Error: " + ex.Message).ShowDialog(this);
                }

                string ratesName = Utils.Utils.GetRatesName(realDependencies, rates);
                this.FindControl<TextBlock>("RatesNameBlock").Text = ratesName;
            }

            if (AreEstimatedPisAvailable())
            {
                this.FindControl<CheckBox>("EstimatedPisBox").IsEnabled = true;
            }
            else
            {
                this.FindControl<CheckBox>("EstimatedPisBox").IsEnabled = false;
                this.FindControl<CheckBox>("EstimatedPisBox").IsChecked = false;
            }
        }

        private void ConfirmParametersClicked(object sender, RoutedEventArgs e)
        {
            parametersConfirmed = true;
            this.FindControl<Button>("EditPisButton").Content = "View...";
            this.FindControl<Button>("ChoosePisFileButton").IsVisible = false;
            this.FindControl<CheckBox>("EstimatedPisBox").IsEnabled = false;

            this.FindControl<Button>("EditCondProbsButton").Content = "View...";

            this.FindControl<Button>("EditRatesButton").Content = "View...";
            this.FindControl<Button>("ChooseRatesFileButton").IsVisible = false;

            this.FindControl<Button>("ConfirmParametersButton").IsVisible = false;

            this.FindControl<StackPanel>("AdvancedSettingsHeader").IsVisible = true;

            this.FindControl<Viewbox>("RunAnalysisHeader").IsVisible = true;

            this.FindControl<StackPanel>("OutputPrefixPanel").IsVisible = true;

            this.FindControl<StackPanel>("SaveButtonsPanel").IsVisible = true;

            this.FindControl<Button>("RunAnalysisButton").IsVisible = true;

            for (int i = 0; i < dependencies.Length; i++)
            {
                for (int j = 0; j < dependencies[i].Length; j++)
                {
                    if (dependencies[i][j].Type == CharacterDependency.Types.Conditioned)
                    {
                        dependencies[i][j].ConditionedProbabilities = realDependencies[i][j].ConditionedProbabilities;
                    }
                    else if (dependencies[i][j].Type == CharacterDependency.Types.Dependent)
                    {
                        Dictionary<string, Parameter> newParams = new Dictionary<string, Parameter>();

                        foreach (KeyValuePair<string, Parameter> kvp in pis[realDependencies[i][j].Index])
                        {
                            newParams.Add(kvp.Key, kvp.Value);
                        }

                        foreach (KeyValuePair<string, Parameter> kvp in rates[realDependencies[i][j].Index])
                        {
                            newParams.Add(kvp.Key, kvp.Value);
                        }

                        dependencies[i][j].ConditionedProbabilities = newParams;
                    }
                }
            }

            this.FindControl<TipContainer>("Tips").SetTip(TipContainer.Tips.RunningAnalysis);

            new Thread(() =>
            {
                Thread.Sleep(10);
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<ScrollViewer>("MainScrollViewer").ScrollToBottom();
                });
            }).Start();
        }

        private void SettingsButtonClicked(object sender, RoutedEventArgs e)
        {

            showHideVeryAdvancedChildren(this, veryAdvancedVisible);

            if (!advancedVisible)
            {
                advancedVisible = true;
                this.FindControl<TextBlock>("SettingsActionBlock").Text = "Hide";
                this.FindControl<Avalonia.Controls.Shapes.Path>("ShowSettingsPath").IsVisible = false;
                this.FindControl<Avalonia.Controls.Shapes.Path>("HideSettingsPath").IsVisible = true;

                this.FindControl<StackPanel>("AdvancedSettingsPanel").IsVisible = true;
                this.FindControl<Button>("VeryAdvancedSettingsButton").IsVisible = true;
            }
            else
            {
                advancedVisible = false;
                this.FindControl<TextBlock>("SettingsActionBlock").Text = "Show";
                this.FindControl<Avalonia.Controls.Shapes.Path>("ShowSettingsPath").IsVisible = true;
                this.FindControl<Avalonia.Controls.Shapes.Path>("HideSettingsPath").IsVisible = false;

                this.FindControl<StackPanel>("AdvancedSettingsPanel").IsVisible = false;
                this.FindControl<Button>("VeryAdvancedSettingsButton").IsVisible = false;
            }

        }

        bool veryAdvancedVisible = false;

        private void VeryAdvancedSettingsButtonClicked(object sender, RoutedEventArgs e)
        {
            if (!veryAdvancedVisible)
            {
                veryAdvancedVisible = true;
                this.FindControl<TextBlock>("VeryAdvancedSettingsActionBlock").Text = "Show less";
                this.FindControl<Avalonia.Controls.Shapes.Path>("ShowVeryAdvancedSettingsPath").IsVisible = false;
                this.FindControl<Avalonia.Controls.Shapes.Path>("HideVeryAdvancedSettingsPath").IsVisible = true;

                showHideVeryAdvancedChildren(this, true);
            }
            else
            {
                veryAdvancedVisible = false;
                this.FindControl<TextBlock>("VeryAdvancedSettingsActionBlock").Text = "Show more";
                this.FindControl<Avalonia.Controls.Shapes.Path>("ShowVeryAdvancedSettingsPath").IsVisible = true;
                this.FindControl<Avalonia.Controls.Shapes.Path>("HideVeryAdvancedSettingsPath").IsVisible = false;

                showHideVeryAdvancedChildren(this, false);
            }

        }

        bool mcmcOptionsVisible = false;

        private static void showHideVeryAdvancedChildren(Control control, bool isVisible)
        {
            if (control is ContentControl && ((ContentControl)control).Content is Control)
            {
                Control child = (Control)((ContentControl)control).Content;
                if (child.Classes.Contains("VeryAdvanced"))
                {
                    child.IsVisible = isVisible;
                }
                showHideVeryAdvancedChildren(child, isVisible);
            }
            else if (control is Panel)
            {
                foreach (Control ctrl in ((Panel)control).Children)
                {
                    if (ctrl.Classes.Contains("VeryAdvanced"))
                    {
                        ctrl.IsVisible = isVisible;
                    }
                    showHideVeryAdvancedChildren(ctrl, isVisible);
                }
            }
            else if (control is Decorator)
            {
                Control child = (Control)((Decorator)control).Child;
                if (child.Classes.Contains("VeryAdvanced"))
                {
                    child.IsVisible = isVisible;
                }
                showHideVeryAdvancedChildren(child, isVisible);
            }
        }

        private void MCMCOptionsShowClicked(object sender, RoutedEventArgs e)
        {
            if (!mcmcOptionsVisible)
            {
                mcmcOptionsVisible = true;
                this.FindControl<TextBlock>("MCMCOptionsActionBlock").Text = "Hide";
                this.FindControl<Avalonia.Controls.Shapes.Path>("ShowMCMCOptionsPath").IsVisible = false;
                this.FindControl<Avalonia.Controls.Shapes.Path>("HideMCMCOptionsPath").IsVisible = true;

                this.FindControl<StackPanel>("MCMCOptionsPanel").IsVisible = true;
            }
            else
            {
                mcmcOptionsVisible = false;
                this.FindControl<TextBlock>("MCMCOptionsActionBlock").Text = "Show";
                this.FindControl<Avalonia.Controls.Shapes.Path>("ShowMCMCOptionsPath").IsVisible = true;
                this.FindControl<Avalonia.Controls.Shapes.Path>("HideMCMCOptionsPath").IsVisible = false;

                this.FindControl<StackPanel>("MCMCOptionsPanel").IsVisible = false;
            }
        }

        private async void EditMLStrategyClicked(object sender, RoutedEventArgs e)
        {
            EditMLStrategyWindow window = new EditMLStrategyWindow(this.FindControl<TextBox>("MLStrategy").Text);

            await window.ShowDialog(this);

            if (!string.IsNullOrEmpty(window.result))
            {
                this.FindControl<TextBox>("MLStrategy").Text = window.result;
            }
        }

        private async void BrowsePreSampledParametersClicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog;

            if (!Program.IsMac)
            {
                dialog = new OpenFileDialog()
                {
                    Title = "Choose model file",
                    AllowMultiple = true,
                    Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Name = "All files", Extensions = new List<string>() { "*" } } }
                };
            }
            else
            {

                dialog = new OpenFileDialog()
                {
                    Title = "Choose model file",
                    AllowMultiple = true,
                };
            }

            string[] result = await dialog.ShowAsync(this);

            if (result != null && result.Length > 0)
            {
                this.FindControl<TextBox>("ParameterFileBox").Text = (from el in result select MainWindow.GetRelativePath(el, Directory.GetCurrentDirectory())).Aggregate((a, b) => a + "," + b);
            }
        }



        private void UseTemporaryFolderClicked(object sender, RoutedEventArgs e)
        {
            if (this.FindControl<CheckBox>("UseTempOutputBox").IsChecked == true)
            {
                this.FindControl<Button>("ChooseOutputFolderButton").IsVisible = false;
                this.FindControl<Octagon>("OutputOctagon").IsTick = true;
            }
            else
            {
                this.FindControl<Button>("ChooseOutputFolderButton").IsVisible = true;
                outputPrefix = null;
                this.FindControl<Octagon>("OutputOctagon").IsTick = false;
            }

            this.FindControl<Button>("RunAnalysisButton").IsVisible = (this.FindControl<CheckBox>("UseTempOutputBox").IsChecked == true || !string.IsNullOrEmpty(outputPrefix));
        }

        private async void ChooseOutputPrefixClicked(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog()
            {
                Title = "Choose output prefix",
                Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Name = "All files", Extensions = new List<string>() { "*" } } },
                DefaultExtension = ""
            };

            string result = await dialog.ShowAsync(this);

            if (!string.IsNullOrEmpty(result))
            {
                outputPrefix = MainWindow.GetRelativePath(result, Directory.GetCurrentDirectory());
            }

            this.FindControl<Octagon>("OutputOctagon").IsTick = !string.IsNullOrEmpty(outputPrefix);
            this.FindControl<Button>("RunAnalysisButton").IsVisible = (this.FindControl<CheckBox>("UseTempOutputBox").IsChecked == true || !string.IsNullOrEmpty(outputPrefix));
        }

        private async void SaveSummaryTreeClicked(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog()
            {
                Title = "Save summary tree",
                Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Name = "NEWICK tree files", Extensions = new List<string>() { "nwk", "tre" } }, new FileDialogFilter() { Name = "All files", Extensions = new List<string>() { "*" } } },
                DefaultExtension = "tre"
            };

            string result = await dialog.ShowAsync(this);

            if (!string.IsNullOrEmpty(result))
            {
                string tree = summaryTree.ToString(true);
                File.WriteAllText(result, tree);
            }
        }

        private async void SaveModelFileClicked(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog()
            {
                Title = "Save model file",
                Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Name = "NEXUS files", Extensions = new List<string>() { "nex" } }, new FileDialogFilter() { Name = "All files", Extensions = new List<string>() { "*" } } },
                DefaultExtension = "nex"
            };

            string result = await dialog.ShowAsync(this);

            if (!string.IsNullOrEmpty(result))
            {
                string model = GetModel();

                File.WriteAllText(result, model);
            }
        }

        string GetModel()
        {
            string model = "#NEXUS\n\n";
            model += Utils.Utils.GetDependencySource(dependencies, true, true, true);
            model += "\n\n";

            model += Utils.Utils.GetOriginalPisSource(realDependencies, pis);

            model += "\n\n";

            model += Utils.Utils.GetOriginalRatesSource(realDependencies, rates);

            return model;
        }

        private async void SaveAnalysisArchiveClicked(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(this.FindControl<TextBox>("ParameterFileBox").Text) && !(from el in this.FindControl<TextBox>("ParameterFileBox").Text.Split(',') select File.Exists(el)).Aggregate((a, b) => a && b))
            {
                await new MessageBox("Attention!", "The specified parameter file(s) does not exist!").ShowDialog(this);
                return;
            }


            SaveFileDialog dialog = new SaveFileDialog()
            {
                Title = "Save analysis archive",
                Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Name = "ZIP archives", Extensions = new List<string>() { "zip" } }, new FileDialogFilter() { Name = "All files", Extensions = new List<string>() { "*" } } },
                DefaultExtension = "zip"
            };

            string result = await dialog.ShowAsync(this);

            if (!string.IsNullOrEmpty(result))
            {
                string tempDir = Path.GetTempFileName();

                File.Delete(tempDir);

                Directory.CreateDirectory(tempDir);

                Directory.CreateDirectory(Path.Combine(tempDir, "Data"));

                File.WriteAllText(Path.Combine(tempDir, "Data", "Data.txt"), data.ToString());

                File.WriteAllLines(Path.Combine(tempDir, "Data", "Trees.treedist"), (from el in trees select el.ToString(true)));

                File.WriteAllText(Path.Combine(tempDir, "Data", "SummaryTree.tre"), summaryTree.ToString(true));

                File.WriteAllText(Path.Combine(tempDir, "Data", "Model.nex"), GetModel());

                if (!string.IsNullOrEmpty(this.FindControl<TextBox>("ParameterFileBox").Text))
                {
                    string[] files = this.FindControl<TextBox>("ParameterFileBox").Text.Split(',');

                    for (int i = 0; i < files.Length; i++)
                    {
                        File.Copy(files[i], Path.Combine(tempDir, "Data", "Parameters" + i.ToString() + ".tsv"));
                    }
                }

                string[] args = GetArgs("Data", string.IsNullOrEmpty(this.FindControl<TextBox>("ParameterFileBox").Text) ? 0 : this.FindControl<TextBox>("ParameterFileBox").Text.Split(',').Length, null);

                File.WriteAllLines(Path.Combine(tempDir, ".args"), args);

                if (File.Exists(result))
                {
                    File.Delete(result);
                }

                ZipFile.CreateFromDirectory(tempDir, result, CompressionLevel.Optimal, false);

                Directory.Delete(tempDir, true);

            }


        }

        string[] GetArgs(string dataPath, int parameterFiles, string output)
        {
            List<string> args = new List<string>();

            args.Add("-d");
            args.Add("\"" + Path.Combine(dataPath, "Data.txt").Replace("\\", "/") + "\"");

            args.Add("-t");
            args.Add("\"" + Path.Combine(dataPath, "Trees.treedist").Replace("\\", "/") + "\"");

            args.Add("-n");
            args.Add(((int)this.FindControl<NumericUpDown>("NumSimBox").Value).ToString());

            args.Add("-T");
            args.Add("\"" + Path.Combine(dataPath, "SummaryTree.tre").Replace("\\", "/") + "\"");

            if (!string.IsNullOrEmpty(output))
            {
                args.Add("-o");
                args.Add("\"" + output + "\"");
            }

            args.Add("-i");
            args.Add("\"" + Path.Combine(dataPath, "Model.nex").Replace("\\", "/") + "\"");

            if (this.FindControl<CheckBox>("EstimatedPisBox").IsChecked == true)
            {
                args.Add("--ep");
            }

            if (this.FindControl<NumericUpDown>("SeedBox").Value > 0)
            {
                args.Add("-s");
                args.Add(((int)this.FindControl<NumericUpDown>("SeedBox").Value).ToString());
            }

            if (this.FindControl<CheckBox>("NormBox").IsChecked == true)
            {
                if (this.FindControl<NumericUpDown>("NormValueBox").Value > 0)
                {
                    args.Add("-N=" + this.FindControl<NumericUpDown>("NormValueBox").Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
                else
                {
                    args.Add("-N");
                }
            }

            if (this.FindControl<ComboBox>("ClockLikeTreesBox").SelectedIndex != 0)
            {
                args.Add("-l");
                if (this.FindControl<ComboBox>("ClockLikeTreesBox").SelectedIndex == 1)
                {
                    args.Add("yes");
                }
                else
                {
                    args.Add("no");
                }
            }

            if (this.FindControl<CheckBox>("CoerceBox").IsChecked == true)
            {
                args.Add("-c");
                args.Add(this.FindControl<NumericUpDown>("CoerceValueBox").Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            if (this.FindControl<NumericUpDown>("ThreadsBox").Value > 1)
            {
                args.Add("--nt");
                args.Add(((int)this.FindControl<NumericUpDown>("ThreadsBox").Value).ToString());
            }

            if (this.FindControl<CheckBox>("KillBox").IsChecked == false)
            {
                args.Add("--kill-");
            }

            if (this.FindControl<TextBox>("MLStrategy").Text != "IterativeSampling(0.0001,10.0001,0.1,plot,Value,0.001)|RandomWalk(Value,0.001,10000,plot)|NesterovClimbing(Value,0.001,100,plot)")
            {
                args.Add("-m");
                args.Add("\"" + this.FindControl<TextBox>("MLStrategy").Text + "\"");
            }

            if (this.FindControl<NumericUpDown>("ParallelMLEBox").Value > 1)
            {
                args.Add("--pm");
                args.Add(((int)this.FindControl<NumericUpDown>("ParallelMLEBox").Value).ToString());
            }

            if (this.FindControl<NumericUpDown>("MLERoundsBox").Value > 1)
            {
                args.Add("--mr");
                args.Add(((int)this.FindControl<NumericUpDown>("MLERoundsBox").Value).ToString());
            }

            if (this.FindControl<CheckBox>("SaveSampledLikelihoods").IsChecked == true)
            {
                args.Add("--sl");
            }

            if (this.FindControl<CheckBox>("PlotSampledLikelihoods").IsChecked == true)
            {
                args.Add("--pl");
            }

            /*if (this.FindControl<CheckBox>("ComputeHessianBox").IsChecked == true)
            {
                args.Add("--H");
            }*/

            if (this.FindControl<CheckBox>("PPBox").IsChecked == true)
            {
                if (this.FindControl<RadioButton>("DTestRadio").IsChecked == true)
                {
                    args.Add("--d-test");
                    args.Add(((int)this.FindControl<NumericUpDown>("PPMultiplicityBox").Value).ToString());
                }
                else if (this.FindControl<RadioButton>("PPRadio").IsChecked == true)
                {
                    args.Add("--posterior-predictive");
                    args.Add(((int)this.FindControl<NumericUpDown>("PPMultiplicityBox").Value).ToString());
                }
            }

            if (this.FindControl<CheckBox>("PollBox").IsChecked == true)
            {
                args.Add("--poll");
            }

            if (this.FindControl<ComboBox>("WatchdogActionBox").SelectedIndex != 2)
            {
                switch (this.FindControl<ComboBox>("WatchdogActionBox").SelectedIndex)
                {
                    case 0:
                        args.Add("--watchdog");
                        args.Add("Nothing");
                        break;
                    case 1:
                        args.Add("--watchdog");
                        args.Add("Converge");
                        break;
                    case 2:
                        args.Add("--watchdog");
                        args.Add("Restart");
                        break;
                }
            }

            if (this.FindControl<NumericUpDown>("WatchdogTimeoutBox").Value != 20000)
            {
                args.Add("--watchdog-timeout");
                args.Add(((long)this.FindControl<NumericUpDown>("WatchdogTimeoutBox").Value).ToString());
            }

            if (this.FindControl<NumericUpDown>("NumRunsBox").Value != 2)
            {
                args.Add("--num-runs");
                args.Add(((int)this.FindControl<NumericUpDown>("NumRunsBox").Value).ToString());
            }

            if (this.FindControl<NumericUpDown>("NumChainsBox").Value != 4)
            {
                args.Add("--num-chains");
                args.Add(((int)this.FindControl<NumericUpDown>("NumChainsBox").Value).ToString());
            }

            if (this.FindControl<NumericUpDown>("TempBox").Value != 0.5)
            {
                args.Add("--temp");
                args.Add(this.FindControl<NumericUpDown>("TempBox").Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            if (this.FindControl<NumericUpDown>("sfBox").Value != 10)
            {
                args.Add("--sf");
                args.Add(((int)this.FindControl<NumericUpDown>("sfBox").Value).ToString());
            }

            if (this.FindControl<NumericUpDown>("wfBox").Value != 10)
            {
                args.Add("--wf");
                args.Add(((int)this.FindControl<NumericUpDown>("wfBox").Value).ToString());
            }

            if (this.FindControl<NumericUpDown>("dfBox").Value != 1000)
            {
                args.Add("--df");
                args.Add(((int)this.FindControl<NumericUpDown>("dfBox").Value).ToString());
            }

            if (this.FindControl<NumericUpDown>("MinSamplesBox").Value != 2 * ((int)this.FindControl<NumericUpDown>("NumSimBox").Value))
            {
                args.Add("--min-samples");
                args.Add(((int)this.FindControl<NumericUpDown>("MinSamplesBox").Value).ToString());
            }

            if (this.FindControl<CheckBox>("MaxSamplesCheckBox").IsChecked == true)
            {
                args.Add("--max-samples");
                args.Add(((int)this.FindControl<NumericUpDown>("MaxSamplesBox").Value).ToString());
            }

            if (this.FindControl<CheckBox>("MaxCoVCheckBox").IsChecked == true)
            {
                args.Add("--max-cov");
                args.Add(this.FindControl<NumericUpDown>("MaxCoVBox").Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            if (this.FindControl<NumericUpDown>("MinESSBox").Value != 200)
            {
                args.Add("--min-ess");
                args.Add(((int)this.FindControl<NumericUpDown>("MinESSBox").Value).ToString());
            }

            if (this.FindControl<CheckBox>("MaxRhatCheckBox").IsChecked == true)
            {
                args.Add("--max-rhat");
                args.Add(this.FindControl<NumericUpDown>("MaxRhatBox").Value.ToString(System.Globalization.CultureInfo.InvariantCulture));

                args.Add("--ss-max-rhat");
                args.Add(this.FindControl<NumericUpDown>("MaxSSRhatBox").Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            if (this.FindControl<CheckBox>("EstimateStepsBox").IsChecked != true)
            {
                args.Add("--estimate-steps-");
            }

            if (this.FindControl<NumericUpDown>("TuningAttemptsBox").Value != 10)
            {
                args.Add("--tuning-attempts");
                args.Add(((int)this.FindControl<NumericUpDown>("TuningAttemptsBox").Value).ToString());
            }

            if (this.FindControl<NumericUpDown>("TuningStepsBox").Value != 100)
            {
                args.Add("--tuning-steps");
                args.Add(((int)this.FindControl<NumericUpDown>("TuningStepsBox").Value).ToString());
            }

            if (this.FindControl<NumericUpDown>("AcceptanceRateBox").Value != 0.37)
            {
                args.Add("--acceptance-rate");
                args.Add(this.FindControl<NumericUpDown>("AcceptanceRateBox").Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            if (this.FindControl<TextBox>("StepSizeMultipliersBox").Text != "1")
            {
                args.Add("--sm");
                args.Add(this.FindControl<TextBox>("StepSizeMultipliersBox").Text.Replace(" ", ""));
            }

            if (this.FindControl<CheckBox>("PriorBox").IsChecked == true)
            {
                args.Add("--prior");
            }

            if (this.FindControl<CheckBox>("SSBox").IsChecked == true)
            {
                args.Add("--ss");
            }

            if (this.FindControl<NumericUpDown>("SSStepsBox").Value != 8)
            {
                args.Add("--ss-steps");
                args.Add(((int)this.FindControl<NumericUpDown>("SSStepsBox").Value).ToString());
            }

            if (this.FindControl<NumericUpDown>("SSShapeBox").Value != 0.3)
            {
                args.Add("--ss-shape");
                args.Add(this.FindControl<NumericUpDown>("SSShapeBox").Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            if (this.FindControl<NumericUpDown>("SSSamplesBox").Value != ((int)this.FindControl<NumericUpDown>("NumSimBox").Value))
            {
                args.Add("--ss-samples");
                args.Add(((int)this.FindControl<NumericUpDown>("SSSamplesBox").Value).ToString());
            }

            if (this.FindControl<CheckBox>("SSEstimateStepsBox").IsChecked == true)
            {
                args.Add("--ss-estimate-steps");
            }

            if (parameterFiles > 0)
            {
                args.Add("--parameters");
                args.Add("\"" + (from el in Utils.Utils.Range(0, parameterFiles) select Path.Combine(dataPath, "Parameters" + el.ToString() + ".tsv")).Aggregate((a, b) => a + "," + b).Replace("\\", "/") + "\"");
            }

            if (this.FindControl<NumericUpDown>("PlotWidthBox").Value != 500)
            {
                args.Add("--pw");
                args.Add(this.FindControl<NumericUpDown>("PlotWidthBox").Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            if (this.FindControl<NumericUpDown>("PlotHeightBox").Value != 0)
            {
                args.Add("--ph");
                args.Add(this.FindControl<NumericUpDown>("PlotHeightBox").Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            if (this.FindControl<ComboBox>("BinRuleBox").SelectedIndex != 5)
            {
                args.Add("--bin-rule");
                args.Add(((Utils.Plotting.BinRules)this.FindControl<ComboBox>("BinRuleBox").SelectedIndex).ToString());
            }

            return args.ToArray();
        }

        bool deleteOutputPrefix = false;

        RunWindow runWindow;

        private async void RunAnalysisClicked(object sender, RoutedEventArgs e)
        {
            string tempDir = Path.GetTempFileName();
            File.Delete(tempDir);

            Directory.CreateDirectory(tempDir);

            Directory.CreateDirectory(Path.Combine(tempDir, "Data"));

            File.WriteAllText(Path.Combine(tempDir, "Data", "Data.txt"), data.ToString());

            File.WriteAllLines(Path.Combine(tempDir, "Data", "Trees.treedist"), (from el in trees select el.ToString(true)));

            File.WriteAllText(Path.Combine(tempDir, "Data", "SummaryTree.tre"), summaryTree.ToString(true));

            File.WriteAllText(Path.Combine(tempDir, "Data", "Model.nex"), GetModel());

            if (!string.IsNullOrEmpty(this.FindControl<TextBox>("ParameterFileBox").Text))
            {
                string[] files = this.FindControl<TextBox>("ParameterFileBox").Text.Split(',');

                for (int i = 0; i < files.Length; i++)
                {
                    File.Copy(files[i], Path.Combine(tempDir, "Data", "Parameters" + i.ToString() + ".tsv"));
                }
            }

            if (this.FindControl<CheckBox>("UseTempOutputBox").IsChecked == true)
            {
                outputPrefix = Path.Combine(tempDir, "output");
                deleteOutputPrefix = true;
            }

            string[] args = GetArgs(Path.Combine(tempDir, "Data"), string.IsNullOrEmpty(this.FindControl<TextBox>("ParameterFileBox").Text) ? 0 : this.FindControl<TextBox>("ParameterFileBox").Text.Split(',').Length, outputPrefix);

            runWindow = new RunWindow(args, false, true);
            await runWindow.ShowDialog(this);

            this.FindControl<Button>("RunAnalysisButton").IsVisible = false;
            DisableAllChildren(this.FindControl<StackPanel>("AdvancedSettingsPanel"), new List<Control>() { this.FindControl<Button>("MCMCOptionsShowButton") });
            this.FindControl<CheckBox>("UseTempOutputBox").IsEnabled = false;
            this.FindControl<Button>("ChooseOutputFolderButton").IsVisible = false;

            this.FindControl<Viewbox>("PlotResultsHeader").IsVisible = true;
            this.FindControl<WrapPanel>("PlotResultsButtonsPanel").IsVisible = true;

            for (int i = 0; i < runWindow.FinishedRuns.Length; i++)
            {
                Button btn = new Button() { Content = "Plot set " + i.ToString() + "...", Padding = new Thickness(10, 2.5), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 10) };
                btn.Classes.Add("Red");

                int j = i;

                btn.Click += async (s, ev) =>
                {
                    await PlotResultsClicked(j);
                };

                this.FindControl<WrapPanel>("PlotResultsButtonsPanel").Children.Add(btn);
            }

            if (runWindow.FinishedRuns.Length > 1)
            {
                this.FindControl<Button>("SaveSMapButton").Content = "Save sMap run files...";
            }

            this.FindControl<StackPanel>("SaveResultsButtonsPanel").IsVisible = true;

            this.FindControl<TipContainer>("Tips").SetTip(TipContainer.Tips.AnalysisResults);

            new Thread(() =>
            {
                Thread.Sleep(10);
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<ScrollViewer>("MainScrollViewer").ScrollToBottom();
                });
            }).Start();
        }

        private void DisableAllChildren(Control control, List<Control> skip)
        {
            if (control is ContentControl && ((ContentControl)control).Content is Control && (!(control is Button) || control is RadioButton))
            {
                Control child = (Control)((ContentControl)control).Content;
                DisableAllChildren(child, skip);
            }
            else if (control is Panel)
            {
                foreach (Control ctrl in ((Panel)control).Children)
                {
                    DisableAllChildren(ctrl, skip);
                }
            }
            else if (control is Decorator)
            {
                Control child = (Control)((Decorator)control).Child;
                DisableAllChildren(child, skip);
            }
            else if (!skip.Contains(control) && (!(control is Button) || control is CheckBox))
            {
                control.IsEnabled = false;
            }
            else if (!skip.Contains(control) && (control is Button) && !(control is CheckBox))
            {
                control.IsVisible = false;
            }
        }

        private void WizardClosed(object sender, EventArgs e)
        {
            if (deleteOutputPrefix)
            {
                try
                {
                    Directory.Delete(Directory.GetParent(outputPrefix).FullName, true);
                }
                catch
                {

                }
            }
        }

        private async Task PlotResultsClicked(int index)
        {
            PlotSMapWindow win = new PlotSMapWindow(runWindow.FinishedRuns[index]);
            await win.ShowDialog(this);
        }

        private async void SaveSMapClicked(object sender, RoutedEventArgs e)
        {
            if (runWindow.FinishedRuns.Length == 1)
            {
                SaveFileDialog dialog = new SaveFileDialog()
                {
                    Title = "Save sMap run file",
                    Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Name = "sMap run files", Extensions = new List<string>() { "bin" } }, new FileDialogFilter() { Name = "All files", Extensions = new List<string>() { "*" } } },
                    DefaultExtension = "bin"
                };

                string result = await dialog.ShowAsync(this);

                if (!string.IsNullOrEmpty(result))
                {
                    File.Copy(outputPrefix + ".smap.bin", result, true);
                }
            }
            else
            {
                SaveFileDialog dialog = new SaveFileDialog()
                {
                    Title = "Save sMap run files",
                    Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Name = "ZIP archives", Extensions = new List<string>() { "zip" } }, new FileDialogFilter() { Name = "All files", Extensions = new List<string>() { "*" } } },
                    DefaultExtension = "zip"
                };

                string result = await dialog.ShowAsync(this);

                if (!string.IsNullOrEmpty(result))
                {
                    string tempDir = Path.GetTempFileName();
                    File.Delete(tempDir);

                    Directory.CreateDirectory(tempDir);

                    string[] files = Directory.GetFiles(Directory.GetParent(outputPrefix).FullName, Path.GetFileName(outputPrefix) + "*.smap.bin");

                    for (int i = 0; i < files.Length; i++)
                    {
                        File.Copy(files[i], Path.Combine(tempDir, Path.GetFileName(files[i])));
                    }

                    if (File.Exists(result))
                    {
                        File.Delete(result);
                    }

                    ZipFile.CreateFromDirectory(tempDir, result, CompressionLevel.Optimal, false);

                    Directory.Delete(tempDir, true);
                }
            }
        }

        private async void SaveAllOutputFilesClicked(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog()
            {
                Title = "Save sMap output files",
                Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Name = "ZIP archives", Extensions = new List<string>() { "zip" } }, new FileDialogFilter() { Name = "All files", Extensions = new List<string>() { "*" } } },
                DefaultExtension = "zip"
            };

            string result = await dialog.ShowAsync(this);

            if (!string.IsNullOrEmpty(result))
            {
                string tempDir = Path.GetTempFileName();
                File.Delete(tempDir);

                Directory.CreateDirectory(tempDir);

                string[] files = Directory.GetFiles(Directory.GetParent(outputPrefix).FullName, Path.GetFileName(outputPrefix) + "*");

                for (int i = 0; i < files.Length; i++)
                {
                    File.Copy(files[i], Path.Combine(tempDir, Path.GetFileName(files[i])));
                }

                if (File.Exists(result))
                {
                    File.Delete(result);
                }

                ZipFile.CreateFromDirectory(tempDir, result, CompressionLevel.Optimal, false);

                Directory.Delete(tempDir, true);
            }
        }
    }
}
