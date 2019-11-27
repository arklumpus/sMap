using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;

namespace sMap_GUI
{
    public class Octagon : UserControl
    {
        public static readonly StyledProperty<IBrush> FillProperty = AvaloniaProperty.Register<Octagon, IBrush>(nameof(IBrush));
        public static readonly StyledProperty<bool> IsTickProperty = AvaloniaProperty.Register<Octagon, bool>(nameof(Boolean));

        public IBrush Fill
        {
            get { return GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        public bool IsTick
        {
            get { return GetValue(IsTickProperty); }
            set { SetValue(IsTickProperty, value); }
        }

        public Octagon()
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
                    this.FindControl<Path>("InnerPath").Fill = this.Fill;
                    this.FindControl<Path>("TickPath").Stroke = this.Fill;
                }
                else if (e.Property == IsTickProperty)
                {
                    this.FindControl<Path>("OuterPath").IsVisible = !this.IsTick;
                    this.FindControl<Path>("InnerPath").IsVisible = !this.IsTick;
                    this.FindControl<Path>("TickPath").IsVisible = this.IsTick;
                }
            };
        }
    }
}
