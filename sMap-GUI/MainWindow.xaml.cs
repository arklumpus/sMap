using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace sMap_GUI
{
    public class MainWindow : Window
    {
        public static string Version = "1.0.7";

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            this.FindControl<TextBlock>("VersionNumber").Text = "v" + Version + " (sMap v" + sMap.Program.Version + ")";
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        static List<T> GetAllChildren<T>(ILogical parent)
        {
            List<T> tbr = new List<T>();
            if (parent is T)
            {
                tbr.Add((T)parent);
            }

            foreach (ILogical l in parent.LogicalChildren)
            {
                tbr.AddRange(GetAllChildren<T>(l));
            }

            return tbr;
        }

        private async void BrowseDataFileClicked(object sender, RoutedEventArgs e)
        {


            OpenFileDialog dialog;

            if (!Program.IsMac)
            {
                dialog = new OpenFileDialog()
                {
                    Title = "Choose data file",
                    AllowMultiple = false,
                    Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Name = "All files", Extensions = new List<string>() { "*" } } }
                };
            }
            else
            {
                dialog = new OpenFileDialog()
                {
                    Title = "Choose data file",
                    AllowMultiple = false
                };
            }

            string[] result = await dialog.ShowAsync(this);

            if (result != null && result.Length == 1)
            {
                this.FindControl<TextBox>("DataFileBox").Text = GetRelativePath(result[0], Directory.GetCurrentDirectory());
            }
        }

        private async void BrowseTreeFileClicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog;

            if (!Program.IsMac)
            {
                dialog = new OpenFileDialog()
                {
                    Title = "Choose tree file",
                    AllowMultiple = false,
                    Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Name = "All files", Extensions = new List<string>() { "*" } } }
                };
            }
            else
            {
                dialog = new OpenFileDialog()
                {
                    Title = "Choose tree file",
                    AllowMultiple = false
                };
            }


            string[] result = await dialog.ShowAsync(this);

            if (result != null && result.Length == 1)
            {
                this.FindControl<TextBox>("TreeFileBox").Text = GetRelativePath(result[0], Directory.GetCurrentDirectory());
            }
        }

        private async void BrowseMeanTreeFileClicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog;

            if (!Program.IsMac)
            {
                dialog = new OpenFileDialog()
                {
                    Title = "Choose mean tree file",
                    AllowMultiple = false,
                    Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Name = "All files", Extensions = new List<string>() { "*" } } }
                };
            }
            else
            {
                dialog = new OpenFileDialog()
                {
                    Title = "Choose mean tree file",
                    AllowMultiple = false
                };
            }

            string[] result = await dialog.ShowAsync(this);

            if (result != null && result.Length == 1)
            {
                this.FindControl<TextBox>("MeanTreeFileBox").Text = GetRelativePath(result[0], Directory.GetCurrentDirectory());
            }
        }

        private async void BrowseDependencyFileClicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog;

            if (!Program.IsMac)
            {
                dialog = new OpenFileDialog()
                {
                    Title = "Choose dependency file",
                    AllowMultiple = false,
                    Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Name = "All files", Extensions = new List<string>() { "*" } } }
                };
            }
            else
            {
                dialog = new OpenFileDialog()
                {
                    Title = "Choose dependency file",
                    AllowMultiple = false
                };
            }

            string[] result = await dialog.ShowAsync(this);

            if (result != null && result.Length == 1)
            {
                this.FindControl<TextBox>("DependencyFileBox").Text = GetRelativePath(result[0], Directory.GetCurrentDirectory());
            }
        }

        private async void BrowseRateFileClicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog;

            if (!Program.IsMac)
            {
                dialog = new OpenFileDialog()
                {
                    Title = "Choose rate file",
                    AllowMultiple = false,
                    Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Name = "All files", Extensions = new List<string>() { "*" } } }
                };
            }
            else
            {
                dialog = new OpenFileDialog()
                {
                    Title = "Choose rate file",
                    AllowMultiple = false
                };
            }


            string[] result = await dialog.ShowAsync(this);

            if (result != null && result.Length == 1)
            {
                this.FindControl<TextBox>("RateFileBox").Text = GetRelativePath(result[0], Directory.GetCurrentDirectory());
            }
        }

        private async void BrowsePiFileClicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog;

            if (!Program.IsMac)
            {
                dialog = new OpenFileDialog()
                {
                    Title = "Choose pi file",
                    AllowMultiple = false,
                    Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Name = "All files", Extensions = new List<string>() { "*" } } }
                };
            }
            else
            {
                dialog = new OpenFileDialog()
                {
                    Title = "Choose pi file",
                    AllowMultiple = false
                };
            }

            string[] result = await dialog.ShowAsync(this);

            if (result != null && result.Length == 1)
            {
                this.FindControl<TextBox>("PiFileBox").Text = GetRelativePath(result[0], Directory.GetCurrentDirectory());
            }
        }

        private async void BrowseModelFileClicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog;

            if (!Program.IsMac)
            {
                dialog = new OpenFileDialog()
                {
                    Title = "Choose model file",
                    AllowMultiple = false,
                    Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Name = "All files", Extensions = new List<string>() { "*" } } }
                };
            }
            else
            {
                dialog = new OpenFileDialog()
                {
                    Title = "Choose model file",
                    AllowMultiple = false
                };
            }

            string[] result = await dialog.ShowAsync(this);

            if (result != null && result.Length == 1)
            {
                this.FindControl<TextBox>("ModelFileBox").Text = GetRelativePath(result[0], Directory.GetCurrentDirectory());
            }
        }

        private async void BrowseOutputPrefixClicked(object sender, RoutedEventArgs e)
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
                this.FindControl<TextBox>("OutputPrefixBox").Text = GetRelativePath(result, Directory.GetCurrentDirectory());
            }
        }

        private async void BrowseParameterFileClicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog;

            if (!Program.IsMac)
            {
                dialog = new OpenFileDialog()
                {
                    Title = "Choose parameter files",
                    AllowMultiple = true,
                    Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Name = "All files", Extensions = new List<string>() { "*" } } }
                };
            }
            else
            {
                dialog = new OpenFileDialog()
                {
                    Title = "Choose parameter files",
                    AllowMultiple = true
                };
            }

            string[] result = await dialog.ShowAsync(this);

            if (result != null && result.Length > 0)
            {
                this.FindControl<TextBox>("ParameterFileBox").Text = (from el in result select GetRelativePath(el, Directory.GetCurrentDirectory())).Aggregate((a, b) => a + "," + b);
            }
        }

        private async void EditMLClicked(object sender, RoutedEventArgs e)
        {
            EditMLStrategyWindow window = new EditMLStrategyWindow(this.FindControl<TextBox>("MLStrategy").Text);

            await window.ShowDialog(this);

            if (!string.IsNullOrEmpty(window.result))
            {
                this.FindControl<TextBox>("MLStrategy").Text = window.result;
            }
        }

        private void WindowLoaded(object sender, EventArgs e)
        {
            this.FindControl<TextBox>("MLStrategy").Text = "IterativeSampling(0.0001,10.0001,0.1,plot,Value,0.001)|RandomWalk(Value,0.001,10000,plot)|NesterovClimbing(Value,0.001,100,plot)";
            this.FindControl<NumericUpDown>("MaxCoVBox").Value = -1.0 / 16.0 * Math.Log⁡(1.0 - 1.0 / 3.0 * (7.0 / 3.0 - 2.0 / (2.0 * 2.0 - 1.0)));
        }


        //From https://stackoverflow.com/questions/703281/getting-path-relative-to-the-current-working-directory
        public static string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            //Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        private async void SaveScriptClicked(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(this.FindControl<TextBox>("DataFileBox").Text) || string.IsNullOrEmpty(this.FindControl<TextBox>("TreeFileBox").Text) || string.IsNullOrEmpty(this.FindControl<TextBox>("OutputPrefixBox").Text))
            {
                await new MessageBox("Attention!", "You have not filled all the required parameter boxes!").ShowDialog(this);
                return;
            }

            string[] args = GetArgs();

            string script = "sMap " + args.Aggregate((a, b) => (a + " " + b)) + "\n";

            SaveFileDialog dialog = new SaveFileDialog()
            {
                Title = "Choose output script file",
                Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Name = "Windows batch", Extensions = new List<string>() { "bat" } },
                    new FileDialogFilter() { Name = "Shell script", Extensions = new List<string>() { "sh" } },
                    new FileDialogFilter() { Name = "All files", Extensions = new List<string>() { "*" } } },
                DefaultExtension = "bat"
            };

            string result = await dialog.ShowAsync(this);

            if (!string.IsNullOrEmpty(result))
            {
                File.WriteAllText(result, script);
            }
        }

        string[] GetArgs()
        {
            List<string> args = new List<string>();

            args.Add("-d");
            args.Add("\"" + this.FindControl<TextBox>("DataFileBox").Text.Replace("\\", "/") + "\"");

            args.Add("-t");
            args.Add("\"" + this.FindControl<TextBox>("TreeFileBox").Text.Replace("\\", "/") + "\"");

            args.Add("-n");
            args.Add(((int)this.FindControl<NumericUpDown>("NumSimBox").Value).ToString());

            if (!string.IsNullOrEmpty(this.FindControl<TextBox>("MeanTreeFileBox").Text))
            {
                args.Add("-T");
                args.Add("\"" + this.FindControl<TextBox>("MeanTreeFileBox").Text.Replace("\\", "/") + "\"");
            }

            args.Add("-o");
            args.Add("\"" + this.FindControl<TextBox>("OutputPrefixBox").Text.Replace("\\", "/") + "\"");


            if (!string.IsNullOrEmpty(this.FindControl<TextBox>("DependencyFileBox").Text))
            {
                args.Add("-D");
                args.Add("\"" + this.FindControl<TextBox>("DependencyFileBox").Text.Replace("\\", "/") + "\"");
            }

            if (!string.IsNullOrEmpty(this.FindControl<TextBox>("RateFileBox").Text))
            {
                args.Add("-r");
                args.Add("\"" + this.FindControl<TextBox>("RateFileBox").Text.Replace("\\", "/") + "\"");
            }

            if (!string.IsNullOrEmpty(this.FindControl<TextBox>("PiFileBox").Text))
            {
                args.Add("-p");
                args.Add("\"" + this.FindControl<TextBox>("PiFileBox").Text.Replace("\\", "/") + "\"");
            }

            if (this.FindControl<CheckBox>("EstimatedPisBox").IsChecked == true)
            {
                args.Add("--ep");
            }

            if (!string.IsNullOrEmpty(this.FindControl<TextBox>("ModelFileBox").Text))
            {
                args.Add("-i");
                args.Add("\"" + this.FindControl<TextBox>("ModelFileBox").Text.Replace("\\", "/") + "\"");
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

            if (this.FindControl<CheckBox>("DTestBox").IsChecked == true)
            {
                args.Add("--d-test");
                args.Add(((int)this.FindControl<NumericUpDown>("DTestMultiplicityBox").Value).ToString());
            }

            if (this.FindControl<CheckBox>("PPBox").IsChecked == true)
            {
                args.Add("--posterior-predictive");
                args.Add(((int)this.FindControl<NumericUpDown>("PPMultiplicityBox").Value).ToString());
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

            if (this.FindControl<CheckBox>("MaxSamplesCheckBox").IsChecked == true)
            {
                args.Add("--max-samples");
                args.Add(((int)this.FindControl<NumericUpDown>("MaxSamplesBox").Value).ToString());
            }

            if (this.FindControl<NumericUpDown>("MinSamplesBox").Value != 2 * ((int)this.FindControl<NumericUpDown>("NumSimBox").Value))
            {
                args.Add("--min-samples");
                args.Add(((int)this.FindControl<NumericUpDown>("MinSamplesBox").Value).ToString());
            }

            if (this.FindControl<NumericUpDown>("MaxCoVBox").Value != -1.0 / 16.0 * Math.Log⁡(1.0 - 1.0 / 3.0 * (7.0 / 3.0 - 2.0 / (2.0 * ((int)this.FindControl<NumericUpDown>("NumRunsBox").Value) - 1.0))))
            {
                args.Add("--max-cov");
                args.Add(this.FindControl<NumericUpDown>("MaxCoVBox").Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            if (this.FindControl<NumericUpDown>("MinESSBox").Value != 200)
            {
                args.Add("--min-ess");
                args.Add(((int)this.FindControl<NumericUpDown>("MinESSBox").Value).ToString());
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

            if (!string.IsNullOrEmpty(this.FindControl<TextBox>("ParameterFileBox").Text))
            {
                args.Add("--parameters");
                args.Add("\"" + this.FindControl<TextBox>("ParameterFileBox").Text.Replace("\\", "/") + "\"");
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

        private async void StartAnalysisClicked(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(this.FindControl<TextBox>("DataFileBox").Text) || string.IsNullOrEmpty(this.FindControl<TextBox>("TreeFileBox").Text) || string.IsNullOrEmpty(this.FindControl<TextBox>("OutputPrefixBox").Text))
            {
                await new MessageBox("Attention!", "You have not filled all the required parameter boxes!").ShowDialog(this);
                return;
            }

            string[] args = GetArgs();

            RunWindow rw = new RunWindow(args, false, false);
            await rw.ShowDialog(this);
        }
    }
}
