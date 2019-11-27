using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ScriptConsoleLibrary;
using System.IO;

namespace Script_sMap
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        bool tipOpen = true;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            this.FindControl<ScriptConsoleControl>("scc").RunCommand(FixedScripts.FixedSlimTreeNode(), false);
            this.FindControl<ScriptConsoleControl>("scc").RunCommand("#r \"Utils\"", false);
            this.FindControl<ScriptConsoleControl>("scc").RunCommand("using System;", false);
            this.FindControl<ScriptConsoleControl>("scc").RunCommand("using System.IO;", false);
            this.FindControl<ScriptConsoleControl>("scc").RunCommand("using System.Linq;", false);
            this.FindControl<ScriptConsoleControl>("scc").RunCommand("using System.Collections.Generic;", false);
            this.FindControl<ScriptConsoleControl>("scc").RunCommand("using Utils;", false);

            this.FindControl<ScriptConsoleControl>("scc").RunCommand("\"Hello world!\"", true);

        }

        private void OpenCloseTipClicked(object sender, RoutedEventArgs e)
        {
            if (tipOpen)
            {
                this.FindControl<Grid>("MainGrid").ColumnDefinitions[2] = new ColumnDefinition(0, GridUnitType.Pixel);
                this.FindControl<Avalonia.Controls.Shapes.Path>("OpenTipPath").IsVisible = true;
                this.FindControl<Avalonia.Controls.Shapes.Path>("CloseTipPath").IsVisible = false;
                tipOpen = false;
            }
            else
            {
                this.FindControl<Grid>("MainGrid").ColumnDefinitions[2] = new ColumnDefinition(0, GridUnitType.Auto);
                this.FindControl<Avalonia.Controls.Shapes.Path>("OpenTipPath").IsVisible = false;
                this.FindControl<Avalonia.Controls.Shapes.Path>("CloseTipPath").IsVisible = true;
                tipOpen = true;
            }
        }
    }
}
