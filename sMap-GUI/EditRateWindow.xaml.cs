using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Reflection;
using Utils;
using System.Linq;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;

namespace sMap_GUI
{
    public class EditRateWindow : Window
    {
        public EditRateWindow()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        string[] distribNames = new string[]
        {
            "Beta", "BetaScaled", "Cauchy", "Chi", "ChiSquared", "ContinuousUniform", "Erlang", "Exponential", "FisherSnedecor", "Gamma", "InverseGamma", "Laplace", "LogNormal", "Normal", "Pareto", "Rayleigh", "Stable", "StudentT", "Triangular", "Weibull"
        };

        double[][] defaultParams = new double[][] { new double[] { 2, 2 }, new double[] { 2, 2, 1, 1 }, new double[] { 1, 1 }, new double[] { 2 }, new double[] { 3 }, new double[] { 0, 1 }, new double[] { 2, 1 }, new double[] { 1 }, new double[] { 2, 4 }, new double[] { 2, 1 }, new double[] { 2, 1 }, new double[] { 1, 1 }, new double[] { 1, 1 }, new double[] { 1, 1 }, new double[] { 1, 2 }, new double[] { 1 }, new double[] { 2, 1, 1, 1 }, new double[] { 1, 1, 1 }, new double[] { 0, 2, 1 }, new double[] { 2, 1 } };

        bool initialized = false;

        Parameter Rate;
        Dictionary<string, Parameter> AllRates;

        List<string> equalParams;

        public EditRateWindow(Parameter rate, Dictionary<string, Parameter> allRates)
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            Rate = rate;
            AllRates = allRates;

            string key = allRates.GetKey(rate);

            this.FindControl<TextBlock>("HeaderBlock").Text = "Edit r(" + key.Replace(">", " > ") + ")";

            equalParams = new List<string>(from el in allRates where el.Value != rate && el.Value.Action != Parameter.ParameterAction.Equal select el.Key);

            this.FindControl<ComboBox>("EqualParameterComboBox").Items = equalParams;
            this.FindControl<ComboBox>("EqualParameterComboBox").SelectedIndex = 0;

            BuildBayesianParameters();

            this.FindControl<NumericUpDown>("FixParameterValue").Value = rate.Value;

            if (rate.Action == Parameter.ParameterAction.Fix)
            {
                this.FindControl<RadioButton>("FixRadio").IsChecked = true;
            }
            else if (rate.Action == Parameter.ParameterAction.ML)
            {
                this.FindControl<RadioButton>("MLRadio").IsChecked = true;
            }
            else if (rate.Action == Parameter.ParameterAction.Equal)
            {
                string equalKey = allRates.GetKey(rate.EqualParameter);

                this.FindControl<RadioButton>("EqualRadio").IsChecked = true;
                this.FindControl<ComboBox>("EqualParameterComboBox").SelectedIndex = equalParams.IndexOf(equalKey);
            }
            else if (rate.Action == Parameter.ParameterAction.Bayes)
            {
                string distrib = rate.PriorDistribution.ToString();

                string distribName = distrib.Substring(0, distrib.IndexOf("("));

                this.FindControl<RadioButton>("BayesianRadio").IsChecked = true;

                this.FindControl<ComboBox>("BayesianParameterComboBox").SelectedIndex = distribNames.IndexOf(distribName);

                distrib = distrib.Substring(distrib.IndexOf("(") + 1);
                distrib = distrib.Substring(0, distrib.IndexOf(")"));

                double[] distribParams = (from el in distrib.Split(',') select double.Parse(el.Substring(el.IndexOf("=") + 1).Trim(' '), System.Globalization.CultureInfo.InvariantCulture)).ToArray();

                for (int i = 0; i < parameterBoxes.Count; i++)
                {
                    parameterBoxes[i].Value = distribParams[i];
                }
            }
        }

        List<NumericUpDown> parameterBoxes;

