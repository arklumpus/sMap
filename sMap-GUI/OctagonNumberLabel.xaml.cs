using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;

namespace sMap_GUI
{
    public class OctagonNumberLabel : UserControl
    {
        public static readonly StyledProperty<IBrush> FillProperty = AvaloniaProperty.Register<OctagonNumberLabel, IBrush>(nameof(IBrush));
        public static readonly StyledProperty<string> NumberProperty = AvaloniaProperty.Register<OctagonNumberLabel, string>(nameof(String));
        public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<OctagonNumberLabel, string>(nameof(String));

        public IBrush Fill
        {
            get { return GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        public string Number
        {
            get { return GetValue(NumberProperty); }
            set { SetValue(NumberProperty, value); }
        }

        public string Text
        {
            get { return GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public OctagonNumberLabel()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            this.PropertyChanged += (s, e) =>
            {
                if (e.Property == FillProperty)
                {
                    this.FindControl<NumberOctagon>("NumberOctagon").Fill = this.Fill;
                    this.FindControl<NumberOctagon>("BackgroundOctagon").Fill = this.Fill;
                    this.FindControl<Grid>("GridContainer").Background = this.Fill;
                }
                else if (e.Property == NumberProperty)
                {
                    this.FindControl<NumberOctagon>("NumberOctagon").Number = this.Number;
                }
                else if (e.Property == TextProperty)
                {
                    this.FindControl<TextBlock>("TextBlock").Text = this.Text;
                }
            };
        }
    }
}
