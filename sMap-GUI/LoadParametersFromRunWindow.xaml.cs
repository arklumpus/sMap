using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MathNet.Numerics.Distributions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Utils;

namespace sMap_GUI
{
    public class LoadParametersFromRunWindow : Window
    {
        public LoadParametersFromRunWindow()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        string LoadParametersFromRunFix;
        string LoadParametersFromRunBayesian;

        bool initialised = false;

        CharacterDependency[][] Dependencies;
        Dictionary<string, Parameter>[] Rates;
        Dictionary<string, Parameter>[] Pi;

        public LoadParametersFromRunWindow(CharacterDependency[][] dependencies, Dictionary<string, Parameter>[] rates, Dictionary<string, Parameter>[] pi)
        {
            Dependencies = dependencies;
            Rates = rates;
            Pi = pi;

            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            List<ComboBoxItem> items = new List<ComboBoxItem>();

            for (int i = 0; i < dependencies.Length; i++)
            {
                items.Add(new ComboBoxItem() { Content = i.ToString() });
            }

            this.FindControl<ComboBox>("SetComboBox").Items = items;
            this.FindControl<ComboBox>("SetComboBox").SelectedIndex = 0;

            LoadParametersFromRunFix = new System.IO.StreamReader(typeof(LoadParametersFromRunWindow).Assembly.GetManifestResourceStream("sMap_GUI.LoadParametersFromRunFix.cs")).ReadToEnd();
            LoadParametersFromRunBayesian = new System.IO.StreamReader(typeof(LoadParametersFromRunWindow).Assembly.GetManifestResourceStream("sMap_GUI.LoadParametersFromRunBayes.cs")).ReadToEnd();

            this.FindControl<TextBox>("AdvancedScriptBox").Text = LoadParametersFromRunFix;

            initialised = true;
        }

        bool changing = false;

        private void ActionSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (initialised)
            {
                changing = true;
                if (this.FindControl<ComboBox>("ActionComboBox").SelectedIndex == 0)
                {
                    this.FindControl<TextBox>("AdvancedScriptBox").Text = LoadParametersFromRunFix;
                }
                else if (this.FindControl<ComboBox>("ActionComboBox").SelectedIndex == 1)
                {
                    this.FindControl<TextBox>("AdvancedScriptBox").Text = LoadParametersFromRunBayesian;
                }
                changing = false;
            }
        }

        private void CancelButtonClicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ScriptKeyUp(object sender, KeyEventArgs e)
        {
            if (initialised && !changing)
            {
                this.FindControl<ComboBox>("ActionComboBox").SelectedIndex = 2;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void AdvancedButtonClicked(object sender, RoutedEventArgs e)
        {
            if (this.FindControl<TextBox>("AdvancedScriptBox").IsVisible)
            {
                this.FindControl<TextBox>("AdvancedScriptBox").IsVisible = false;
                this.Width = 600;
                this.Height = 180;
                this.FindControl<Path>("HideScriptPath").IsVisible = false;
                this.FindControl<Path>("ShowScriptPath").IsVisible = true;
            }
            else
            {
                this.FindControl<TextBox>("AdvancedScriptBox").IsVisible = true;
                this.Width = 800;
                this.Height = 450;
                this.FindControl<Path>("HideScriptPath").IsVisible = true;
                this.FindControl<Path>("ShowScriptPath").IsVisible = false;
            }
        }

        private async void BrowseButtonClicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog;

            if (!Program.IsMac)
            {
                dialog = new OpenFileDialog() { AllowMultiple = false, Title = "Open sMap run file", Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Extensions = Program.IsMac ? new List<string>() { "*" } : new List<string>() { "bin" }, Name = "sMap run files" }, new FileDialogFilter() { Extensions = new List<string>() { "*" }, Name = "All files" } } };
            }
            else
            {
                dialog = new OpenFileDialog() { AllowMultiple = false, Title = "Open sMap run file" };
            }

            string[] result = await dialog.ShowAsync(this);

