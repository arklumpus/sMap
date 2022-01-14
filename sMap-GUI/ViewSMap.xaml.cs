using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Linq;
using System.Collections.Generic;
using SlimTreeNode;
using Utils;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using VectSharp.Canvas;

namespace sMap_GUI
{
    public class ViewSMap : Window
    {
        public ViewSMap()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        TreeNode Tree;
        bool TreesClockLike;
        string[][] States;

        bool initialised = false;

        double[][][] StateProbs;

        TaggedHistory[][] Histories;

        int[] TreeSamples;
        LikelihoodModel[] LikModels;

        LikelihoodModel MeanLikModel;

        int[][] MeanNodeCorresp;

        public enum SMapWindowType
        {
            SMap, SampleSizes
        }

        SMapWindowType WindowType;

        public ViewSMap(Utils.TaggedHistory[][] histories, TreeNode tree, string[][] states, double[][][] stateProbs, int[] treeSamples, LikelihoodModel[] likModels, LikelihoodModel meanLikModel, int[][] meanNodeCorresp, SMapWindowType windowType, bool treesClockLike)
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            switch (windowType)
            {
                case SMapWindowType.SampleSizes:
                    this.FindControl<StackPanel>("SampleSizesTitle").IsVisible = true;
                    this.Title = "View sample sizes";
                    this.FindControl<StackPanel>("CharacterSetPanel").IsVisible = false;
                    this.FindControl<StackPanel>("LegendPanel").IsVisible = false;
                    break;
                case SMapWindowType.SMap:
                    this.FindControl<StackPanel>("sMapTitle").IsVisible = true;
                    this.Title = "View stochastic map";
                    break;
            }


            this.TreesClockLike = treesClockLike;

            WindowType = windowType;


            List<ComboBoxItem> items = new List<ComboBoxItem>(from el in Utils.Utils.Range(0, states.Length) select new ComboBoxItem() { Content = new TextBlock() { Text = "Set " + el.ToString() } });

            this.FindControl<ComboBox>("SelectedSetBox").Items = items;
            this.FindControl<ComboBox>("SelectedSetBox").SelectedIndex = 0;

            tree = tree.Clone();
            tree.SortNodes(false);
            Tree = tree;

            States = states;

            StateProbs = stateProbs;

            Histories = histories;

            TreeSamples = treeSamples;

            LikModels = likModels;

            MeanLikModel = meanLikModel;

            MeanNodeCorresp = meanNodeCorresp;

            DrawTree(0);

            initialised = true;

            //Workaround Avalonia bug
            async void resize()
            {
                await System.Threading.Tasks.Task.Delay(100);
                this.Height = this.Height + 1;
            };

            resize();
        }

        private void DrawTree(int setInd)
        {
            List<(int r, int g, int b, double a)> stateColours = new List<(int r, int g, int b, double a)>();

            if (WindowType == SMapWindowType.SMap)
            {
                this.FindControl<StackPanel>("LegendContainer").Children.Clear();

                for (int j = 0; j < States[setInd].Length; j++)
                {
                    stateColours.Add(Plotting.GetColor(j, 1, States[setInd].Length));

                    this.FindControl<StackPanel>("LegendContainer").Children.Add(new TextBlock() { Text = States[setInd][j], FontWeight = FontWeight.Bold, FontSize = 18, Margin = new Thickness(20, 0, 0, 0), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
                    this.FindControl<StackPanel>("LegendContainer").Children.Add(new Ellipse { Fill = Program.GetBrush(stateColours.Last()), StrokeThickness = 1.5, Stroke = new SolidColorBrush(Colors.Black), Margin = new Thickness(5, 0, 0, 0), Width = 20, Height = 20, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
                }
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

            bool isClockLike = TreesClockLike;

            double resolution = 2;

            if (WindowType == SMapWindowType.SMap)
            {
                Tree.PlotTree(ctx, plotWidth, plotHeight, opt, 10, Plotting.NodePie(opt, StateProbs[setInd], stateColours), Plotting.BranchSMap(resolution, Histories[setInd], isClockLike, TreeSamples, LikModels, MeanLikModel, MeanNodeCorresp, new List<string>(States[setInd]), opt, stateColours), Plotting.NoLegend, plotHeight);
            }
            else if (WindowType == SMapWindowType.SampleSizes)
            {
                Tree.PlotTree(ctx, plotWidth, plotHeight, opt, 10, Plotting.NodeNoAction, Plotting.BranchSampleSizes(resolution, Histories[setInd], isClockLike, TreeSamples, LikModels, MeanLikModel, MeanNodeCorresp, opt), Plotting.NoLegend, plotHeight);
            }

            this.FindControl<Viewbox>("TreeContainer").Child = pg.PaintToCanvas();
        }

        private void SelectedSetChanged(object sender, SelectionChangedEventArgs e)
        {
            if (initialised)
            {
                DrawTree(this.FindControl<ComboBox>("SelectedSetBox").SelectedIndex);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
