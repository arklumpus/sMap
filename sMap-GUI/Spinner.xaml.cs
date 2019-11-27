using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace sMap_GUI
{
    public class Spinner : UserControl
    {

        public static readonly StyledProperty<IBrush> FillProperty = AvaloniaProperty.Register<Octagon, IBrush>(nameof(IBrush));

        public IBrush Fill
        {
            get { return GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }



        public Spinner()
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
                    this.FindControl<Path>("SpinnerPath").Fill = this.Fill;
                }
            };
        }
    }
}
