using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Threading;

namespace sMap_GUI
{
    public class RenderingProgressWindow : Window
    {
        public RenderingProgressWindow()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
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

        EventWaitHandle Handle;

        public RenderingProgressWindow(EventWaitHandle handle)
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            Handle = handle;
        }

        private void WindowOpened(object sender, EventArgs e)
        {
            Handle.Set();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
