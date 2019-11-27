using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Threading;

namespace sMap_GUI
{
    public class PlotProgressWindow : Window
    {
        private double _progress;

        public double Progress
        {
            get
            {
                return _progress;
            }

            set
            {
                _progress = value;
                this.FindControl<ProgressBar>("ProgressBar").Value = _progress;
                this.FindControl<TextBlock>("ProgressDesc").Text = _progress.ToString("0%");
            }
        }

        public string ProgressText
        {
            get
            {
                return this.FindControl<TextBlock>("ProgressText").Text;
            }

            set
            {
                this.FindControl<TextBlock>("ProgressText").Text = value;
            }
        }

        public bool IsIndeterminate
        {
            set
            {
                if (value)
                {
                    this.FindControl<TextBlock>("ProgressText").IsVisible = true;
                    this.FindControl<ProgressBar>("ProgressBar").IsIndeterminate = true;
                    this.FindControl<Grid>("MainGrid").ColumnDefinitions[1] = new ColumnDefinition(0, GridUnitType.Pixel);
                }
                else
                {
                    this.FindControl<TextBlock>("ProgressText").IsVisible = true;
                    this.FindControl<ProgressBar>("ProgressBar").IsIndeterminate = false;
                    this.FindControl<Grid>("MainGrid").ColumnDefinitions[1] = new ColumnDefinition(50, GridUnitType.Pixel);
                }
            }
        }

        public PlotProgressWindow()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        EventWaitHandle Handle;

        public PlotProgressWindow(EventWaitHandle handle)
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            Handle = handle;
        }

        private void WindowOpened(object sender, EventArgs e)
        {
            if (Handle != null)
            {
                Handle.Set();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