        void BuildBayesianParameters()
        {
            int ind = this.FindControl<ComboBox>("BayesianParameterComboBox").SelectedIndex;
            string distributionName = distribNames[ind];

            Type tp = typeof(Gamma).Assembly.GetType("MathNet.Numerics.Distributions." + distributionName, true, true);

            ParameterInfo[] pi = (from el in tp.GetConstructors() where el.GetParameters().Length > 0 && el.GetParameters()[0].ParameterType != typeof(Random) orderby el.GetParameters().Length ascending select el.GetParameters()).First();

            this.FindControl<StackPanel>("BayesianParametersPanel").Children.Clear();

            parameterBoxes = new List<NumericUpDown>();

            for (int i = 0; i < pi.Length; i++)
            {
                Grid grd = new Grid() { Margin = new Thickness(30, 0, 10, 0) };
                grd.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));
                grd.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));

                string name = pi[i].Name;
                if (name.Length > 2 || name == "mu")
                {
                    name = name.Substring(0, 1).ToUpper() + name.Substring(1);
                }

                grd.Children.Add(new TextBlock() { Text = name + " =", Margin = new Thickness(5), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
                NumericUpDown nu = new NumericUpDown() { Margin = new Thickness(5), Padding = new Thickness(5, 0, 5, 0), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Value = defaultParams[ind][i] };
                nu.ValueChanged += (s, e) =>
                {
                    BuildBayesianPreview();
                };

                Grid.SetColumn(nu, 1);
                grd.Children.Add(nu);

                parameterBoxes.Add(nu);

                this.FindControl<StackPanel>("BayesianParametersPanel").Children.Add(grd);
            }

            BuildBayesianPreview();
        }

        void BuildBayesianPreview()
        {
            Canvas container = this.FindControl<Canvas>("BayesianPreviewCanvas");

            int ind = this.FindControl<ComboBox>("BayesianParameterComboBox").SelectedIndex;

            container.Children.Clear();
            container.Background = null;

            try
            {
                string distribution = distribNames[ind] + "(" + (from el in parameterBoxes select el.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)).Aggregate((a, b) => a + "," + b) + ")";

                IContinuousDistribution distrib = Utils.Utils.ParseDistribution(distribution, new ThreadSafeRandom());

                double[] computedYs = new double[300];

                double[] range = distrib.GetRange(0.95);

                double minX = range[0];
                double maxX = range[1];

                for (int x = 0; x < 300; x++)
                {
                    double realX = x / 299.0 * (maxX - minX) + minX;
                    computedYs[x] = distrib.Density(realX);
                }

                double maxY = (from el in computedYs where !double.IsInfinity(el) select el).Max();

                PathFigure fig = new PathFigure();
                PathFigure fillFig = new PathFigure() { StartPoint = new Point(0, 170) };

                bool startedFig = false;

                for (int x = 0; x < 300; x++)
                {
                    if (!double.IsNaN(computedYs[x]))
                    {
                        if (!startedFig)
                        {
                            fig.StartPoint = new Point(x, 170 - Math.Min(1, computedYs[x] / maxY) * 150);
                            startedFig = true;
                        }
                        else
                        {
                            fig.Segments.Add(new LineSegment() { Point = new Point(x, 170 - Math.Min(1, computedYs[x] / maxY) * 150) });
                        }

                        fillFig.Segments.Add(new LineSegment() { Point = new Point(x, 170 - Math.Min(1, computedYs[x] / maxY) * 150) });
                    }
                    else
                    {
                        if (!startedFig)
                        {
                            fillFig.StartPoint = new Point(x, 170);
                        }
                    }
                }

                fillFig.Segments.Add(new LineSegment() { Point = new Point(299, 170) });
                fillFig.IsClosed = true;

                fig.IsClosed = false;

                PathGeometry geo = new PathGeometry();
                geo.Figures.Add(fig);

                PathGeometry fillGeo = new PathGeometry();
                fillGeo.Figures.Add(fillFig);

                PathGeometry interestingPoints = new PathGeometry();

                {
                    PathFigure interestingFig = new PathFigure() { StartPoint = new Point(0, 190) };
                    interestingFig.Segments.Add(new LineSegment() { Point = new Point(0, 20) });
                    interestingPoints.Figures.Add(interestingFig);
                }

                {
                    PathFigure interestingFig = new PathFigure() { StartPoint = new Point(300, 190) };
                    interestingFig.Segments.Add(new LineSegment() { Point = new Point(300, 20) });
                    interestingPoints.Figures.Add(interestingFig);
                }

                int maxYX = computedYs.MaxInd();

                {
                    PathFigure interestingFig = new PathFigure() { StartPoint = new Point(maxYX, 170) };
                    interestingFig.Segments.Add(new LineSegment() { Point = new Point(maxYX, 0) });
                    interestingPoints.Figures.Add(interestingFig);
                }

                container.Children.Add(new Path() { Data = fillGeo, Fill = Program.GetBrush(Plotting.GetColor(ind, 0.25, distribNames.Length)) });
                container.Children.Add(new Path() { Data = geo, Stroke = Program.GetBrush(Plotting.GetColor(ind, 1, distribNames.Length)), StrokeThickness = 2, StrokeJoin = PenLineJoin.Round });

                container.Children.Add(new Path() { Data = interestingPoints, Stroke = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200)), StrokeThickness = 1.5 });

                container.Children.Add(new TextBlock() { Text = range[0].ToString(range[0] >= 100 ? 0 : 2, range[0] >= 100), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, Margin = new Thickness(5, 172, 0, 0) });

                {
                    FormattedText txt = new FormattedText() { Text = range[1].ToString(range[1] >= 100 ? 0 : 2, range[1] >= 100), Typeface = new Typeface(this.FontFamily, this.FontSize) };
                    container.Children.Add(new TextBlock() { Text = range[1].ToString(range[1] >= 100 ? 0 : 2, range[1] >= 100), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, Margin = new Thickness(295 - txt.Bounds.Width, 172, 0, 0) });
                }


                double realMaxYX = minX + (double)maxYX / 300.0 * (maxX - minX);

                if (maxYX <= 150)
                {
                    container.Children.Add(new TextBlock() { Text = realMaxYX.ToString(realMaxYX >= 100 ? 0 : 2, realMaxYX >= 100), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, Margin = new Thickness(maxYX + 5, 0, 0, 0) });
                }
                else
                {
                    {
                        FormattedText txt = new FormattedText() { Text = realMaxYX.ToString(realMaxYX >= 100 ? 0 : 2, realMaxYX >= 100), Typeface = new Typeface(this.FontFamily, this.FontSize) };
                        container.Children.Add(new TextBlock() { Text = realMaxYX.ToString(realMaxYX >= 100 ? 0 : 2, realMaxYX >= 100), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, Margin = new Thickness(maxYX - 5 - txt.Bounds.Width, 0, 0, 0) });
                    }
                }



            }
            catch (Exception e)
            {
                container.Background = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
                Grid grd = new Grid() { Width = 280, Height = 170, Margin = new Thickness(10) };
                container.Children.Add(grd);
                grd.Children.Add(new TextBlock() { VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Text = e.Message, TextWrapping = TextWrapping.Wrap });
            }
        }

        private void BayesianPriorChanged(object sender, SelectionChangedEventArgs e)
        {
            if (initialized)
            {
                BuildBayesianParameters();
            }
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OKClicked(object sender, RoutedEventArgs e)
        {
            if (this.FindControl<RadioButton>("FixRadio").IsChecked == true)
            {
                Rate.Action = Parameter.ParameterAction.Fix;
                Rate.Value = this.FindControl<NumericUpDown>("FixParameterValue").Value;
            }
            else if (this.FindControl<RadioButton>("MLRadio").IsChecked == true)
            {
                Rate.Action = Parameter.ParameterAction.ML;
            }
            else if (this.FindControl<RadioButton>("EqualRadio").IsChecked == true)
            {
                Rate.Action = Parameter.ParameterAction.Equal;
                Rate.EqualParameter = AllRates[equalParams[this.FindControl<ComboBox>("EqualParameterComboBox").SelectedIndex]];
            }
            else if (this.FindControl<RadioButton>("BayesianRadio").IsChecked == true)
            {
                Rate.Action = Parameter.ParameterAction.Bayes;
                int ind = this.FindControl<ComboBox>("BayesianParameterComboBox").SelectedIndex;
                string distribution = distribNames[ind] + "(" + (from el in parameterBoxes select el.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)).Aggregate((a, b) => a + "," + b) + ")";
                Rate.PriorDistribution = Utils.Utils.ParseDistribution(distribution, new ThreadSafeRandom());
            }

            this.Close();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            initialized = true;
        }
    }
}
