using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;

namespace sMap_GUI
{
    public class MessageBox : Window
    {
        public MessageBox()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        public enum MessageBoxButtonTypes
        {
            OK,
            YesNo
        }

        public enum Results
        {
            Yes,
            No,
            OK
        }

        public enum MessageBoxIconTypes
        {
            Warning, Tick
        }

        public Results Result = Results.No; 


        public MessageBox(string title, string text, MessageBoxButtonTypes type = MessageBoxButtonTypes.OK, MessageBoxIconTypes iconType = MessageBoxIconTypes.Warning)
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            this.Title = title;
            this.FindControl<TextBlock>("Message").Text = text;

            if (type == MessageBoxButtonTypes.OK)
            {
                this.FindControl<Button>("OKButton").IsVisible = true;
            }
            else
            {
                this.FindControl<Grid>("YesNoButtons").IsVisible = true;
            }
            
            if (iconType == MessageBoxIconTypes.Warning)
            {
                this.FindControl<Canvas>("WarningCanvas").IsVisible = true;
            }
            else if (iconType == MessageBoxIconTypes.Tick)
            {
                this.FindControl<Canvas>("TickCanvas").IsVisible = true;
            }
        }

        private void MessageBoxOpened(object sender, EventArgs e)
        {
            
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OKClicked(object sender, RoutedEventArgs e)
        {
            this.Result = Results.OK;
            this.Close();
        }

        private void YesClicked(object sender, RoutedEventArgs e)
        {
            this.Result = Results.Yes;
            this.Close();
        }

        private void NoClicked(object sender, RoutedEventArgs e)
        {
            this.Result = Results.No;
            this.Close();
        }
    }
}
