using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace sMap_GUI
{
    public class Tick : UserControl
    {
        public enum Type { Tick, Cross }

        public static readonly StyledProperty<Type> IconTypeProperty = AvaloniaProperty.Register<Tick, Type>(nameof(Type));

        public Type IconType
        {
            get { return GetValue(IconTypeProperty); }
            set { SetValue(IconTypeProperty, value); }
        }

        public Tick()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            switch (this.IconType)
            {
                case Type.Tick:
                    this.FindControl<Path>("OuterPath").Fill = new SolidColorBrush(Color.FromArgb(255, 34, 177, 76));
                    this.FindControl<Path>("TickPath").IsVisible = true;
                    this.FindControl<Path>("CrossPath").IsVisible = false;
                    break;
                case Type.Cross:
                    this.FindControl<Path>("OuterPath").Fill = new SolidColorBrush(Color.FromArgb(255, 237, 28, 36));
                    this.FindControl<Path>("CrossPath").IsVisible = true;
                    this.FindControl<Path>("TickPath").IsVisible = false;
                    break;
            }

            this.PropertyChanged += (s, e) =>
            {
                if (e.Property == IconTypeProperty)
                {
                    switch (this.IconType)
                    {
                        case Type.Tick:
                            this.FindControl<Path>("OuterPath").Fill = new SolidColorBrush(Color.FromArgb(255, 34, 177, 76));
                            this.FindControl<Path>("TickPath").IsVisible = true;
                            this.FindControl<Path>("CrossPath").IsVisible = false;
                            break;
                        case Type.Cross:
                            this.FindControl<Path>("OuterPath").Fill = new SolidColorBrush(Color.FromArgb(255, 237, 28, 36));
                            this.FindControl<Path>("CrossPath").IsVisible = true;
                            this.FindControl<Path>("TickPath").IsVisible = false;
                            break;
                    }
                }
            };
        }


    }
}
