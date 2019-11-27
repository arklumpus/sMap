using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SlimTreeNode;
using System.Collections.Generic;
using Utils;
using System.Linq;
using Avalonia.Media;
using System;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Threading;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Threading;
using VectSharp.Canvas;

namespace sMap_GUI
{
    public class NodeInfoWindow : Window
    {
        public NodeInfoWindow()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        int nodeID = 0;
        double nodePosition = 0;
        double fixedNodePosition = 0;

        SerializedRun Run;
        List<TreeNode> Nodes;
        (double CenterX, double Y, bool Top)[] branchesPositions;
        Path[] branchHighlights;

        Canvas containerCanvas;

        int timeout = 5000;

        public NodeInfoWindow(SerializedRun run)
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            Run = run;

            Nodes = run.SummaryTree.GetChildrenRecursive();

            branchesPositions = new (double CenterX, double Y, bool Top)[Nodes.Count];

            branchHighlights = new Path[Nodes.Count];

            UpdateBranchInfo();


            Thread thr = new Thread(() =>
            {
                Thread.Sleep(500);
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<ZoomBorder>("TreeContainer").Uniform();
                });
            });

            thr.Start();
        }

        private void DrawSMap(EventWaitHandle abortHandle)
        {
            string[] States = Run.States;
            TreeNode Tree = Run.SummaryTree;

            List<(int r, int g, int b, double a)> stateColours = new List<(int r, int g, int b, double a)>();

            this.FindControl<StackPanel>("LegendContainer").Children.Clear();

            for (int j = 0; j < States.Length; j++)
            {
                stateColours.Add(Plotting.GetColor(j, 1, States.Length));

                this.FindControl<StackPanel>("LegendContainer").Children.Add(new TextBlock() { Text = States[j], FontWeight = FontWeight.Bold, FontSize = 18, Margin = new Thickness(20, 0, 0, 0), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
                this.FindControl<StackPanel>("LegendContainer").Children.Add(new Ellipse { Fill = Program.GetBrush(stateColours.Last()), StrokeThickness = 1.5, Stroke = new SolidColorBrush(Colors.Black), Margin = new Thickness(5, 0, 0, 0), Width = 20, Height = 20, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
            }

            double totalLength = Tree.DownstreamLength();

            double minBranch = (from el in Tree.GetChildrenRecursive() where el.Length > 0 select el.Length).Min();

            List<string> leaves = Tree.GetLeafNames();

            double maxLabelWidth = 0;

            for (int i = 0; i < leaves.Count; i++)
            {
                FormattedText txt = new FormattedText() { Text = leaves[i], Typeface = new Typeface(FontFamily, 15) };
                maxLabelWidth = Math.Max(maxLabelWidth, txt.Bounds.Width);
            }

            maxLabelWidth += 28;

            float plotWidth = Math.Max(500, Math.Min(2000, (float)(10 * totalLength / minBranch))) + (float)maxLabelWidth;

            float plotHeight = (Tree.GetLeafNames().Count * 15 * 1.4F + plotWidth / 25F);

            VectSharp.Page pg = new VectSharp.Page(plotWidth, plotHeight);

            VectSharp.Graphics ctx = pg.Graphics;

            Plotting.Options opt = new Plotting.Options()
            {
                PieSize = 8,
                FontSize = 15,
                LineWidth = 1.5F
            };

            float pageHeight = plotHeight;

            pageHeight = plotHeight + opt.LineWidth + opt.FontSize * 3;

            bool isClockLike = Tree.IsClocklike();

            double resolution = 2;
            Action<Path> enterAction = path =>
            {
                path.Stroke = new SolidColorBrush(Color.FromArgb(255, 180, 180, 180));
            };

            Action<Path> exitAction = path =>
            {
                path.Stroke = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
            };

            Action<int> clickAction = i =>
            {
                nodeID = i;
                nodePosition = 0;
                fixedNodePosition = 0;
                UpdateBranchInfo();
            };

            Action<int, double, double, bool, Path> controlAction = (i, x, y, top, path) => { branchesPositions[i] = (x, y, top); branchHighlights[i] = path;  };

            Dictionary<string, Delegate> taggedActions = new Dictionary<string, Delegate>();

            Tree.PlotTree(ctx, plotWidth, plotHeight, opt, 10, PlotUtils.NodePie(taggedActions, opt, GetConditionedProbabilities(), stateColours, enterAction, exitAction, clickAction, branchHighlights, controlAction), PlotUtils.BranchSMapInteractive(taggedActions, resolution, Run.Histories, isClockLike, Run.TreeSamples, Run.LikelihoodModels, new LikelihoodModel(Run.SummaryTree), Run.SummaryNodeCorresp, new List<string>(States), opt, stateColours, enterAction, exitAction, clickAction, controlAction), Plotting.NoLegend, plotHeight, abortHandle, abortHandle == null);

            Canvas parentCanvas = new Canvas() { Width = plotWidth, Height = plotHeight };

            parentCanvas.Children.Add(pg.PaintToCanvas(taggedActions));

            containerCanvas = new Canvas() { Width = plotWidth, Height = plotHeight, Margin = new Thickness(18, 10, 0, 0) };
            parentCanvas.Children.Add(containerCanvas);

            this.FindControl<ZoomBorder>("TreeContainer").Child = parentCanvas;

        }

        private void DrawTreePies(double[][] probs, EventWaitHandle abortHandle)
        {
            string[] States = Run.States;
            TreeNode Tree = Run.SummaryTree;

            List<(int r, int g, int b, double a)> stateColours = new List<(int r, int g, int b, double a)>();

            this.FindControl<StackPanel>("LegendContainer").Children.Clear();

            for (int j = 0; j < States.Length; j++)
            {
                stateColours.Add(Plotting.GetColor(j, 1, States.Length));

                this.FindControl<StackPanel>("LegendContainer").Children.Add(new TextBlock() { Text = States[j], FontWeight = FontWeight.Bold, FontSize = 18, Margin = new Thickness(20, 0, 0, 0), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
                this.FindControl<StackPanel>("LegendContainer").Children.Add(new Ellipse { Fill = Program.GetBrush(stateColours.Last()), StrokeThickness = 1.5, Stroke = new SolidColorBrush(Colors.Black), Margin = new Thickness(5, 0, 0, 0), Width = 20, Height = 20, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
            }

            double totalLength = Tree.DownstreamLength();

            double minBranch = (from el in Tree.GetChildrenRecursive() where el.Length > 0 select el.Length).Min();

            List<string> leaves = Tree.GetLeafNames();

            double maxLabelWidth = 0;

            for (int i = 0; i < leaves.Count; i++)
            {
                FormattedText txt = new FormattedText() { Text = leaves[i], Typeface = new Typeface(FontFamily, 15) };
                maxLabelWidth = Math.Max(maxLabelWidth, txt.Bounds.Width);
            }

            maxLabelWidth += 28;

            float plotWidth = Math.Max(500, Math.Min(2000, (float)(10 * totalLength / minBranch))) + (float)maxLabelWidth;

            float plotHeight = (Tree.GetLeafNames().Count * 15 * 1.4F + plotWidth / 25F);

            VectSharp.Page pg = new VectSharp.Page(plotWidth, plotHeight);

            VectSharp.Graphics ctx = pg.Graphics;

            Plotting.Options opt = new Plotting.Options()
            {
                PieSize = 8,
                FontSize = 15,
                LineWidth = 1.5F
            };

            float pageHeight = plotHeight;

            pageHeight = plotHeight + opt.LineWidth + opt.FontSize * 3;

            bool isClockLike = Tree.IsClocklike();

            Action<Path> enterAction = path =>
            {
                path.Stroke = new SolidColorBrush(Color.FromArgb(255, 180, 180, 180));
            };

            Action<Path> exitAction = path =>
            {
                path.Stroke = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
            };

            Action<int> clickAction = i =>
            {
                nodeID = i;
                nodePosition = 0;
                fixedNodePosition = 0;
                UpdateBranchInfo();
            };

            Action<int, double, double, bool, Path> controlAction = (i, x, y, top, path) => { branchesPositions[i] = (x, y, top); branchHighlights[i] = path; };

            Dictionary<string, Delegate> taggedActions = new Dictionary<string, Delegate>();

            Tree.PlotTree(ctx, plotWidth, plotHeight, opt, 10, PlotUtils.NodePie(taggedActions, opt, probs, stateColours, enterAction, exitAction, clickAction, branchHighlights, controlAction), PlotUtils.BranchSimpleInteractive(taggedActions, opt, enterAction, exitAction, clickAction, controlAction), Plotting.NoLegend, plotHeight, abortHandle, abortHandle == null);

            Canvas parentCanvas = new Canvas() { Width = plotWidth, Height = plotHeight };

            parentCanvas.Children.Add(pg.PaintToCanvas(taggedActions));

            containerCanvas = new Canvas() { Width = plotWidth, Height = plotHeight, Margin = new Thickness(18, 10, 0, 0) };
            parentCanvas.Children.Add(containerCanvas);

            this.FindControl<ZoomBorder>("TreeContainer").Child = parentCanvas;
        }


        private void DrawSimpleTree()
        {
            string[] States = Run.States;
            TreeNode Tree = Run.SummaryTree;

            List<(int r, int g, int b, double a)> stateColours = new List<(int r, int g, int b, double a)>();

            this.FindControl<StackPanel>("LegendContainer").Children.Clear();

            for (int j = 0; j < States.Length; j++)
            {
                stateColours.Add(Plotting.GetColor(j, 1, States.Length));

                this.FindControl<StackPanel>("LegendContainer").Children.Add(new TextBlock() { Text = States[j], FontWeight = FontWeight.Bold, FontSize = 18, Margin = new Thickness(20, 0, 0, 0), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
                this.FindControl<StackPanel>("LegendContainer").Children.Add(new Ellipse { Fill = Program.GetBrush(stateColours.Last()), StrokeThickness = 1.5, Stroke = new SolidColorBrush(Colors.Black), Margin = new Thickness(5, 0, 0, 0), Width = 20, Height = 20, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
            }

            double totalLength = Tree.DownstreamLength();

            double minBranch = (from el in Tree.GetChildrenRecursive() where el.Length > 0 select el.Length).Min();

            List<string> leaves = Tree.GetLeafNames();

            double maxLabelWidth = 0;

            for (int i = 0; i < leaves.Count; i++)
            {
                FormattedText txt = new FormattedText() { Text = leaves[i], Typeface = new Typeface(FontFamily, 15) };
                maxLabelWidth = Math.Max(maxLabelWidth, txt.Bounds.Width);
            }

            maxLabelWidth += 28;

            float plotWidth = Math.Max(500, Math.Min(2000, (float)(10 * totalLength / minBranch))) + (float)maxLabelWidth;

            float plotHeight = (Tree.GetLeafNames().Count * 15 * 1.4F + plotWidth / 25F);

            VectSharp.Page pg = new VectSharp.Page(plotWidth, plotHeight);

            VectSharp.Graphics ctx = pg.Graphics;

            Plotting.Options opt = new Plotting.Options()
            {
                PieSize = 0,
                FontSize = 15,
                LineWidth = 1.5F
            };

            float pageHeight = plotHeight;

            pageHeight = plotHeight + opt.LineWidth + opt.FontSize * 3;

            bool isClockLike = Tree.IsClocklike();

            Action<Path> enterAction = path =>
            {
                path.Stroke = new SolidColorBrush(Color.FromArgb(255, 180, 180, 180));
            };

            Action<Path> exitAction = path =>
            {
                path.Stroke = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
            };

            Action<int> clickAction = i =>
            {
                nodeID = i;
                nodePosition = 0;
                fixedNodePosition = 0;
                UpdateBranchInfo();
            };

            Action<int, double, double, bool, Path> controlAction = (i, x, y, top, path) => { branchesPositions[i] = (x, y, top); branchHighlights[i] = path; };

            Dictionary<string, Delegate> taggedActions = new Dictionary<string, Delegate>();

            Tree.PlotTree(ctx, plotWidth, plotHeight, opt, 10, Plotting.NodeNoAction, PlotUtils.BranchSimpleInteractive(taggedActions, opt, enterAction, exitAction, clickAction, controlAction), Plotting.NoLegend, plotHeight, null, false);

            Canvas parentCanvas = new Canvas() { Width = plotWidth, Height = plotHeight };

            parentCanvas.Children.Add(pg.PaintToCanvas(taggedActions));

            containerCanvas = new Canvas() { Width = plotWidth, Height = plotHeight, Margin = new Thickness(18, 10, 0, 0) };
            parentCanvas.Children.Add(containerCanvas);

            this.FindControl<ZoomBorder>("TreeContainer").Child = parentCanvas;
        }

        private double[][] GetConditionedProbabilities()
        {
            bool isClockLike = Run.SummaryTree.IsClocklike();

            double[][] tbr = new double[Run.MeanPosterior.Length][];

            LikelihoodModel summaryLikelihoodModel = new LikelihoodModel(Run.SummaryTree);

            for (int i = 0; i < tbr.Length; i++)
            {

                if (isClockLike)
                {
                    tbr[i] = Utils.Utils.GetBranchStateProbs(Run.Histories, Run.TreeSamples, Run.LikelihoodModels, summaryLikelihoodModel, Run.SummaryNodeCorresp, new List<string>(Run.AllPossibleStates), i, 0, true);
                }
                else
                {
                    tbr[i] = Utils.Utils.GetBranchStateProbs(Run.Histories, Run.TreeSamples, Run.LikelihoodModels, summaryLikelihoodModel, Run.SummaryNodeCorresp, new List<string>(Run.AllPossibleStates), i, summaryLikelihoodModel.BranchLengths[i], false);
                }
            }

            if (!isClockLike)
            {
                tbr[tbr.Length - 1] = Run.MeanPosterior.Last();
            }

            return tbr;
        }


        List<int>[][] GetMarginalStateCorresp()
        {
            List<List<int>[]> finalTbr = new List<List<int>[]>();

            for (int l = 0; l < Run.States[0].Split(',').Length; l++)
            {
                List<string[]> activeStates = new List<string[]>();
                List<int> activeChars = new List<int>();

                List<string> st = new List<string>();

                for (int j = 0; j < Run.States.Length; j++)
                {
                    if (!st.Contains(Run.States[j].Split(',')[l]))
                    {
                        st.Add(Run.States[j].Split(',')[l]);
                    }
                }

                activeStates.Add(st.ToArray());

                activeChars.Add(l);

                string[][] stateCombinations = Utils.Utils.GetCombinations(activeStates.ToArray());

                List<int>[] tbr = new List<int>[stateCombinations.Length];

                for (int i = 0; i < stateCombinations.Length; i++)
                {
                    bool[] correspStates = new bool[Run.States.Length];

                    for (int j = 0; j < Run.States.Length; j++)
                    {
                        correspStates[j] = true;
                    }

                    for (int j = 0; j < Run.States.Length; j++)
                    {
                        for (int k = 0; k < stateCombinations[i].Length; k++)
                        {
                            if (Run.States[j].Split(',')[activeChars[k]] != stateCombinations[i][k])
                            {
                                correspStates[j] = false;
                            }
                        }
                    }

                    tbr[i] = new List<int>();

                    for (int k = 0; k < correspStates.Length; k++)
                    {
                        if (correspStates[k])
                        {
                            tbr[i].Add(k);
                        }
                    }
                }

                finalTbr.Add(tbr);
            }

            return finalTbr.ToArray();
        }

        string[][] GetMarginalStates()
        {
            List<string[]> finalTbr = new List<string[]>();

            for (int l = 0; l < Run.States[0].Split(',').Length; l++)
            {
                List<string> st = new List<string>();

                for (int j = 0; j < Run.States.Length; j++)
                {
                    if (!st.Contains(Run.States[j].Split(',')[l]))
                    {
                        st.Add(Run.States[j].Split(',')[l]);
                    }
                }

                finalTbr.Add(st.ToArray());
            }

            return finalTbr.ToArray();
        }

        double[][] GetMarginalProbabilities(double[] meanProb)
        {
            List<double[]> finalTbr = new List<double[]>();

            for (int l = 0; l < Run.States[0].Split(',').Length; l++)
            {
                List<string[]> activeStates = new List<string[]>();
                List<int> activeChars = new List<int>();

                List<string> st = new List<string>();

                for (int j = 0; j < Run.States.Length; j++)
                {
                    if (!st.Contains(Run.States[j].Split(',')[l]))
                    {
                        st.Add(Run.States[j].Split(',')[l]);
                    }
                }

                activeStates.Add(st.ToArray());

                activeChars.Add(l);

                string[][] stateCombinations = Utils.Utils.GetCombinations(activeStates.ToArray());

                double[] tbr = new double[stateCombinations.Length];

                for (int i = 0; i < stateCombinations.Length; i++)
                {
                    bool[] correspStates = new bool[Run.States.Length];

                    for (int j = 0; j < Run.States.Length; j++)
                    {
                        correspStates[j] = true;
                    }

                    for (int j = 0; j < Run.States.Length; j++)
                    {
                        for (int k = 0; k < stateCombinations[i].Length; k++)
                        {
                            if (Run.States[j].Split(',')[activeChars[k]] != stateCombinations[i][k])
                            {
                                correspStates[j] = false;
                            }
                        }
                    }

                    tbr[i] = 0;

                    for (int k = 0; k < correspStates.Length; k++)
                    {
                        if (correspStates[k])
                        {
                            tbr[i] += meanProb[k];
                        }
                    }
                }

                finalTbr.Add(tbr);
            }

            return finalTbr.ToArray();
        }

        int lastTreeType = -1;


        private void UpdateTree()
        {
            int treeType = this.FindControl<ComboBox>("ProbTypeBox").SelectedIndex % 3;

            if (lastTreeType == treeType)
            {
                return;
            }

            if (treeType == 0)
            {
                EventWaitHandle abortHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

                Thread watchdog = new Thread(() =>
                {
                    Timer timer = new Timer(
                        (state) =>
                        {
                            abortHandle.Set();
                        }, null, timeout, Timeout.Infinite);

                    abortHandle.WaitOne();
                });

                watchdog.Start();

                DrawSMap(abortHandle);

                if (!abortHandle.WaitOne(0))
                {
                    abortHandle.Set();
                    this.FindControl<Button>("Warning1").IsVisible = false;
                    this.FindControl<Button>("Warning2").IsVisible = false;
                }
                else
                {
                    this.FindControl<Button>("Warning1").IsVisible = true;

                    EventWaitHandle abortHandle2 = new EventWaitHandle(false, EventResetMode.ManualReset);

                    Thread watchdog2 = new Thread(() =>
                    {
                        Timer timer = new Timer(
                            (state) =>
                            {
                                abortHandle2.Set();
                            }, null, timeout, Timeout.Infinite);

                        abortHandle2.WaitOne();
                    });

                    watchdog2.Start();

                    DrawTreePies(GetConditionedProbabilities(), abortHandle2);

                    if (!abortHandle2.WaitOne(0))
                    {
                        abortHandle2.Set();
                        this.FindControl<Button>("Warning2").IsVisible = false;
                    }
                    else
                    {
                        this.FindControl<Button>("Warning2").IsVisible = true;
                        DrawSimpleTree();
                    }
                }
            }
            else if (treeType == 1)
            {
                EventWaitHandle abortHandle2 = new EventWaitHandle(false, EventResetMode.ManualReset);

                Thread watchdog2 = new Thread(() =>
                {
                    Timer timer = new Timer(
                        (state) =>
                        {
                            abortHandle2.Set();
                        }, null, timeout, Timeout.Infinite);

                    abortHandle2.WaitOne();
                });

                watchdog2.Start();

                DrawTreePies(Run.MeanPrior, abortHandle2);

                if (!abortHandle2.WaitOne(0))
                {
                    abortHandle2.Set();
                    this.FindControl<Button>("Warning1").IsVisible = false;
                    this.FindControl<Button>("Warning2").IsVisible = false;
                }
                else
                {
                    this.FindControl<Button>("Warning2").IsVisible = true;
                    this.FindControl<Button>("Warning1").IsVisible = false;
                    DrawSimpleTree();
                }
            }
            else if (treeType == 2)
            {
                EventWaitHandle abortHandle2 = new EventWaitHandle(false, EventResetMode.ManualReset);

                Thread watchdog2 = new Thread(() =>
                {
                    Timer timer = new Timer(
                        (state) =>
                        {
                            abortHandle2.Set();
                        }, null, timeout, Timeout.Infinite);

                    abortHandle2.WaitOne();
                });

                watchdog2.Start();

                DrawTreePies(Run.MeanPosterior, abortHandle2);

                if (!abortHandle2.WaitOne(0))
                {
                    abortHandle2.Set();
                    this.FindControl<Button>("Warning1").IsVisible = false;
                    this.FindControl<Button>("Warning2").IsVisible = false;

                }
                else
                {
                    this.FindControl<Button>("Warning2").IsVisible = true;
                    this.FindControl<Button>("Warning1").IsVisible = false;
                    DrawSimpleTree();
                }
            }

            lastTreeType = treeType;
        }

        private async void Warning1Clicked(object sender, RoutedEventArgs e)
        {
            MessageBox box = new MessageBox("Warning", "Drawing the stochastic map plot was taking too long, so we fell back on only displaying node probabilities. Would you like to retry without the timeout? You can check the progress in the console window.", MessageBox.MessageBoxButtonTypes.YesNo);

            await box.ShowDialog(this);

            if (box.Result == MessageBox.Results.Yes)
            {
                this.FindControl<Button>("Warning1").IsVisible = false;
                this.FindControl<Button>("Warning2").IsVisible = false;

                DrawSMap(null);
            }
        }

        private async void Warning2Clicked(object sender, RoutedEventArgs e)
        {
            MessageBox box = new MessageBox("Warning", "Drawing the node probabilities was taking too long, so we fell back on only displaying the phylogenetic tree. Would you like to retry without the timeout? You can check the progress in the console window.", MessageBox.MessageBoxButtonTypes.YesNo);

            await box.ShowDialog(this);

            if (box.Result == MessageBox.Results.Yes)
            {
                this.FindControl<Button>("Warning2").IsVisible = false;

                int treeType = this.FindControl<ComboBox>("ProbTypeBox").SelectedIndex % 3;

                if (treeType == 0)
                {
                    DrawTreePies(GetConditionedProbabilities(), null);
                }
                else if (treeType == 1)
                {
                    DrawTreePies(Run.MeanPrior, null);
                }
                else if (treeType == 2)
                {
                    DrawTreePies(Run.MeanPosterior, null);
                }

                lastTreeType = treeType;
            }
        }

        private void UpdateBranchInfo()
        {
            UpdateTree();

            this.FindControl<TextBlock>("NodeId").Text = nodeID.ToString();

            if (Nodes[nodeID].Length > 0)
            {
                this.FindControl<TextBlock>("BranchLengthLabel").Text = "Branch length:";
                this.FindControl<TextBlock>("BranchLength").Text = (Nodes[nodeID].Length * Run.AgeScale).ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                this.FindControl<TextBlock>("BranchLengthLabel").Text = "Root node";
                this.FindControl<TextBlock>("BranchLength").Text = "";
            }

            List<TreeNode> allChildren = Nodes[nodeID].GetChildrenRecursive();
            List<TreeNode> leaves = new List<TreeNode>(from el in allChildren where el.Children.Count == 0 select el);

            if (Nodes[nodeID].Children.Count > 0)
            {
                this.FindControl<TextBlock>("InternalNodesLabel").Text = "Internal nodes:";
                this.FindControl<TextBlock>("InternalNodes").Text = ((from el in allChildren where el.Children.Count > 0 select el).Count() - 1).ToString();

                this.FindControl<StackPanel>("TipChildrenContainer").IsVisible = true;
                this.FindControl<TextBlock>("TipChildren").Text = ((from el in allChildren where el.Children.Count == 0 select el).Count()).ToString();

                this.FindControl<TextBlock>("DefiningChildrenLabel").Text = "Defining children:";
                this.FindControl<StackPanel>("DefiningChildren").Children.Clear();
                this.FindControl<StackPanel>("DefiningChildren").Children.Add(new TextBlock() { Text = leaves.First().Name });
                this.FindControl<StackPanel>("DefiningChildren").Children.Add(new TextBlock() { Text = leaves.Last().Name });
            }
            else
            {
                this.FindControl<TextBlock>("InternalNodesLabel").Text = "Leaf node";
                this.FindControl<TextBlock>("InternalNodes").Text = "";

                this.FindControl<StackPanel>("TipChildrenContainer").IsVisible = false;

                this.FindControl<TextBlock>("DefiningChildrenLabel").Text = "Leaf name:";
                this.FindControl<StackPanel>("DefiningChildren").Children.Clear();
                this.FindControl<StackPanel>("DefiningChildren").Children.Add(new TextBlock() { Text = Nodes[nodeID].Name });
            }

            containerCanvas.Children.Clear();

            double x = branchesPositions[nodeID].CenterX;

            double y = branchesPositions[nodeID].Y;

            PathGeometry geo = new PathGeometry();

            if (branchesPositions[nodeID].Top)
            {
                PathFigure pthFig = new PathFigure() { StartPoint = new Point(x - 4, y - 31.5) };
                pthFig.Segments.Add(new LineSegment() { Point = new Point(x + 4, y - 31.5) });
                pthFig.Segments.Add(new LineSegment() { Point = new Point(x + 4, y - 19) });
                pthFig.Segments.Add(new LineSegment() { Point = new Point(x + 10, y - 19) });
                pthFig.Segments.Add(new LineSegment() { Point = new Point(x, y - 6.5) });
                pthFig.Segments.Add(new LineSegment() { Point = new Point(x - 10, y - 19) });
                pthFig.Segments.Add(new LineSegment() { Point = new Point(x - 4, y - 19) });

                pthFig.IsClosed = true;
                geo.Figures.Add(pthFig);
            }
            else
            {
                PathFigure pthFig = new PathFigure() { StartPoint = new Point(x - 4, y + 31.5) };
                pthFig.Segments.Add(new LineSegment() { Point = new Point(x + 4, y + 31.5) });
                pthFig.Segments.Add(new LineSegment() { Point = new Point(x + 4, y + 19) });
                pthFig.Segments.Add(new LineSegment() { Point = new Point(x + 10, y + 19) });
                pthFig.Segments.Add(new LineSegment() { Point = new Point(x, y + 6.5) });
                pthFig.Segments.Add(new LineSegment() { Point = new Point(x - 10, y + 19) });
                pthFig.Segments.Add(new LineSegment() { Point = new Point(x - 4, y + 19) });

                pthFig.IsClosed = true;
                geo.Figures.Add(pthFig);
            }

            containerCanvas.Children.Add(new Path() { Data = geo, Stroke = new SolidColorBrush(Colors.White), StrokeThickness = 2, Fill = new SolidColorBrush(Colors.Black), StrokeJoin = PenLineJoin.Round });

            this.FindControl<Canvas>("ProbPieContainer").Children.Clear();

            int probType = this.FindControl<ComboBox>("ProbTypeBox").SelectedIndex;

            if (probType == 0)
            {
                List<(int r, int g, int b, double a)> stateColours = new List<(int r, int g, int b, double a)>();

                for (int j = 0; j < Run.States.Length; j++)
                {
                    stateColours.Add(Plotting.GetColor(j, 1, Run.States.Length));
                }

                bool isClockLike = Run.SummaryTree.IsClocklike();

                LikelihoodModel summaryLikelihoodModel = new LikelihoodModel(Run.SummaryTree);

                double[] probs;

                if (isClockLike)
                {
                    probs = Utils.Utils.GetBranchStateProbs(Run.Histories, Run.TreeSamples, Run.LikelihoodModels, summaryLikelihoodModel, Run.SummaryNodeCorresp, new List<string>(Run.AllPossibleStates), Run.MeanPosterior.Length - nodeID - 1, nodePosition, true);
                }
                else
                {
                    probs = Utils.Utils.GetBranchStateProbs(Run.Histories, Run.TreeSamples, Run.LikelihoodModels, summaryLikelihoodModel, Run.SummaryNodeCorresp, new List<string>(Run.AllPossibleStates), Run.MeanPosterior.Length - nodeID - 1, summaryLikelihoodModel.BranchLengths[Run.MeanPosterior.Length - nodeID - 1] - nodePosition, false);
                }

                if (!isClockLike && nodeID == 0)
                {
                    probs = Run.MeanPosterior.Last();
                }

                StackPanel probContainer = this.FindControl<StackPanel>("ProbabilitiesContainer");

                probContainer.Children.Clear();

                for (int i = 0; i < Run.States.Length; i++)
                {
                    probContainer.Children.Add(new Ellipse() { Fill = Program.GetBrush(stateColours[i]), Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 1.5, Margin = new Thickness(5, 0, 5, 0), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Width = 20, Height = 20 });
                    probContainer.Children.Add(new TextBlock() { Text = Run.States[i], FontWeight = FontWeight.Bold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
                    probContainer.Children.Add(new TextBlock() { Text = ": " + probs[i].ToString("0.##%", System.Globalization.CultureInfo.InvariantCulture), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) });
                }

                double prevAngle = 0;

                for (int i = 0; i < Run.States.Length; i++)
                {
                    double x0 = 50 + 50 * Math.Cos(prevAngle * 2 * Math.PI);
                    double y0 = 50 + 50 * Math.Sin(prevAngle * 2 * Math.PI);

                    double x1 = 50 + 50 * Math.Cos((prevAngle + probs[i] * 0.5) * 2 * Math.PI);
                    double y1 = 50 + 50 * Math.Sin((prevAngle + probs[i] * 0.5) * 2 * Math.PI);

                    double x2 = 50 + 50 * Math.Cos((prevAngle + probs[i]) * 2 * Math.PI);
                    double y2 = 50 + 50 * Math.Sin((prevAngle + probs[i]) * 2 * Math.PI);

                    prevAngle += probs[i];

                    PathFigure fig = new PathFigure() { StartPoint = new Point(50, 50) };
                    fig.Segments.Add(new LineSegment() { Point = new Point(x0, y0) });
                    fig.Segments.Add(new ArcSegment() { Point = new Point(x1, y1), RotationAngle = probs[i] * 0.5 * 2 * Math.PI, Size = new Size(50, 50) });
                    fig.Segments.Add(new ArcSegment() { Point = new Point(x2, y2), RotationAngle = probs[i] * 0.5 * 2 * Math.PI, Size = new Size(50, 50) });
                    fig.IsClosed = true;

                    PathGeometry geom = new PathGeometry();
                    geom.Figures.Add(fig);

                    this.FindControl<Canvas>("ProbPieContainer").Children.Add(new Path() { Data = geom, Fill = Program.GetBrush(stateColours[i]) });
                }

                this.FindControl<Canvas>("ProbPieContainer").Children.Add(new Ellipse() { Width = 100, Height = 100, Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 2 });
            }
            else if (probType == 1)
            {
                List<(int r, int g, int b, double a)> stateColours = new List<(int r, int g, int b, double a)>();

                for (int j = 0; j < Run.States.Length; j++)
                {
                    stateColours.Add(Plotting.GetColor(j, 1, Run.States.Length));
                }

                bool isClockLike = Run.SummaryTree.IsClocklike();

                LikelihoodModel summaryLikelihoodModel = new LikelihoodModel(Run.SummaryTree);

                double[] probs = Run.MeanPrior[Nodes.Count - 1 - nodeID];

                StackPanel probContainer = this.FindControl<StackPanel>("ProbabilitiesContainer");

                probContainer.Children.Clear();

                for (int i = 0; i < Run.States.Length; i++)
                {
                    probContainer.Children.Add(new Ellipse() { Fill = Program.GetBrush(stateColours[i]), Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 1.5, Margin = new Thickness(5, 0, 5, 0), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Width = 20, Height = 20 });
                    probContainer.Children.Add(new TextBlock() { Text = Run.States[i], FontWeight = FontWeight.Bold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
                    probContainer.Children.Add(new TextBlock() { Text = ": " + probs[i].ToString("0.##%", System.Globalization.CultureInfo.InvariantCulture), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) });
                }

                double prevAngle = 0;

                for (int i = 0; i < Run.States.Length; i++)
                {
                    double x0 = 50 + 50 * Math.Cos(prevAngle * 2 * Math.PI);
                    double y0 = 50 + 50 * Math.Sin(prevAngle * 2 * Math.PI);

                    double x1 = 50 + 50 * Math.Cos((prevAngle + probs[i] * 0.5) * 2 * Math.PI);
                    double y1 = 50 + 50 * Math.Sin((prevAngle + probs[i] * 0.5) * 2 * Math.PI);

                    double x2 = 50 + 50 * Math.Cos((prevAngle + probs[i]) * 2 * Math.PI);
                    double y2 = 50 + 50 * Math.Sin((prevAngle + probs[i]) * 2 * Math.PI);

                    prevAngle += probs[i];

                    PathFigure fig = new PathFigure() { StartPoint = new Point(50, 50) };
                    fig.Segments.Add(new LineSegment() { Point = new Point(x0, y0) });
                    fig.Segments.Add(new ArcSegment() { Point = new Point(x1, y1), RotationAngle = probs[i] * 0.5 * 2 * Math.PI, Size = new Size(50, 50) });
                    fig.Segments.Add(new ArcSegment() { Point = new Point(x2, y2), RotationAngle = probs[i] * 0.5 * 2 * Math.PI, Size = new Size(50, 50) });
                    fig.IsClosed = true;

                    PathGeometry geom = new PathGeometry();
                    geom.Figures.Add(fig);

                    this.FindControl<Canvas>("ProbPieContainer").Children.Add(new Path() { Data = geom, Fill = Program.GetBrush(stateColours[i]) });
                }

                this.FindControl<Canvas>("ProbPieContainer").Children.Add(new Ellipse() { Width = 100, Height = 100, Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 2 });

            }
            else if (probType == 2)
            {
                List<(int r, int g, int b, double a)> stateColours = new List<(int r, int g, int b, double a)>();

                for (int j = 0; j < Run.States.Length; j++)
                {
                    stateColours.Add(Plotting.GetColor(j, 1, Run.States.Length));
                }

                bool isClockLike = Run.SummaryTree.IsClocklike();

                LikelihoodModel summaryLikelihoodModel = new LikelihoodModel(Run.SummaryTree);

                double[] probs = Run.MeanPosterior[Nodes.Count - 1 - nodeID];

                StackPanel probContainer = this.FindControl<StackPanel>("ProbabilitiesContainer");

                probContainer.Children.Clear();

                for (int i = 0; i < Run.States.Length; i++)
                {
                    probContainer.Children.Add(new Ellipse() { Fill = Program.GetBrush(stateColours[i]), Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 1.5, Margin = new Thickness(5, 0, 5, 0), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Width = 20, Height = 20 });
                    probContainer.Children.Add(new TextBlock() { Text = Run.States[i], FontWeight = FontWeight.Bold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
                    probContainer.Children.Add(new TextBlock() { Text = ": " + probs[i].ToString("0.##%", System.Globalization.CultureInfo.InvariantCulture), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) });
                }

                double prevAngle = 0;

                for (int i = 0; i < Run.States.Length; i++)
                {
                    double x0 = 50 + 50 * Math.Cos(prevAngle * 2 * Math.PI);
                    double y0 = 50 + 50 * Math.Sin(prevAngle * 2 * Math.PI);

                    double x1 = 50 + 50 * Math.Cos((prevAngle + probs[i] * 0.5) * 2 * Math.PI);
                    double y1 = 50 + 50 * Math.Sin((prevAngle + probs[i] * 0.5) * 2 * Math.PI);

                    double x2 = 50 + 50 * Math.Cos((prevAngle + probs[i]) * 2 * Math.PI);
                    double y2 = 50 + 50 * Math.Sin((prevAngle + probs[i]) * 2 * Math.PI);

                    prevAngle += probs[i];

                    PathFigure fig = new PathFigure() { StartPoint = new Point(50, 50) };
                    fig.Segments.Add(new LineSegment() { Point = new Point(x0, y0) });
                    fig.Segments.Add(new ArcSegment() { Point = new Point(x1, y1), RotationAngle = probs[i] * 0.5 * 2 * Math.PI, Size = new Size(50, 50) });
                    fig.Segments.Add(new ArcSegment() { Point = new Point(x2, y2), RotationAngle = probs[i] * 0.5 * 2 * Math.PI, Size = new Size(50, 50) });
                    fig.IsClosed = true;

                    PathGeometry geom = new PathGeometry();
                    geom.Figures.Add(fig);

                    this.FindControl<Canvas>("ProbPieContainer").Children.Add(new Path() { Data = geom, Fill = Program.GetBrush(stateColours[i]) });
                }

                this.FindControl<Canvas>("ProbPieContainer").Children.Add(new Ellipse() { Width = 100, Height = 100, Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 2 });

            }
            else if (probType == 3)
            {

                bool isClockLike = Run.SummaryTree.IsClocklike();

                LikelihoodModel summaryLikelihoodModel = new LikelihoodModel(Run.SummaryTree);

                double[] allProbs;

                if (isClockLike)
                {
                    allProbs = Utils.Utils.GetBranchStateProbs(Run.Histories, Run.TreeSamples, Run.LikelihoodModels, summaryLikelihoodModel, Run.SummaryNodeCorresp, new List<string>(Run.AllPossibleStates), Run.MeanPosterior.Length - nodeID - 1, nodePosition, true);
                }
                else
                {
                    allProbs = Utils.Utils.GetBranchStateProbs(Run.Histories, Run.TreeSamples, Run.LikelihoodModels, summaryLikelihoodModel, Run.SummaryNodeCorresp, new List<string>(Run.AllPossibleStates), Run.MeanPosterior.Length - nodeID - 1, summaryLikelihoodModel.BranchLengths[Run.MeanPosterior.Length - nodeID - 1] - nodePosition, false);
                }

                if (!isClockLike && nodeID == 0)
                {
                    allProbs = Run.MeanPosterior.Last();
                }

                double[][] probs = GetMarginalProbabilities(allProbs);

                string[][] states = GetMarginalStates();

                List<int>[][] stateCorresp = GetMarginalStateCorresp();


                List<(int r, int g, int b, double a)> stateColours = new List<(int r, int g, int b, double a)>();

                for (int j = 0; j < Run.States.Length; j++)
                {
                    stateColours.Add(Plotting.GetColor(j, 1, Run.States.Length));
                }


                StackPanel probContainer = this.FindControl<StackPanel>("ProbabilitiesContainer");

                probContainer.Children.Clear();

                for (int i = 0; i < states.Length; i++)
                {
                    probContainer.Children.Add(new TextBlock() { Text = "Character " + i.ToString() + ": ", FontWeight = FontWeight.Bold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) });

                    for (int j = 0; j < states[i].Length; j++)
                    {
                        Canvas statePieContainer = new Canvas() { Width = 20, Height = 20, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(5, 0, 5, 0) };

                        double statePrevAngle = 0;

                        for (int k = 0; k < stateCorresp[i][j].Count; k++)
                        {
                            double x0 = 10 + 10 * Math.Cos(statePrevAngle * 2 * Math.PI);
                            double y0 = 10 + 10 * Math.Sin(statePrevAngle * 2 * Math.PI);

                            double x1 = 10 + 10 * Math.Cos((statePrevAngle + 1.0 / stateCorresp[i][j].Count * 0.5) * 2 * Math.PI);
                            double y1 = 10 + 10 * Math.Sin((statePrevAngle + 1.0 / stateCorresp[i][j].Count * 0.5) * 2 * Math.PI);

                            double x2 = 10 + 10 * Math.Cos((statePrevAngle + 1.0 / stateCorresp[i][j].Count) * 2 * Math.PI);
                            double y2 = 10 + 10 * Math.Sin((statePrevAngle + 1.0 / stateCorresp[i][j].Count) * 2 * Math.PI);

                            statePrevAngle += 1.0 / stateCorresp[i][j].Count;

                            PathFigure fig = new PathFigure() { StartPoint = new Point(10, 10) };
                            fig.Segments.Add(new LineSegment() { Point = new Point(x0, y0) });
                            fig.Segments.Add(new ArcSegment() { Point = new Point(x1, y1), RotationAngle = 1.0 / stateCorresp[i][j].Count * 0.5 * 2 * Math.PI, Size = new Size(10, 10) });
                            fig.Segments.Add(new ArcSegment() { Point = new Point(x2, y2), RotationAngle = 1.0 / stateCorresp[i][j].Count * 0.5 * 2 * Math.PI, Size = new Size(10, 10) });
                            fig.IsClosed = true;

                            PathGeometry geom = new PathGeometry();
                            geom.Figures.Add(fig);

                            statePieContainer.Children.Add(new Path() { Data = geom, Fill = Program.GetBrush(stateColours[stateCorresp[i][j][k]]) });
                        }

                        statePieContainer.Children.Add(new Ellipse() { Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 1.5, Width = 20, Height = 20 });

                        probContainer.Children.Add(statePieContainer);


                        probContainer.Children.Add(new TextBlock() { Text = states[i][j], FontWeight = FontWeight.Bold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
                        probContainer.Children.Add(new TextBlock() { Text = ": " + probs[i][j].ToString("0.##%", System.Globalization.CultureInfo.InvariantCulture), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) });
                    }

                    if (i < states.Length - 1)
                    {
                        probContainer.Children.Add(new TextBlock() { Text = "|", FontWeight = FontWeight.Bold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(10, 0, 10, 0) });
                    }
                }

                double prevAngle = 0;

                for (int i = 0; i < Run.States.Length; i++)
                {
                    double x0 = 50 + 50 * Math.Cos(prevAngle * 2 * Math.PI);
                    double y0 = 50 + 50 * Math.Sin(prevAngle * 2 * Math.PI);

                    double x1 = 50 + 50 * Math.Cos((prevAngle + allProbs[i] * 0.5) * 2 * Math.PI);
                    double y1 = 50 + 50 * Math.Sin((prevAngle + allProbs[i] * 0.5) * 2 * Math.PI);

                    double x2 = 50 + 50 * Math.Cos((prevAngle + allProbs[i]) * 2 * Math.PI);
                    double y2 = 50 + 50 * Math.Sin((prevAngle + allProbs[i]) * 2 * Math.PI);

                    prevAngle += allProbs[i];

                    PathFigure fig = new PathFigure() { StartPoint = new Point(50, 50) };
                    fig.Segments.Add(new LineSegment() { Point = new Point(x0, y0) });
                    fig.Segments.Add(new ArcSegment() { Point = new Point(x1, y1), RotationAngle = allProbs[i] * 0.5 * 2 * Math.PI, Size = new Size(50, 50) });
                    fig.Segments.Add(new ArcSegment() { Point = new Point(x2, y2), RotationAngle = allProbs[i] * 0.5 * 2 * Math.PI, Size = new Size(50, 50) });
                    fig.IsClosed = true;

                    PathGeometry geom = new PathGeometry();
                    geom.Figures.Add(fig);

                    this.FindControl<Canvas>("ProbPieContainer").Children.Add(new Path() { Data = geom, Fill = Program.GetBrush(stateColours[i]) });
                }

                this.FindControl<Canvas>("ProbPieContainer").Children.Add(new Ellipse() { Width = 100, Height = 100, Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 2 });
            }
            else if (probType == 4)
            {

                bool isClockLike = Run.SummaryTree.IsClocklike();

                LikelihoodModel summaryLikelihoodModel = new LikelihoodModel(Run.SummaryTree);

                double[] allProbs = Run.MeanPrior[Nodes.Count - 1 - nodeID];

                double[][] probs = GetMarginalProbabilities(allProbs);

                string[][] states = GetMarginalStates();

                List<int>[][] stateCorresp = GetMarginalStateCorresp();


                List<(int r, int g, int b, double a)> stateColours = new List<(int r, int g, int b, double a)>();

                for (int j = 0; j < Run.States.Length; j++)
                {
                    stateColours.Add(Plotting.GetColor(j, 1, Run.States.Length));
                }


                StackPanel probContainer = this.FindControl<StackPanel>("ProbabilitiesContainer");

                probContainer.Children.Clear();

                for (int i = 0; i < states.Length; i++)
                {
                    probContainer.Children.Add(new TextBlock() { Text = "Character " + i.ToString() + ": ", FontWeight = FontWeight.Bold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) });

                    for (int j = 0; j < states[i].Length; j++)
                    {
                        Canvas statePieContainer = new Canvas() { Width = 20, Height = 20, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(5, 0, 5, 0) };

                        double statePrevAngle = 0;

                        for (int k = 0; k < stateCorresp[i][j].Count; k++)
                        {
                            double x0 = 10 + 10 * Math.Cos(statePrevAngle * 2 * Math.PI);
                            double y0 = 10 + 10 * Math.Sin(statePrevAngle * 2 * Math.PI);

                            double x1 = 10 + 10 * Math.Cos((statePrevAngle + 1.0 / stateCorresp[i][j].Count * 0.5) * 2 * Math.PI);
                            double y1 = 10 + 10 * Math.Sin((statePrevAngle + 1.0 / stateCorresp[i][j].Count * 0.5) * 2 * Math.PI);

                            double x2 = 10 + 10 * Math.Cos((statePrevAngle + 1.0 / stateCorresp[i][j].Count) * 2 * Math.PI);
                            double y2 = 10 + 10 * Math.Sin((statePrevAngle + 1.0 / stateCorresp[i][j].Count) * 2 * Math.PI);

                            statePrevAngle += 1.0 / stateCorresp[i][j].Count;

                            PathFigure fig = new PathFigure() { StartPoint = new Point(10, 10) };
                            fig.Segments.Add(new LineSegment() { Point = new Point(x0, y0) });
                            fig.Segments.Add(new ArcSegment() { Point = new Point(x1, y1), RotationAngle = 1.0 / stateCorresp[i][j].Count * 0.5 * 2 * Math.PI, Size = new Size(10, 10) });
                            fig.Segments.Add(new ArcSegment() { Point = new Point(x2, y2), RotationAngle = 1.0 / stateCorresp[i][j].Count * 0.5 * 2 * Math.PI, Size = new Size(10, 10) });
                            fig.IsClosed = true;

                            PathGeometry geom = new PathGeometry();
                            geom.Figures.Add(fig);

                            statePieContainer.Children.Add(new Path() { Data = geom, Fill = Program.GetBrush(stateColours[stateCorresp[i][j][k]]) });
                        }

                        statePieContainer.Children.Add(new Ellipse() { Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 1.5, Width = 20, Height = 20 });

                        probContainer.Children.Add(statePieContainer);


                        probContainer.Children.Add(new TextBlock() { Text = states[i][j], FontWeight = FontWeight.Bold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
                        probContainer.Children.Add(new TextBlock() { Text = ": " + probs[i][j].ToString("0.##%", System.Globalization.CultureInfo.InvariantCulture), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) });
                    }

                    if (i < states.Length - 1)
                    {
                        probContainer.Children.Add(new TextBlock() { Text = "|", FontWeight = FontWeight.Bold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(10, 0, 10, 0) });
                    }
                }

                double prevAngle = 0;

                for (int i = 0; i < Run.States.Length; i++)
                {
                    double x0 = 50 + 50 * Math.Cos(prevAngle * 2 * Math.PI);
                    double y0 = 50 + 50 * Math.Sin(prevAngle * 2 * Math.PI);

                    double x1 = 50 + 50 * Math.Cos((prevAngle + allProbs[i] * 0.5) * 2 * Math.PI);
                    double y1 = 50 + 50 * Math.Sin((prevAngle + allProbs[i] * 0.5) * 2 * Math.PI);

                    double x2 = 50 + 50 * Math.Cos((prevAngle + allProbs[i]) * 2 * Math.PI);
                    double y2 = 50 + 50 * Math.Sin((prevAngle + allProbs[i]) * 2 * Math.PI);

                    prevAngle += allProbs[i];

                    PathFigure fig = new PathFigure() { StartPoint = new Point(50, 50) };
                    fig.Segments.Add(new LineSegment() { Point = new Point(x0, y0) });
                    fig.Segments.Add(new ArcSegment() { Point = new Point(x1, y1), RotationAngle = allProbs[i] * 0.5 * 2 * Math.PI, Size = new Size(50, 50) });
                    fig.Segments.Add(new ArcSegment() { Point = new Point(x2, y2), RotationAngle = allProbs[i] * 0.5 * 2 * Math.PI, Size = new Size(50, 50) });
                    fig.IsClosed = true;

                    PathGeometry geom = new PathGeometry();
                    geom.Figures.Add(fig);

                    this.FindControl<Canvas>("ProbPieContainer").Children.Add(new Path() { Data = geom, Fill = Program.GetBrush(stateColours[i]) });
                }

                this.FindControl<Canvas>("ProbPieContainer").Children.Add(new Ellipse() { Width = 100, Height = 100, Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 2 });
            }
            else if (probType == 5)
            {

                bool isClockLike = Run.SummaryTree.IsClocklike();

                LikelihoodModel summaryLikelihoodModel = new LikelihoodModel(Run.SummaryTree);

                double[] allProbs = Run.MeanPosterior[Nodes.Count - 1 - nodeID];

                double[][] probs = GetMarginalProbabilities(allProbs);

                string[][] states = GetMarginalStates();

                List<int>[][] stateCorresp = GetMarginalStateCorresp();


                List<(int r, int g, int b, double a)> stateColours = new List<(int r, int g, int b, double a)>();

                for (int j = 0; j < Run.States.Length; j++)
                {
                    stateColours.Add(Plotting.GetColor(j, 1, Run.States.Length));
                }


                StackPanel probContainer = this.FindControl<StackPanel>("ProbabilitiesContainer");

                probContainer.Children.Clear();

                for (int i = 0; i < states.Length; i++)
                {
                    probContainer.Children.Add(new TextBlock() { Text = "Character " + i.ToString() + ": ", FontWeight = FontWeight.Bold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) });

                    for (int j = 0; j < states[i].Length; j++)
                    {
                        Canvas statePieContainer = new Canvas() { Width = 20, Height = 20, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(5, 0, 5, 0) };

                        double statePrevAngle = 0;

                        for (int k = 0; k < stateCorresp[i][j].Count; k++)
                        {
                            double x0 = 10 + 10 * Math.Cos(statePrevAngle * 2 * Math.PI);
                            double y0 = 10 + 10 * Math.Sin(statePrevAngle * 2 * Math.PI);

                            double x1 = 10 + 10 * Math.Cos((statePrevAngle + 1.0 / stateCorresp[i][j].Count * 0.5) * 2 * Math.PI);
                            double y1 = 10 + 10 * Math.Sin((statePrevAngle + 1.0 / stateCorresp[i][j].Count * 0.5) * 2 * Math.PI);

                            double x2 = 10 + 10 * Math.Cos((statePrevAngle + 1.0 / stateCorresp[i][j].Count) * 2 * Math.PI);
                            double y2 = 10 + 10 * Math.Sin((statePrevAngle + 1.0 / stateCorresp[i][j].Count) * 2 * Math.PI);

                            statePrevAngle += 1.0 / stateCorresp[i][j].Count;

                            PathFigure fig = new PathFigure() { StartPoint = new Point(10, 10) };
                            fig.Segments.Add(new LineSegment() { Point = new Point(x0, y0) });
                            fig.Segments.Add(new ArcSegment() { Point = new Point(x1, y1), RotationAngle = 1.0 / stateCorresp[i][j].Count * 0.5 * 2 * Math.PI, Size = new Size(10, 10) });
                            fig.Segments.Add(new ArcSegment() { Point = new Point(x2, y2), RotationAngle = 1.0 / stateCorresp[i][j].Count * 0.5 * 2 * Math.PI, Size = new Size(10, 10) });
                            fig.IsClosed = true;

                            PathGeometry geom = new PathGeometry();
                            geom.Figures.Add(fig);

                            statePieContainer.Children.Add(new Path() { Data = geom, Fill = Program.GetBrush(stateColours[stateCorresp[i][j][k]]) });
                        }

                        statePieContainer.Children.Add(new Ellipse() { Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 1.5, Width = 20, Height = 20 });

                        probContainer.Children.Add(statePieContainer);


                        probContainer.Children.Add(new TextBlock() { Text = states[i][j], FontWeight = FontWeight.Bold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
                        probContainer.Children.Add(new TextBlock() { Text = ": " + probs[i][j].ToString("0.##%", System.Globalization.CultureInfo.InvariantCulture), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) });
                    }

                    if (i < states.Length - 1)
                    {
                        probContainer.Children.Add(new TextBlock() { Text = "|", FontWeight = FontWeight.Bold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(10, 0, 10, 0) });
                    }
                }

                double prevAngle = 0;

                for (int i = 0; i < Run.States.Length; i++)
                {
                    double x0 = 50 + 50 * Math.Cos(prevAngle * 2 * Math.PI);
                    double y0 = 50 + 50 * Math.Sin(prevAngle * 2 * Math.PI);

                    double x1 = 50 + 50 * Math.Cos((prevAngle + allProbs[i] * 0.5) * 2 * Math.PI);
                    double y1 = 50 + 50 * Math.Sin((prevAngle + allProbs[i] * 0.5) * 2 * Math.PI);

                    double x2 = 50 + 50 * Math.Cos((prevAngle + allProbs[i]) * 2 * Math.PI);
                    double y2 = 50 + 50 * Math.Sin((prevAngle + allProbs[i]) * 2 * Math.PI);

                    prevAngle += allProbs[i];

                    PathFigure fig = new PathFigure() { StartPoint = new Point(50, 50) };
                    fig.Segments.Add(new LineSegment() { Point = new Point(x0, y0) });
                    fig.Segments.Add(new ArcSegment() { Point = new Point(x1, y1), RotationAngle = allProbs[i] * 0.5 * 2 * Math.PI, Size = new Size(50, 50) });
                    fig.Segments.Add(new ArcSegment() { Point = new Point(x2, y2), RotationAngle = allProbs[i] * 0.5 * 2 * Math.PI, Size = new Size(50, 50) });
                    fig.IsClosed = true;

                    PathGeometry geom = new PathGeometry();
                    geom.Figures.Add(fig);

                    this.FindControl<Canvas>("ProbPieContainer").Children.Add(new Path() { Data = geom, Fill = Program.GetBrush(stateColours[i]) });
                }

                this.FindControl<Canvas>("ProbPieContainer").Children.Add(new Ellipse() { Width = 100, Height = 100, Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 2 });
            }


            if (nodeID > 0 && (probType == 0 || probType == 3))
            {
                this.FindControl<TextBlock>("Position").Text = (nodePosition * Run.AgeScale).ToString(System.Globalization.CultureInfo.InvariantCulture);
                this.FindControl<StackPanel>("PositionContainer").IsVisible = true;

                List<(int r, int g, int b, double a)> stateColours = new List<(int r, int g, int b, double a)>();

                for (int j = 0; j < Run.States.Length; j++)
                {
                    stateColours.Add(Plotting.GetColor(j, 1, Run.States.Length));
                }

                double[][] probs = new double[101][];

                for (int i = 0; i <= 100; i++)
                {
                    double realX = Nodes[nodeID].Length / 100 * i;

                    bool isClockLike = Run.SummaryTree.IsClocklike();

                    LikelihoodModel summaryLikelihoodModel = new LikelihoodModel(Run.SummaryTree);

                    if (isClockLike)
                    {
                        probs[i] = Utils.Utils.GetBranchStateProbs(Run.Histories, Run.TreeSamples, Run.LikelihoodModels, summaryLikelihoodModel, Run.SummaryNodeCorresp, new List<string>(Run.AllPossibleStates), Run.MeanPosterior.Length - nodeID - 1, summaryLikelihoodModel.BranchLengths[Run.MeanPosterior.Length - nodeID - 1] - realX, true);
                    }
                    else
                    {
                        probs[i] = Utils.Utils.GetBranchStateProbs(Run.Histories, Run.TreeSamples, Run.LikelihoodModels, summaryLikelihoodModel, Run.SummaryNodeCorresp, new List<string>(Run.AllPossibleStates), Run.MeanPosterior.Length - nodeID - 1, realX, false);
                    }
                }

                double[] usedProbs = new double[101];

                this.FindControl<Canvas>("BranchPlotContainer").Children.Clear();

                for (int i = 1; i <= 100; i++)
                {
                    if (probs[i].Sum() == 0)
                    {
                        probs[i] = probs[i - 1];
                    }
                }

                for (int i = 0; i < Run.States.Length; i++)
                {
                    PathFigure fig = new PathFigure() { StartPoint = new Point(0, usedProbs[0] * 100) };

                    for (int j = 0; j <= 100; j++)
                    {
                        fig.Segments.Add(new LineSegment() { Point = new Point(j, (probs[j][i] + usedProbs[j]) * 100) });
                    }

                    for (int j = 100; j >= 0; j--)
                    {
                        fig.Segments.Add(new LineSegment() { Point = new Point(j, usedProbs[j] * 100) });
                        usedProbs[j] += probs[j][i];
                    }

                    fig.IsClosed = true;

                    PathGeometry stateGeo = new PathGeometry();
                    stateGeo.Figures.Add(fig);

                    this.FindControl<Canvas>("BranchPlotContainer").Children.Add(new Path() { Data = stateGeo, Fill = Program.GetBrush(stateColours[i]) });
                }



                this.FindControl<Grid>("PosSliderContainer").ColumnDefinitions[0] = new ColumnDefinition(1 - fixedNodePosition / Nodes[nodeID].Length, GridUnitType.Star);
                this.FindControl<Grid>("PosSliderContainer").ColumnDefinitions[2] = new ColumnDefinition(fixedNodePosition / Nodes[nodeID].Length, GridUnitType.Star);

                if (mouseOnPlot)
                {
                    this.FindControl<Grid>("PosMovingSliderContainer").ColumnDefinitions[0] = new ColumnDefinition(1 - nodePosition / Nodes[nodeID].Length, GridUnitType.Star);
                    this.FindControl<Grid>("PosMovingSliderContainer").ColumnDefinitions[2] = new ColumnDefinition(nodePosition / Nodes[nodeID].Length, GridUnitType.Star);
                    this.FindControl<Grid>("PosMovingSliderContainer").IsVisible = true;
                }
                else
                {
                    this.FindControl<Grid>("PosMovingSliderContainer").IsVisible = false;
                }

                this.FindControl<Grid>("PosLabelContainer").ColumnDefinitions[0] = new ColumnDefinition(1 - nodePosition / Nodes[nodeID].Length, GridUnitType.Star);
                this.FindControl<Grid>("PosLabelContainer").ColumnDefinitions[1] = new ColumnDefinition(nodePosition / Nodes[nodeID].Length, GridUnitType.Star);

                if (nodePosition / Nodes[nodeID].Length > 0.5)
                {
                    this.FindControl<TextBlock>("RightPosLabel").Text = nodePosition.ToString(4, false);
                    this.FindControl<TextBlock>("LeftPosLabel").Text = "";
                }
                else
                {
                    this.FindControl<TextBlock>("LeftPosLabel").Text = nodePosition.ToString(4, false);
                    this.FindControl<TextBlock>("RightPosLabel").Text = "";
                }

                this.FindControl<Grid>("BranchContainer").IsVisible = true;
            }
            else
            {
                this.FindControl<StackPanel>("PositionContainer").IsVisible = false;
                this.FindControl<Grid>("BranchContainer").IsVisible = false;
            }
        }

        private void BranchPlotClicked(object sender, PointerPressedEventArgs e)
        {
            nodePosition = Math.Min(Math.Max(0, (1.0 - e.Device.GetPosition(this.FindControl<Canvas>("BranchPlotContainer")).X / 100.0) * Nodes[nodeID].Length), Nodes[nodeID].Length);
            fixedNodePosition = nodePosition;
            UpdateBranchInfo();
        }

        private void LeftEndPressed(object sender, RoutedEventArgs e)
        {
            fixedNodePosition = nodePosition = Nodes[nodeID].Length;
            UpdateBranchInfo();
        }

        private void RightEndPressed(object sender, RoutedEventArgs e)
        {
            fixedNodePosition = nodePosition = 0;
            UpdateBranchInfo();
        }

        bool initialized = false;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            initialized = true;
        }

        private void BranchPlotMouseMove(object sender, PointerEventArgs e)
        {
            nodePosition = Math.Min(Math.Max(0, (1.0 - e.Device.GetPosition(this.FindControl<Canvas>("BranchPlotContainer")).X / 100.0) * Nodes[nodeID].Length), Nodes[nodeID].Length);
            UpdateBranchInfo();
        }

        bool mouseOnPlot = false;

        private void BranchPlotMouseEnter(object sender, PointerEventArgs e)
        {
            mouseOnPlot = true;
        }

        private void BranchPlotMouseLeave(object sender, PointerEventArgs e)
        {
            mouseOnPlot = false;
            nodePosition = fixedNodePosition;
            UpdateBranchInfo();
        }

        private void ProbTypeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (initialized)
            {
                UpdateBranchInfo();


                Thread thr = new Thread(() =>
                {
                    Thread.Sleep(10);

                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        this.FindControl<ZoomBorder>("TreeContainer").Uniform();
                    });
                });

                thr.Start();
            }
        }

        private void PropertyChangedEvent(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == WidthProperty || e.Property == HeightProperty)
            {
                Thread thr = new Thread(() =>
               {
                   Dispatcher.UIThread.InvokeAsync(() =>
                   {
                       this.FindControl<ZoomBorder>("TreeContainer").Uniform();
                   });
               });

                thr.Start();
            }
        }

        private void FitButtonClicked(object sender, RoutedEventArgs e)
        {
            Button btn1 = this.FindControl<Button>("Warning1");
            Button btn2 = this.FindControl<Button>("Warning2");


            this.FindControl<ZoomBorder>("TreeContainer").Uniform();
        }
    }
}
