using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SlimTreeNode;
using System.Collections.Generic;
using Utils;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using System.Linq;
using System;
using Avalonia.Interactivity;
using System.IO;
using System.Threading;
using Avalonia.Threading;
using System.Threading.Tasks;

namespace sMap_GUI
{
    public class PlotSMapWindow : Window
    {
        public PlotSMapWindow()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        SerializedRun Run;

        bool[] marginalActiveCharacters;

        string[] activeStateNames;

        byte[] gridColour = new byte[] { 200, 200, 200 };
        byte[][] stateColours;

        string[] fontNames = new string[]
        {
            "Helvetica", "Helvetica-Bold", "Helvetica-Italic", "Helvetica-BoldItalic",
            "Courier", "Courier-Bold", "Courier-Italic", "Courier-BoldItalic",
            "Times-Roman", "Times-Bold", "Times-Italic", "Times-BoldItalic",
            "OpenSans-Regular.ttf", "OpenSans-Bold.ttf", "OpenSans-Italic.ttf", "OpenSans-BoldItalic.ttf",
            "Custom"
        };

        public PlotSMapWindow(SerializedRun run)
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            Run = run;

            string[] States = Run.States;
            TreeNode Tree = Run.SummaryTree;


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

            this.FindControl<NumericUpDown>("PlotWidthBox").Value = Math.Round(plotWidth);
            this.FindControl<NumericUpDown>("PlotHeightBox").Value = Math.Round(plotHeight);

            marginalActiveCharacters = new bool[Run.States[0].Split(',').Length];

            for (int i = 0; i < Run.States[0].Split(',').Length; i++)
            {
                int j = i;

                CheckBox cb = new CheckBox() { Content = i.ToString(), Margin = new Thickness(0, 0, 10, 0), IsChecked = true };

                cb.PropertyChanged += (s, e) =>
                {
                    if (e.Property == CheckBox.IsCheckedProperty)
                    {
                        marginalActiveCharacters[j] = cb.IsChecked == true;
                        updateActiveStateNames();
                    }
                };

                this.FindControl<StackPanel>("ActiveCharactersContainer").Children.Add(cb);
                marginalActiveCharacters[i] = true;
            }

            stateColours = new byte[Run.States.Length][];
            activeStateNames = new string[Run.States.Length];

            for (int i = 0; i < Run.States.Length; i++)
            {
                (int r, int g, int b, double a) col = Utils.Plotting.GetColor(i, 1, Run.States.Length);
                stateColours[i] = new byte[] { (byte)col.r, (byte)col.g, (byte)col.b };
                activeStateNames[i] = Run.States[i];
            }

            updateStateColours();

            this.FindControl<NumericUpDown>("TreeAgeScaleBox").Value = run.AgeScale;

            //Workaround Avalonia bug
            async void resize()
            {
                await System.Threading.Tasks.Task.Delay(100);
                this.Height = this.Height + 1;
            };

            resize();
        }

        private void updateActiveStateNames()
        {
            List<string[]> activeStates = new List<string[]>();

            for (int i = 0; i < marginalActiveCharacters.Length; i++)
            {
                if (marginalActiveCharacters[i])
                {
                    activeStates.Add(new HashSet<string>(from el in Run.States select el.Split(',')[i]).ToArray());
                }
            }

            activeStateNames = (from el in Utils.Utils.GetCombinations(activeStates.ToArray()) select Utils.Utils.StringifyArray(el)).ToArray();

            stateColours = new byte[activeStateNames.Length][];

            for (int i = 0; i < activeStateNames.Length; i++)
            {
                (int r, int g, int b, double a) col = Plotting.GetColor(i, 1, activeStateNames.Length);
                stateColours[i] = new byte[] { (byte)col.r, (byte)col.g, (byte)col.b };
            }

            updateStateColours();
        }

        private void updateStateColours()
        {
            this.FindControl<StackPanel>("StateColoursContainer").Children.Clear();

            for (int i = 0; i < activeStateNames.Length; i++)
            {
                StackPanel pnl = new StackPanel() { Orientation = Avalonia.Layout.Orientation.Horizontal };

                Border brd = new Border() { Width = 20, Height = 20, BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new Thickness(1.5), Background = Program.GetBrush((stateColours[i][0], stateColours[i][1], stateColours[i][2], 1)), CornerRadius = new CornerRadius(10, 10, 10, 10) };

                pnl.Children.Add(brd);

                pnl.Children.Add(new TextBlock() { Text = activeStateNames[i], Margin = new Thickness(10, 0, 0, 0) });

                Button btn = new Button() { Content = pnl, Margin = new Thickness(0, 0, 10, 0) };

                int j = i;

                btn.Click += async (s, e) =>
                {
                    ColorDialog dialog = new ColorDialog(stateColours[j][0], stateColours[j][1], stateColours[j][2]);

                    await dialog.ShowDialog(this);

                    if (dialog.Colour != null)
                    {
                        brd.Background = Program.GetBrush(dialog.Colour.Value);

                        stateColours[j] = new byte[] { dialog.Colour.Value.r, dialog.Colour.Value.g, dialog.Colour.Value.b };
                    }
                };

                this.FindControl<StackPanel>("StateColoursContainer").Children.Add(btn);
            }
        }

        bool initialised = false;

