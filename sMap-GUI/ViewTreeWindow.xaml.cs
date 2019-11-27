using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SlimTreeNode;
using System.Linq;
using Utils;
using System;
using Avalonia.Media;
using System.Collections.Generic;
using Avalonia.Controls.PanAndZoom;
using System.Threading;
using Avalonia.Threading;
using Avalonia.Interactivity;
using VectSharp.Canvas;

namespace sMap_GUI
{
    public class ViewTreeWindow : Window
    {
        public string Header
        {
            get { return this.FindControl<TextBlock>("HeaderBlock").Text; }
            set { this.FindControl<TextBlock>("HeaderBlock").Text = value; }
        }

        public ViewTreeWindow()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        List<TreeNode> Trees;
        int index = 0;

        public ViewTreeWindow(List<TreeNode> trees)
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            InitializeTrees(trees);
        }

        public ViewTreeWindow(TreeNode tree)
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            InitializeTrees(new List<TreeNode>() { tree });
        }

        private void InitializeTrees(List<TreeNode> trees)
        {
            Trees = trees;

            index = 0;

            if (trees.Count > 1)
            {
                this.FindControl<StackPanel>("MultiTreePanel").IsVisible = true;
                this.FindControl<TextBlock>("MultiTreeTotalBlock").Text = "/ " + trees.Count.ToString();
                this.FindControl<NumericUpDown>("MultiTreeIndex").Maximum = trees.Count;
            }
            else
            {
                this.FindControl<StackPanel>("MultiTreePanel").IsVisible = false;
            }

            DrawTree(index);
        }

        private void DrawTree(int ind)
        {
            TreeNode tree = Trees[ind];

            double totalLength = tree.DownstreamLength();

            double minBranch = (from el in tree.GetChildrenRecursive() where el.Length > 0 select el.Length).Min();

            List<string> leaves = tree.GetLeafNames();

            double maxLabelWidth = 0;

            for (int i = 0; i < leaves.Count; i++)
            {
                FormattedText txt = new FormattedText() { Text = leaves[i], Typeface = new Typeface(FontFamily, 15) };
                maxLabelWidth = Math.Max(maxLabelWidth, txt.Bounds.Width);
            }

            maxLabelWidth += 28;

            float plotWidth = Math.Max(500, Math.Min(2000, (float)(5 * totalLength / minBranch))) + (float)maxLabelWidth;

            float plotHeight = (tree.GetLeafNames().Count * 15 * 1.4F + plotWidth / 25F);

            VectSharp.Page pg = new VectSharp.Page(plotWidth, plotHeight);

            VectSharp.Graphics ctx = pg.Graphics;

            Plotting.Options opt = new Plotting.Options()
            {
                PieSize = 0,
                FontSize = 15,
                LineWidth = 2
            };

            tree.PlotTree(ctx, plotWidth, plotHeight, opt, plotWidth / 50F, Utils.Plotting.NodeNoAction, Utils.Plotting.BranchSimple(opt.LineWidth), Utils.Plotting.StandardAgeAxis(10, plotWidth, plotHeight, 0, opt, tree), plotHeight);

            this.FindControl<ZoomBorder>("TreeContainer").Child = pg.PaintToCanvas();

            Thread thr = new Thread(async () =>
            {
                Thread.Sleep(10);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FindControl<ZoomBorder>("TreeContainer").Uniform();
                });
            });
            thr.Start();
        }

        private void TreeIndexChanged(object sender, NumericUpDownValueChangedEventArgs e)
        {
            index = (int)this.FindControl<NumericUpDown>("MultiTreeIndex").Value - 1;
            DrawTree(index);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void FitButtonClicked(object sender, RoutedEventArgs e)
        {
            this.FindControl<ZoomBorder>("TreeContainer").Uniform();
        }
    }
}