            if (result != null && result.Length > 0)
            {
                this.FindControl<TextBox>("RunFileBox").Text = result[0];
            }
        }

        private async void OKButtonClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                int depInd = this.FindControl<ComboBox>("SetComboBox").SelectedIndex;
                var paramsToEstimate = Utils.Utils.ParametersToEstimateList(Utils.Utils.GetParametersToEstimate(Dependencies[depInd], Rates, Pi), new ThreadSafeRandom());

                string script = this.FindControl<TextBox>("AdvancedScriptBox").Text;

                Func<object[], object> getEstimate = await CompileScript(script);

                if (getEstimate != null)
                {
                    SerializedRun run = SerializedRun.Deserialize(this.FindControl<TextBox>("RunFileBox").Text);

                    for (int i = 0; i < paramsToEstimate.rates.Count; i++)
                    {
                        double[] estimates = (from el in run.Parameters select el[depInd][i]).ToArray();
                        string parameterName = run.ParameterNames[depInd][i + 1];

                        var meanAndVariance = estimates.MeanAndVariance();

                        object estimate = getEstimate(new object[] { parameterName, 0, estimates, meanAndVariance.Mean, meanAndVariance.Variance });

                        if (estimate is double)
                        {
                            paramsToEstimate.rates[i].Action = Parameter.ParameterAction.Fix;
                            paramsToEstimate.rates[i].Value = (double)estimate;
                        }
                        else if (estimate is string)
                        {
                            paramsToEstimate.rates[i].Action = Parameter.ParameterAction.Bayes;
                            paramsToEstimate.rates[i].PriorDistribution = Utils.Utils.ParseDistribution((string)estimate, new Random());
                        }
                    }

                    for (int i = 0; i < paramsToEstimate.pis.Count; i++)
                    {
                        double[] estimates = (from el in run.Parameters select el[depInd][i + paramsToEstimate.rates.Count]).ToArray();
                        string parameterName = run.ParameterNames[depInd][i + paramsToEstimate.rates.Count + 1];

                        var meanAndVariance = estimates.MeanAndVariance();

                        object estimate = getEstimate(new object[] { parameterName, parameterName.Contains(": π(") ? 1 : 2, estimates, meanAndVariance.Mean, meanAndVariance.Variance });

                        if (estimate is double)
                        {
                            paramsToEstimate.pis[i].Item1.Action = Parameter.ParameterAction.Fix;
                            paramsToEstimate.pis[i].Item1.Value = (double)estimate;
                        }
                        else if (estimate is string)
                        {
                            string distrib = (string)estimate;
                            if (distrib.StartsWith("Dirichlet"))
                            {
                                paramsToEstimate.pis[i].Item1.Action = Parameter.ParameterAction.Dirichlet;
                                string valString = distrib.Substring(distrib.IndexOf("(") + 1);
                                valString = valString.Substring(0, valString.IndexOf(")"));
                                paramsToEstimate.pis[i].Item1.DistributionParameter = double.Parse(valString, System.Globalization.CultureInfo.InvariantCulture);
                            }
                            else if (distrib.StartsWith("Multinomial"))
                            {
                                paramsToEstimate.pis[i].Item1.Action = Parameter.ParameterAction.Multinomial;
                                string valString = distrib.Substring(distrib.IndexOf("(") + 1);
                                valString = valString.Substring(0, valString.IndexOf(")"));
                                paramsToEstimate.pis[i].Item1.DistributionParameter = double.Parse(valString, System.Globalization.CultureInfo.InvariantCulture);
                            }
                        }
                    }

                    this.Close();
                }
            }
            catch (Exception ex)
            {
                await ShowErrorMessage("Error!", "Error: " + ex.Message);
            }
        }

        async Task ShowErrorMessage(string title, string message)
        {
            await new MessageBox(title, message).ShowDialog(this);
        }


        private async Task<Func<object[], object>> CompileScript(string script)
        {

            string[] splittedScript = script.Replace("\r", "").Split("\n");

            string preScript = "";

            script = "";

            for (int i = 0; i < splittedScript.Length; i++)
            {
                if (!splittedScript[i].Trim().StartsWith("using") && !splittedScript[i].Trim().StartsWith("#"))
                {
                    script += splittedScript[i] + "\n";
                }
                else
                {
                    preScript += splittedScript[i] + "\n";
                }
            }


            script = preScript + "using System;\nnamespace LoadingParameters\n{\npublic enum ParameterType { Rate = 0, Pi = 1, ConditionedProbability = 2 }\npublic class ParameterLoader\n{\npublic object SetParameter(string parameterName, int parameterType, double[] parameterEstimates, double mean, double variance) { return SetParameter(parameterName, (ParameterType)parameterType, parameterEstimates, mean, variance); }\n" + script + "\n}\n}";

            SyntaxTree tree = CSharpSyntaxTree.ParseText(script);
            string assemblyName = System.IO.Path.GetRandomFileName();
            MetadataReference[] references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "mscorlib.dll")),
                MetadataReference.CreateFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "System.dll")),
                MetadataReference.CreateFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "System.Core.dll")),
                MetadataReference.CreateFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "System.Runtime.dll")),
                MetadataReference.CreateFromFile(Assembly.GetEntryAssembly().Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Utils.Utils).Assembly.Location)
            };

            CSharpCompilation compilation = CSharpCompilation.Create(assemblyName, new[] { tree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    string error = "Compilation error(s)!\n";

                    foreach (Diagnostic diagnostic in failures)
                    {
                        error += diagnostic.Id + ": " + diagnostic.GetMessage() + "\n";
                    }

                    await ShowErrorMessage("Compilation error", error);

                    return null;
                }
                else
                {
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    Assembly assembly = Assembly.Load(ms.ToArray());

                    Type type = assembly.GetType("LoadingParameters.ParameterLoader");
                    object obj = Activator.CreateInstance(type);


                    return (par =>
                    {
                        return type.InvokeMember("SetParameter", BindingFlags.Default | BindingFlags.InvokeMethod, null, obj, par);
                    });
                }
            }
        }
    }
}