        bool changing = false;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            initialised = true;
        }

        private async void FontFamilySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (initialised && !changing)
            {
                if (this.FindControl<ComboBox>("FontFamilyBox").SelectedIndex == 16)
                {
                    changing = true;

                    OpenFileDialog dialog;

                    if (!Program.IsMac)
                    {

                        dialog = new OpenFileDialog() { Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Extensions = Program.IsMac ? new List<string>() { "*" } : new List<string>() { "ttf" }, Name = "TrueType Font" }, new FileDialogFilter() { Extensions = new List<string>() { "*" }, Name = "All files" } }, AllowMultiple = false, Title = "Choose font file" };
                    }
                    else
                    {
                        dialog = new OpenFileDialog() { AllowMultiple = false, Title = "Choose font file" };
                    }

                    string[] fileName = await dialog.ShowAsync(this);

                    if (fileName != null && fileName.Length > 0)
                    {
                        fontNames[16] = fileName[0];
                    }
                    changing = false;
                }
            }
        }

        private async void GridColourClicked(object sender, RoutedEventArgs e)
        {
            ColorDialog dialog = new ColorDialog(gridColour[0], gridColour[1], gridColour[2]);

            await dialog.ShowDialog(this);

            if (dialog.Colour != null)
            {
                this.FindControl<Border>("GridColourContainer").Background = Program.GetBrush(dialog.Colour.Value);

                gridColour = new byte[] { dialog.Colour.Value.r, dialog.Colour.Value.g, dialog.Colour.Value.b };
            }
        }


        private async void PlotPreviewClicked(object sender, RoutedEventArgs e)
        {
            string tempFileName = System.IO.Path.GetTempFileName() + ".pdf";

            sMap.Program.RunningGUI = true;

            PlotProgressWindow win = new PlotProgressWindow();

            Utils.Utils.Trigger = async (status, data) =>
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {

                    if (status == "BranchProgress")
                    {
                        win.Progress = 0.5 * (double)data[0];
                    }
                    else if (status == "NodeProgress")
                    {
                        win.Progress = 0.5 + 0.5 * (double)data[0];
                    }
                    else if (status == "PlottingFinished")
                    {
                        win.Close();
                    }
                });
            };

            int index = this.FindControl<ComboBox>("PlotTargetBox").SelectedIndex;

            Thread thr = new Thread(async () =>
           {
               await Plot(tempFileName, index);
           });

            thr.Start();

            await win.ShowDialog(this);

            PlotPreviewWindow prev = new PlotPreviewWindow(tempFileName);

            await prev.ShowDialog(this);
        }

        async Task Plot(string outputFileName, int index)
        {
            switch (index)
            {
                case 0:
                    await PlotTree(outputFileName);
                    break;
                case 1:
                    await PlotPosteriors(outputFileName);
                    break;
                case 2:
                    await PlotPriors(outputFileName);
                    break;
                case 3:
                    await PlotConditionedProbs(outputFileName);
                    break;
                case 4:
                    await PlotSMap(outputFileName);
                    break;
                case 5:
                    await PlotSampleSizes(outputFileName);
                    break;
            }
        }

        async Task PlotTree(string outputFileName)
        {
            string realFontFamily = "";
            Plotting.Options opt = null;
            float plotWidth = 0, plotHeight = 0, margins = 0;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                plotWidth = (float)this.FindControl<NumericUpDown>("PlotWidthBox").Value;
                plotHeight = (float)this.FindControl<NumericUpDown>("PlotHeightBox").Value;
                margins = (float)this.FindControl<NumericUpDown>("MarginsBox").Value;

                string fontFamily = fontNames[this.FindControl<ComboBox>("FontFamilyBox").SelectedIndex];

                if (this.FindControl<ComboBox>("FontFamilyBox").SelectedIndex < 12)
                {
                    realFontFamily = fontFamily;
                }
                else
                {
                    if (File.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), fontFamily)))
                    {
                        realFontFamily = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), fontFamily);
                    }
                    else if (File.Exists(fontFamily))
                    {
                        realFontFamily = fontFamily;
                    }
                    else
                    {
                        realFontFamily = System.IO.Path.Combine(Directory.GetCurrentDirectory(), fontFamily);
                    }
                }

                opt = new Plotting.Options()
                {
                    FontFamily = realFontFamily,
                    FontSize = (float)this.FindControl<NumericUpDown>("FontSizeBox").Value,
                    NodeNumbers = this.FindControl<CheckBox>("NodeIdsBox").IsChecked == true,
                    LineWidth = (float)this.FindControl<NumericUpDown>("LineWidthBox").Value,
                    PieSize = 0,
                    ScaleAxis = this.FindControl<CheckBox>("ScaleAxisBox").IsChecked == true,
                    ScaleSpacing = (float)this.FindControl<NumericUpDown>("ScaleSpacingBox").Value,
                    ScaleGrid = this.FindControl<CheckBox>("ScaleGridBox").IsChecked == true,
                    GridSpacing = (float)this.FindControl<NumericUpDown>("GridSpacingBox").Value,
                    GridColour = gridColour,
                    GridWidth = (float)this.FindControl<NumericUpDown>("GridWidthBox").Value,
                    TreeScale = (float)this.FindControl<NumericUpDown>("TreeAgeScaleBox").Value,
                    SignificantDigits = (int)this.FindControl<NumericUpDown>("ScaleDigitsBox").Value
                };
            });

            Run.SummaryTree.PlotSimpleTree(plotWidth, plotHeight, margins, outputFileName, opt, true);
        }

        async Task PlotPriors(string outputFileName)
        {
            string realFontFamily = "";
            Plotting.Options opt = null;
            float plotWidth = 0, plotHeight = 0, margins = 0;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                plotWidth = (float)this.FindControl<NumericUpDown>("PlotWidthBox").Value;
                plotHeight = (float)this.FindControl<NumericUpDown>("PlotHeightBox").Value;
                margins = (float)this.FindControl<NumericUpDown>("MarginsBox").Value;
                string fontFamily = fontNames[this.FindControl<ComboBox>("FontFamilyBox").SelectedIndex];


                if (this.FindControl<ComboBox>("FontFamilyBox").SelectedIndex < 12)
                {
                    realFontFamily = fontFamily;
                }
                else
                {
                    if (File.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), fontFamily)))
                    {
                        realFontFamily = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), fontFamily);
                    }
                    else if (File.Exists(fontFamily))
                    {
                        realFontFamily = fontFamily;
                    }
                    else
                    {
                        realFontFamily = System.IO.Path.Combine(Directory.GetCurrentDirectory(), fontFamily);
                    }
                }

                opt = new Plotting.Options()
                {
                    FontFamily = realFontFamily,
                    FontSize = (float)this.FindControl<NumericUpDown>("FontSizeBox").Value,
                    NodeNumbers = this.FindControl<CheckBox>("NodeIdsBox").IsChecked == true,
                    LineWidth = (float)this.FindControl<NumericUpDown>("LineWidthBox").Value,
                    PieSize = (float)this.FindControl<NumericUpDown>("PieSizeBox").Value,
                    ScaleAxis = this.FindControl<CheckBox>("ScaleAxisBox").IsChecked == true,
                    ScaleSpacing = (float)this.FindControl<NumericUpDown>("ScaleSpacingBox").Value,
                    ScaleGrid = this.FindControl<CheckBox>("ScaleGridBox").IsChecked == true,
                    GridSpacing = (float)this.FindControl<NumericUpDown>("GridSpacingBox").Value,
                    GridColour = gridColour,
                    GridWidth = (float)this.FindControl<NumericUpDown>("GridWidthBox").Value,
                    TreeScale = (float)this.FindControl<NumericUpDown>("TreeAgeScaleBox").Value,
                    SignificantDigits = (int)this.FindControl<NumericUpDown>("ScaleDigitsBox").Value,
                    StateColours = stateColours
                };
            });

            Run.SummaryTree.PlotTreeWithPies(plotWidth, plotHeight, margins, outputFileName, opt, GetMarginalProbabilities(Run.MeanPrior), activeStateNames, true);
        }

        double[][] GetMarginalProbabilities(double[][] meanProb)
        {
            List<string[]> activeStates = new List<string[]>();
            List<int> activeChars = new List<int>();

            for (int i = 0; i < marginalActiveCharacters.Length; i++)
            {
                if (marginalActiveCharacters[i])
                {
                    List<string> st = new List<string>();

                    for (int j = 0; j < Run.States.Length; j++)
                    {
                        if (!st.Contains(Run.States[j].Split(',')[i]))
                        {
                            st.Add(Run.States[j].Split(',')[i]);
                        }
                    }

                    activeStates.Add(st.ToArray());

                    activeChars.Add(i);
                }
            }

            string[][] stateCombinations = Utils.Utils.GetCombinations(activeStates.ToArray());

            double[][] tbr = new double[meanProb.Length][];

            for (int j = 0; j < meanProb.Length; j++)
            {
                tbr[j] = new double[stateCombinations.Length];
            }


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

                for (int j = 0; j < meanProb.Length; j++)
                {
                    tbr[j][i] = 0;

                    for (int k = 0; k < correspStates.Length; k++)
                    {
                        if (correspStates[k])
                        {
                            tbr[j][i] += meanProb[j][k];
                        }
                    }
                }
            }

            return tbr;
        }


        async Task PlotPosteriors(string outputFileName)
        {
            string realFontFamily = "";
            Plotting.Options opt = null;
            float plotWidth = 0, plotHeight = 0, margins = 0;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                plotWidth = (float)this.FindControl<NumericUpDown>("PlotWidthBox").Value;
                plotHeight = (float)this.FindControl<NumericUpDown>("PlotHeightBox").Value;
                margins = (float)this.FindControl<NumericUpDown>("MarginsBox").Value;

                string fontFamily = fontNames[this.FindControl<ComboBox>("FontFamilyBox").SelectedIndex];

                if (this.FindControl<ComboBox>("FontFamilyBox").SelectedIndex < 12)
                {
                    realFontFamily = fontFamily;
                }
                else
                {
                    if (File.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), fontFamily)))
                    {
                        realFontFamily = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), fontFamily);
                    }
                    else if (File.Exists(fontFamily))
                    {
                        realFontFamily = fontFamily;
                    }
                    else
                    {
                        realFontFamily = System.IO.Path.Combine(Directory.GetCurrentDirectory(), fontFamily);
                    }
                }

                opt = new Plotting.Options()
                {
                    FontFamily = realFontFamily,
                    FontSize = (float)this.FindControl<NumericUpDown>("FontSizeBox").Value,
                    NodeNumbers = this.FindControl<CheckBox>("NodeIdsBox").IsChecked == true,
                    LineWidth = (float)this.FindControl<NumericUpDown>("LineWidthBox").Value,
                    PieSize = (float)this.FindControl<NumericUpDown>("PieSizeBox").Value,
                    ScaleAxis = this.FindControl<CheckBox>("ScaleAxisBox").IsChecked == true,
                    ScaleSpacing = (float)this.FindControl<NumericUpDown>("ScaleSpacingBox").Value,
                    ScaleGrid = this.FindControl<CheckBox>("ScaleGridBox").IsChecked == true,
                    GridSpacing = (float)this.FindControl<NumericUpDown>("GridSpacingBox").Value,
                    GridColour = gridColour,
                    GridWidth = (float)this.FindControl<NumericUpDown>("GridWidthBox").Value,
                    TreeScale = (float)this.FindControl<NumericUpDown>("TreeAgeScaleBox").Value,
                    SignificantDigits = (int)this.FindControl<NumericUpDown>("ScaleDigitsBox").Value,
                    StateColours = stateColours
                };
            });

            Run.SummaryTree.PlotTreeWithPies(plotWidth, plotHeight, margins, outputFileName, opt, GetMarginalProbabilities(Run.MeanPosterior), activeStateNames, true);
        }

        async Task PlotConditionedProbs(string outputFileName)
        {
            string realFontFamily = "";
            Plotting.Options opt = null;
            float plotWidth = 0, plotHeight = 0, margins = 0;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                plotWidth = (float)this.FindControl<NumericUpDown>("PlotWidthBox").Value;
                plotHeight = (float)this.FindControl<NumericUpDown>("PlotHeightBox").Value;
                margins = (float)this.FindControl<NumericUpDown>("MarginsBox").Value;
                string fontFamily = fontNames[this.FindControl<ComboBox>("FontFamilyBox").SelectedIndex];

                if (this.FindControl<ComboBox>("FontFamilyBox").SelectedIndex < 12)
                {
                    realFontFamily = fontFamily;
                }
                else
                {
                    if (File.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), fontFamily)))
                    {
                        realFontFamily = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), fontFamily);
                    }
                    else if (File.Exists(fontFamily))
                    {
                        realFontFamily = fontFamily;
                    }
                    else
                    {
                        realFontFamily = System.IO.Path.Combine(Directory.GetCurrentDirectory(), fontFamily);
                    }
                }

                opt = new Plotting.Options()
                {
                    FontFamily = realFontFamily,
                    FontSize = (float)this.FindControl<NumericUpDown>("FontSizeBox").Value,
                    NodeNumbers = this.FindControl<CheckBox>("NodeIdsBox").IsChecked == true,
                    LineWidth = (float)this.FindControl<NumericUpDown>("LineWidthBox").Value,
                    PieSize = (float)this.FindControl<NumericUpDown>("PieSizeBox").Value,
                    ScaleAxis = this.FindControl<CheckBox>("ScaleAxisBox").IsChecked == true,
                    ScaleSpacing = (float)this.FindControl<NumericUpDown>("ScaleSpacingBox").Value,
                    ScaleGrid = this.FindControl<CheckBox>("ScaleGridBox").IsChecked == true,
                    GridSpacing = (float)this.FindControl<NumericUpDown>("GridSpacingBox").Value,
                    GridColour = gridColour,
                    GridWidth = (float)this.FindControl<NumericUpDown>("GridWidthBox").Value,
                    TreeScale = (float)this.FindControl<NumericUpDown>("TreeAgeScaleBox").Value,
                    SignificantDigits = (int)this.FindControl<NumericUpDown>("ScaleDigitsBox").Value,
                    StateColours = stateColours
                };
            });

            Run.SummaryTree.PlotTreeWithPies(plotWidth, plotHeight, margins, outputFileName, opt, GetMarginalProbabilities(GetConditionedProbabilities()), activeStateNames, true);
        }


        async Task PlotSMap(string outputFileName)
        {
            string realFontFamily = "";
            Plotting.Options opt = null;
            float plotWidth = 0, plotHeight = 0, margins = 0;
            double timeResolution = 0;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                plotWidth = (float)this.FindControl<NumericUpDown>("PlotWidthBox").Value;
                plotHeight = (float)this.FindControl<NumericUpDown>("PlotHeightBox").Value;
                margins = (float)this.FindControl<NumericUpDown>("MarginsBox").Value;
                timeResolution = this.FindControl<NumericUpDown>("TimeResolutionBox").Value;

                string fontFamily = fontNames[this.FindControl<ComboBox>("FontFamilyBox").SelectedIndex];

                if (this.FindControl<ComboBox>("FontFamilyBox").SelectedIndex < 12)
                {
                    realFontFamily = fontFamily;
                }
                else
                {
                    if (File.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), fontFamily)))
                    {
                        realFontFamily = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), fontFamily);
                    }
                    else if (File.Exists(fontFamily))
                    {
                        realFontFamily = fontFamily;
                    }
                    else
                    {
                        realFontFamily = System.IO.Path.Combine(Directory.GetCurrentDirectory(), fontFamily);
                    }
                }

                opt = new Plotting.Options()
                {
                    FontFamily = realFontFamily,
                    FontSize = (float)this.FindControl<NumericUpDown>("FontSizeBox").Value,
                    NodeNumbers = this.FindControl<CheckBox>("NodeIdsBox").IsChecked == true,
                    LineWidth = (float)this.FindControl<NumericUpDown>("LineWidthBox").Value,
                    PieSize = (float)this.FindControl<NumericUpDown>("PieSizeBox").Value,
                    ScaleAxis = this.FindControl<CheckBox>("ScaleAxisBox").IsChecked == true,
                    ScaleSpacing = (float)this.FindControl<NumericUpDown>("ScaleSpacingBox").Value,
                    ScaleGrid = this.FindControl<CheckBox>("ScaleGridBox").IsChecked == true,
                    GridSpacing = (float)this.FindControl<NumericUpDown>("GridSpacingBox").Value,
                    GridColour = gridColour,
                    GridWidth = (float)this.FindControl<NumericUpDown>("GridWidthBox").Value,
                    TreeScale = (float)this.FindControl<NumericUpDown>("TreeAgeScaleBox").Value,
                    SignificantDigits = (int)this.FindControl<NumericUpDown>("ScaleDigitsBox").Value,
                    StateColours = stateColours,
                    BranchSize = this.FindControl<NumericUpDown>("BranchWidthBox").Value
                };
            });

            Run.SummaryTree.PlotTreeWithPiesAndBranchStates(plotWidth, plotHeight, margins, outputFileName, opt, GetMarginalProbabilities(GetConditionedProbabilities()), GetMarginalHistories(), Run.TreeSamples, Run.LikelihoodModels, new LikelihoodModel(Run.SummaryTree), Run.SummaryNodeCorresp, timeResolution, new List<string>(activeStateNames), true);

        }

        async Task PlotSampleSizes(string outputFileName)
        {
            string realFontFamily = "";
            Plotting.Options opt = null;
            float plotWidth = 0, plotHeight = 0, margins = 0;
            double timeResolution = 0;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                plotWidth = (float)this.FindControl<NumericUpDown>("PlotWidthBox").Value;
                plotHeight = (float)this.FindControl<NumericUpDown>("PlotHeightBox").Value;
                margins = (float)this.FindControl<NumericUpDown>("MarginsBox").Value;
                timeResolution = this.FindControl<NumericUpDown>("TimeResolutionBox").Value;
                string fontFamily = fontNames[this.FindControl<ComboBox>("FontFamilyBox").SelectedIndex];

                if (this.FindControl<ComboBox>("FontFamilyBox").SelectedIndex < 12)
                {
                    realFontFamily = fontFamily;
                }
                else
                {
                    if (File.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), fontFamily)))
                    {
                        realFontFamily = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), fontFamily);
                    }
                    else if (File.Exists(fontFamily))
                    {
                        realFontFamily = fontFamily;
                    }
                    else
                    {
                        realFontFamily = System.IO.Path.Combine(Directory.GetCurrentDirectory(), fontFamily);
                    }
                }

                opt = new Plotting.Options()
                {
                    FontFamily = realFontFamily,
                    FontSize = (float)this.FindControl<NumericUpDown>("FontSizeBox").Value,
                    NodeNumbers = this.FindControl<CheckBox>("NodeIdsBox").IsChecked == true,
                    LineWidth = (float)this.FindControl<NumericUpDown>("LineWidthBox").Value,
                    PieSize = (float)this.FindControl<NumericUpDown>("PieSizeBox").Value,
                    ScaleAxis = this.FindControl<CheckBox>("ScaleAxisBox").IsChecked == true,
                    ScaleSpacing = (float)this.FindControl<NumericUpDown>("ScaleSpacingBox").Value,
                    ScaleGrid = this.FindControl<CheckBox>("ScaleGridBox").IsChecked == true,
                    GridSpacing = (float)this.FindControl<NumericUpDown>("GridSpacingBox").Value,
                    GridColour = gridColour,
                    GridWidth = (float)this.FindControl<NumericUpDown>("GridWidthBox").Value,
                    TreeScale = (float)this.FindControl<NumericUpDown>("TreeAgeScaleBox").Value,
                    SignificantDigits = (int)this.FindControl<NumericUpDown>("ScaleDigitsBox").Value,
                    StateColours = stateColours,
                    BranchSize = this.FindControl<NumericUpDown>("BranchWidthBox").Value
                };
            });

            Run.SummaryTree.PlotTreeWithBranchSampleSizes(plotWidth, plotHeight, margins, outputFileName, opt, Run.Histories, Run.TreeSamples, Run.LikelihoodModels, new LikelihoodModel(Run.SummaryTree), Run.SummaryNodeCorresp, timeResolution, new List<string>(activeStateNames), true);

        }

        TaggedHistory[] GetMarginalHistories()
        {
            List<string[]> activeStates = new List<string[]>();
            List<int> activeChars = new List<int>();

            for (int i = 0; i < marginalActiveCharacters.Length; i++)
            {
                if (marginalActiveCharacters[i])
                {
                    List<string> st = new List<string>();

                    for (int j = 0; j < Run.States.Length; j++)
                    {
                        if (!st.Contains(Run.States[j].Split(',')[i]))
                        {
                            st.Add(Run.States[j].Split(',')[i]);
                        }
                    }

                    activeStates.Add(st.ToArray());

                    activeChars.Add(i);
                }
            }

            string[][] stateCombinations = Utils.Utils.GetCombinations(activeStates.ToArray());

            int[] invCorrespStates = new int[Run.States.Length];

            Dictionary<string, int> statesInds = new Dictionary<string, int>();

            for (int i = 0; i < Run.States.Length; i++)
            {
                statesInds.Add(Run.States[i], i);

                string[] splitState = Run.States[i].Split(',');

                for (int j = 0; j < stateCombinations.Length; j++)
                {
                    bool foundDiff = false;
                    for (int k = 0; k < stateCombinations[j].Length; k++)
                    {
                        if (stateCombinations[j][k] != splitState[activeChars[k]])
                        {
                            foundDiff = true;
                            break;
                        }
                    }
                    if (!foundDiff)
                    {
                        invCorrespStates[i] = j;
                        break;
                    }
                }
            }

            TaggedHistory[] marginalHistories = new TaggedHistory[Run.Histories.Length];

            for (int l = 0; l < Run.Histories.Length; l++)
            {
                marginalHistories[l] = new TaggedHistory(Run.Histories[l].Tag, Utils.Simulation.GetMarginalHistory(Run.Histories[l].History, invCorrespStates, (from el in stateCombinations select Utils.Utils.StringifyArray(el)).ToArray(), statesInds));
            }

            return marginalHistories;
        }

        double[][] GetConditionedProbabilities()
        {
            bool isClockLike = Run.TreesClockLike;

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


        private async void SaveSettingsClicked(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog() { Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Name = "Plot settings file", Extensions = new List<string>() { "plot" } }, new FileDialogFilter() { Name = "All files", Extensions = new List<string>() { "*" } } }, Title = "Save plot settings" };

            string result = await dialog.ShowAsync(this);

            if (!string.IsNullOrEmpty(result))
            {
                SaveSettings(result);
            }
        }

        private void SaveSettings(string settingsFileName)
        {
            using (StreamWriter sw = new StreamWriter(settingsFileName))
            {
                sw.WriteLine("--target");

                switch (this.FindControl<ComboBox>("PlotTargetBox").SelectedIndex)
                {
                    case 0:
                        sw.WriteLine("Tree");
                        break;
                    case 1:
                        sw.WriteLine("MeanPosteriors");
                        break;
                    case 2:
                        sw.WriteLine("MeanPriors");
                        break;
                    case 3:
                        sw.WriteLine("MeanCondPosteriors");
                        break;
                    case 4:
                        sw.WriteLine("StochasticMap");
                        break;
                    case 5:
                        sw.WriteLine("SampleSizes");
                        break;
                }

                sw.WriteLine("--width");
                sw.WriteLine(this.FindControl<NumericUpDown>("PlotWidthBox").Value.ToString(System.Globalization.CultureInfo.InvariantCulture));

                sw.WriteLine("--height");
                sw.WriteLine(this.FindControl<NumericUpDown>("PlotHeightBox").Value.ToString(System.Globalization.CultureInfo.InvariantCulture));

                sw.WriteLine("--margins");
                sw.WriteLine(this.FindControl<NumericUpDown>("MarginsBox").Value.ToString(System.Globalization.CultureInfo.InvariantCulture));

                sw.WriteLine("--font-size");
                sw.WriteLine(this.FindControl<NumericUpDown>("FontSizeBox").Value.ToString(System.Globalization.CultureInfo.InvariantCulture));

                sw.WriteLine("--font-family");
                sw.WriteLine(fontNames[this.FindControl<ComboBox>("FontFamilyBox").SelectedIndex]);

                sw.WriteLine("--pie-size");
                sw.WriteLine(this.FindControl<NumericUpDown>("PieSizeBox").Value.ToString(System.Globalization.CultureInfo.InvariantCulture));

                sw.WriteLine("--branch-width");
                sw.WriteLine(this.FindControl<NumericUpDown>("BranchWidthBox").Value.ToString(System.Globalization.CultureInfo.InvariantCulture));

                sw.WriteLine("--line-width");
                sw.WriteLine(this.FindControl<NumericUpDown>("LineWidthBox").Value.ToString(System.Globalization.CultureInfo.InvariantCulture));

                if (this.FindControl<CheckBox>("NodeIdsBox").IsChecked == true)
                {
                    sw.WriteLine("--node-ids");
                }

                if (this.FindControl<CheckBox>("ScaleAxisBox").IsChecked == true)
                {
                    sw.WriteLine("--scale-axis");
                }

                sw.WriteLine("--scale-spacing");
                sw.WriteLine(this.FindControl<NumericUpDown>("ScaleSpacingBox").Value.ToString(System.Globalization.CultureInfo.InvariantCulture));

                if (this.FindControl<CheckBox>("ScaleGridBox").IsChecked == true)
                {
                    sw.WriteLine("--scale-grid");
                }

                sw.WriteLine("--grid-spacing");
                sw.WriteLine(this.FindControl<NumericUpDown>("GridSpacingBox").Value.ToString(System.Globalization.CultureInfo.InvariantCulture));

                sw.WriteLine("--grid-line-width");
                sw.WriteLine(this.FindControl<NumericUpDown>("GridWidthBox").Value.ToString(System.Globalization.CultureInfo.InvariantCulture));

                sw.WriteLine("--grid-colour");
                sw.WriteLine(Utils.Utils.StringifyArray(gridColour, ","));

                sw.WriteLine("--digits");
                sw.WriteLine(((int)this.FindControl<NumericUpDown>("ScaleDigitsBox").Value).ToString());

                sw.WriteLine("--age-scale");
                sw.WriteLine(this.FindControl<NumericUpDown>("TreeAgeScaleBox").Value.ToString(System.Globalization.CultureInfo.InvariantCulture));

                sw.WriteLine("--resolution");
                sw.WriteLine(this.FindControl<NumericUpDown>("TimeResolutionBox").Value.ToString(System.Globalization.CultureInfo.InvariantCulture));

                sw.WriteLine("--active-characters");
                string activeChars = "";
                for (int i = 0; i < marginalActiveCharacters.Length; i++)
                {
                    if (marginalActiveCharacters[i])
                    {
                        activeChars += i.ToString() + ",";
                    }
                }
                sw.WriteLine(activeChars.Substring(0, activeChars.Length - 1));

                sw.WriteLine("--state-colours");
                string colourStr = "";

                for (int i = 0; i < stateColours.Length; i++)
                {
                    colourStr += "[" + Utils.Utils.StringifyArray(stateColours[i], ",") + "]:";
                }

                sw.WriteLine(colourStr.Substring(0, colourStr.Length - 1));
            }
        }

        private async void LoadSettingsClicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog;

            if (!Program.IsMac)
            {
                dialog = new OpenFileDialog() { Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Name = "Plot settings file", Extensions = Program.IsMac ? new List<string>() { "*" } : new List<string>() { "plot" } }, new FileDialogFilter() { Name = "All files", Extensions = new List<string>() { "*" } } }, Title = "Load plot settings", AllowMultiple = false };
            }
            else
            {
                dialog = new OpenFileDialog() { Title = "Load plot settings", AllowMultiple = false };
            }


            string[] result = await dialog.ShowAsync(this);

            if (result != null && result.Length > 0)
            {
                await LoadSettings(result[0]);
            }
        }

        async Task LoadSettings(string settingsFileName)
        {
            Mono.Options.OptionSet argParser = new Mono.Options.OptionSet()
            {
                { "t|target=", "Optional. Plot target. Available targets are: Tree, MeanPriors, MeanPosteriors, MeanCondPosteriors, StochasticMap, SampleSizes. Default: StochasticMap.", v => {
                    switch (v.ToLower())
                    {
                        case "tree":
                            this.FindControl<ComboBox>("PlotTargetBox").SelectedIndex = 0;
                            break;
                        case "meanposteriors":
                            this.FindControl<ComboBox>("PlotTargetBox").SelectedIndex = 1;
                            break;
                        case "meanpriors":
                            this.FindControl<ComboBox>("PlotTargetBox").SelectedIndex = 2;
                            break;
                        case "meancondposteriors":
                            this.FindControl<ComboBox>("PlotTargetBox").SelectedIndex = 3;
                            break;
                        case "stochasticmap":
                            this.FindControl<ComboBox>("PlotTargetBox").SelectedIndex = 4;
                            break;
                        case "samplesizes":
                            this.FindControl<ComboBox>("PlotTargetBox").SelectedIndex = 5;
                            break;
                    }
                } },
                { "w|width=", "Optional. Plot width in points. Default: 500.", v => { this.FindControl<NumericUpDown>("PlotWidthBox").Value = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "H|height=", "Optional. Plot height in points. Default: determined by the number of taxa.", v => { this.FindControl<NumericUpDown>("PlotHeightBox").Value = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "m|margins=", "Optional. Plot margins in points. Default: 10.", v => { this.FindControl<NumericUpDown>("MarginsBox").Value = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "f|font-size=", "Optional. Font size in points. Default: 12.", v => { this.FindControl<NumericUpDown>("FontSizeBox").Value = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "F|font-family=", "Optional. Font family. This should be a standard font family, or the path to a TrueText font file (this path can be absolute, relative to the location of this executable, or relative to the current working directory. Available standard font families are: Helvetica, Helvetica-Bold, Helvetica-Oblique, Helvetica-BoldOblique, Courier, Courier-Bold, Courier-Oblique, Courier-BoldOblique, Times-Roman, Times-Bold, Times-Italic, Times-BoldItalic, OpenSans-Regular.ttf, OpenSans-Bold.ttf, OpenSans-Italic.ttf, OpenSans-BoldItalic.ttf. Default: OpenSans-Regular.ttf.", v => {

                    changing = true;

                    if (fontNames.Contains(v))
                    {
                        this.FindControl<ComboBox>("FontFamilyBox").SelectedIndex = fontNames.IndexOf(v);
                    }
                    else
                    {
                        fontNames[16] = v;
                        this.FindControl<ComboBox>("FontFamilyBox").SelectedIndex = 16;
                    }

                    changing = false;

                } },
                { "p|pie-size=", "Optional. Plot pie radius in points. Default: 5.", v => { this.FindControl<NumericUpDown>("PieSizeBox").Value = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "B|branch-width=", "Optional. Branch width in points (only for stochastic map plots). Default: 3.", v => { this.FindControl<NumericUpDown>("BranchWidthBox").Value = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "l|line-width=", "Optional. Plot line width in points. Default: 1.", v => { this.FindControl<NumericUpDown>("LineWidthBox").Value = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "n|node-ids", "Optional. Show node ids in the plot. Default: off.", v => { this.FindControl<CheckBox>("NodeIdsBox").IsChecked = v != null; } },
                { "x|scale-axis", "Optional. Show scale axis at the bottom of the plot. Default: off.", v => { this.FindControl<CheckBox>("ScaleAxisBox").IsChecked = v != null; } },
                { "S|scale-spacing=", "Optional. Scale axis spacing. Default: tree_height / 5.", v => { this.FindControl<NumericUpDown>("ScaleSpacingBox").Value = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "g|scale-grid", "Optional. Show scale grid behind the plot. Only works when the scale axis is also enabled. Default: off.", v => { this.FindControl<CheckBox>("ScaleGridBox").IsChecked = v != null; } },
                { "G|grid-spacing=", "Optional. Scale grid spacing. Default: tree_height / 10.", v => { this.FindControl<NumericUpDown>("GridSpacingBox").Value = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "gw|grid-line-width=", "Optional. Scale grid line width. Default: 0.5.", v => { this.FindControl<NumericUpDown>("GridWidthBox").Value = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "gc|grid-colour=", "Optional. Scale grid line colour, in r,g,b format. Default: 200,200,200.", v => { gridColour = (from el in v.Split(',') select byte.Parse(el, System.Globalization.CultureInfo.InvariantCulture)).ToArray(); this.FindControl<Border>("GridColourContainer").Background = Program.GetBrush((gridColour[0], gridColour[1], gridColour[2], 1));} },
                { "d|digits=", "Optional. Significant digits on the scale axis. Default: 3.", v => { this.FindControl<NumericUpDown>("ScaleDigitsBox").Value = int.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "a|age-scale=", "Optional. Tree age scale multiplier. Default: same value used to normalise the tree heights in sMap.", v => { this.FindControl<NumericUpDown>("TreeAgeScaleBox").Value = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "r|resolution=", "Optional. Branch time resolution. Default: plot_width / 250.", v => { this.FindControl<NumericUpDown>("TimeResolutionBox").Value = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "c|active-characters=", "Optional. Comma-separated list of active characters (e.g. 0,1). Default: all.", v => {
                    if (v.ToLower() == "all")
                    {
                        marginalActiveCharacters = new bool[Run.States[0].Split(',').Length];
                        for (int i = 0; i < marginalActiveCharacters.Length; i++)
                        {
                            marginalActiveCharacters[i] = true;
                            ((CheckBox)this.FindControl<StackPanel>("ActiveCharactersContainer").Children[i]).IsChecked = true;
                        }

                        updateActiveStateNames();
                    }
                    else
                    {
                        int[] activeChars = (from el in v.Split(',') select int.Parse(el, System.Globalization.CultureInfo.InvariantCulture)).ToArray();

                        marginalActiveCharacters = new bool[Run.States[0].Split(',').Length];

                        for (int i = 0; i < marginalActiveCharacters.Length; i++)
                        {
                            if (activeChars.Contains(i))
                            {
                                marginalActiveCharacters[i] = true;
                                ((CheckBox)this.FindControl<StackPanel>("ActiveCharactersContainer").Children[i]).IsChecked = true;
                            }
                            else
                            {
                                ((CheckBox)this.FindControl<StackPanel>("ActiveCharactersContainer").Children[i]).IsChecked = false;
                            }
                        }

                        updateActiveStateNames();
                    }
                } },
                { "C|state-colours=", "Optional. Colon-separated list of colours in [r,g,b] format (e.g. [255,0,0]:[0,255,255]). Default: auto (determined by the number of states).", v => {
                    if (v.ToLower() == "auto")
                    {
                        stateColours = new byte[activeStateNames.Length][];

                        for (int i = 0; i < activeStateNames.Length; i++)
                        {
                            (int r, int g, int b, double a) col = Plotting.GetColor(i, 1, activeStateNames.Length);
                            stateColours[i] = new byte[] { (byte)col.r, (byte)col.g, (byte)col.b };
                        }
                    }
                    else
                    {
                        string[] colours = v.Split(':');
                        stateColours = new byte[colours.Length][];

                        for (int i = 0; i < colours.Length; i++)
                        {
                            string col = colours[i].Substring(colours[i].IndexOf("[") + 1);
                            col = col.Substring(0, col.IndexOf("]"));
                            stateColours[i] = (from el in col.Split(',') select byte.Parse(el, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                        }
                    }

                    updateStateColours();
                } }
            };

            List<string> unrecognisedParsed = argParser.Parse(File.ReadAllLines(settingsFileName));

            if (unrecognisedParsed.Count > 0)
            {
                MessageBox box = new MessageBox("Warning", "Unrecognised setting" + (unrecognisedParsed.Count > 1 ? "s" : "") + ": " + Utils.Utils.StringifyArray(unrecognisedParsed, " "));

                await box.ShowDialog(this);
            }
        }
    }
}
