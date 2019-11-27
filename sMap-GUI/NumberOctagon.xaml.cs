using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;

namespace sMap_GUI
{
    public class NumberOctagon : UserControl
    {
        public static readonly StyledProperty<IBrush> FillProperty = AvaloniaProperty.Register<NumberOctagon, IBrush>(nameof(IBrush));
        public static readonly StyledProperty<string> NumberProperty = AvaloniaProperty.Register<NumberOctagon, string>(nameof(String));

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

        public NumberOctagon()
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
                    this.FindControl<Path>("OuterPath").Fill = this.Fill;
                }
                else if (e.Property == NumberProperty)
                {
                    this.FindControl<TextBlock>("NumberBlock").Text = this.Number;
                }
            };
        }
    }
}
