using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using SlimTreeNode;
using System;
using System.Collections.Generic;
using System.Linq;
using Utils;
using VectSharp.Canvas;

namespace sMap_GUI
{
    public class ViewPriorsWindow : Window
    {
        public ViewPriorsWindow()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        string[][] States;
        double[][][] StateProbs;
        TreeNode Tree;

        bool initialised = false;

        public enum ProbWindowType
        {
            Priors, Likelihoods, Posteriors
        }

        ProbWindowType WindowType;

        public ViewPriorsWindow(TreeNode tree, string[][] states, double[][][] stateProbs, ProbWindowType windowType)
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            switch (windowType)
            {
                case ProbWindowType.Priors:
                    this.FindControl<StackPanel>("PriorsTitle").IsVisible = true;
                    this.Title = "View priors";
                    break;
                case ProbWindowType.Posteriors:
                    this.FindControl<StackPanel>("PosteriorsTitle").IsVisible = true;
                    this.Title = "View posteriors";
                    break;
                case ProbWindowType.Likelihoods:
                    this.FindControl<StackPanel>("LikelihoodsTitle").IsVisible = true;
                    this.Title = "View likelihoods";
                    break;
            }

            WindowType = windowType;

            List<ComboBoxItem> items = new List<ComboBoxItem>(from el in Utils.Utils.Range(0, states.Length) select new ComboBoxItem() { Content = new TextBlock() { Text = "Set " + el.ToString() } });

            this.FindControl<ComboBox>("SelectedSetBox").Items = items;
            this.FindControl<ComboBox>("SelectedSetBox").SelectedIndex = 0;

            tree = tree.Clone();
            tree.SortNodes(false);
            Tree = tree;

            States = states;
            StateProbs = stateProbs;

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
            this.FindControl<StackPanel>("LegendContainer").Children.Clear();

            List<(int r, int g, int b, double a)> stateColours = new List<(int r, int g, int b, double a)>();

            for (int j = 0; j < States[setInd].Length; j++)
            {
                stateColours.Add(Plotting.GetColor(j, 1, States[setInd].Length));

                this.FindControl<StackPanel>("LegendContainer").Children.Add(new TextBlock() { Text = States[setInd][j], FontWeight = FontWeight.Bold, FontSize = 18, Margin = new Thickness(20, 0, 0, 0), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
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



            if (this.WindowType != ProbWindowType.Likelihoods)
            {
                Tree.PlotTree(ctx, plotWidth, plotHeight, opt, plotWidth / 50F, Utils.Plotting.NodePie(opt, StateProbs[setInd], stateColours), Utils.Plotting.BranchSimple(opt.LineWidth), Utils.Plotting.StandardAgeAxis(10, plotWidth, plotHeight, 0, opt, Tree), plotHeight);
            }
            else
            {
                Tree.PlotTree(ctx, plotWidth, plotHeight, opt, plotWidth / 50F, Utils.Plotting.NodeTarget(opt, StateProbs[setInd], stateColours), Utils.Plotting.BranchSimple(opt.LineWidth), Utils.Plotting.StandardAgeAxis(10, plotWidth, plotHeight, 0, opt, Tree), plotHeight);
            }

            this.FindControl<Viewbox>("TreeContainer").Child = pg.PaintToCanvas();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void SelectedSetChanged(object sender, SelectionChangedEventArgs e)
        {
            if (initialised)
            {
                DrawTree(this.FindControl<ComboBox>("SelectedSetBox").SelectedIndex);
            }
        }
    }
}
