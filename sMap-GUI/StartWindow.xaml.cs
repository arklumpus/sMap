using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using Utils;

namespace sMap_GUI
{
    public class StartWindow : Window
    {
        public StartWindow()
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

        private async void sMapWizardClicked(object sender, RoutedEventArgs e)
        {
            await new WizardWindow().ShowDialog(this);
        }

        private async void sMapClicked(object sender, RoutedEventArgs e)
        {
            await new MainWindow().ShowDialog(this);
        }

        private async void NodeInfoClicked(object sender, RoutedEventArgs e)
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
                try
                {
                    SerializedRun run = SerializedRun.Deserialize(result[0]);

                    await new NodeInfoWindow(run).ShowDialog(this);
                }
                catch (Exception ex)
                {
                    MessageBox box = new MessageBox("Error!", "Error: " + ex.Message);
                    await box.ShowDialog(this);
                }
            }
        }

        private async void PlotSMapClicked(object sender, RoutedEventArgs e)
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
                try
                {
                    SerializedRun run = SerializedRun.Deserialize(result[0]);

                    await new PlotSMapWindow(run).ShowDialog(this);
                }
                catch (Exception ex)
                {
                    MessageBox box = new MessageBox("Error!", "Error: " + ex.Message);
                    await box.ShowDialog(this);
                }
            }
        }

        private async void StatSMapClicked(object sender, RoutedEventArgs e)
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
                try
                {
                    SerializedRun run = SerializedRun.Deserialize(result[0]);

                    await new StatSMapWindow(run).ShowDialog(this);
                }
                catch (Exception ex)
                {
                    MessageBox box = new MessageBox("Error!", "Error: " + ex.Message);
                    await box.ShowDialog(this);
                }
            }
        }

        private async void BlendSMapClicked(object sender, RoutedEventArgs e)
        {
            await new BlendSMapWindow().ShowDialog(this);
        }

        private async void MergeSMapClicked(object sender, RoutedEventArgs e)
        {
            await new MergeSMapWindow().ShowDialog(this);
        }

        private void ScriptSMapClicked(object sender, RoutedEventArgs e)
        {
            if (Program.IsWindows)
            {
                System.Diagnostics.Process.Start("Script-sMap.exe").WaitForExit();
            }
            else
            {
                System.Diagnostics.Process.Start("Script-sMap").WaitForExit();
            }
        }
    }
}
