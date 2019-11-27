using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace sMap_GUI
{
    public class ColorDialog : Window
    {
        public ColorDialog()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        public ColorDialog(byte r, byte g, byte b)
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            changing = true;

            this.FindControl<NumericUpDown>("RBox").Value = this.FindControl<Slider>("RSlider").Value = r;
            this.FindControl<NumericUpDown>("GBox").Value = this.FindControl<Slider>("GSlider").Value = g;
            this.FindControl<NumericUpDown>("BBox").Value = this.FindControl<Slider>("BSlider").Value = b;

            this.FindControl<Canvas>("ColourCanvas").Background = new SolidColorBrush(Color.FromArgb(255, (byte)this.FindControl<NumericUpDown>("RBox").Value, (byte)this.FindControl<NumericUpDown>("GBox").Value, (byte)this.FindControl<NumericUpDown>("BBox").Value));

            changing = false;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            initialised = true;
        }

        bool initialised = false;

        bool changing = false;

        private void PropertyChangedEvent(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (!initialised)
            {
                return;
            }

            if (!changing)
            {
                if (e.Property == Slider.ValueProperty)
                {
                    changing = true;

                    this.FindControl<NumericUpDown>("RBox").Value = this.FindControl<Slider>("RSlider").Value;
                    this.FindControl<NumericUpDown>("GBox").Value = this.FindControl<Slider>("GSlider").Value;
                    this.FindControl<NumericUpDown>("BBox").Value = this.FindControl<Slider>("BSlider").Value;

                    this.FindControl<Canvas>("ColourCanvas").Background = new SolidColorBrush(Color.FromArgb(255, (byte)this.FindControl<NumericUpDown>("RBox").Value, (byte)this.FindControl<NumericUpDown>("GBox").Value, (byte)this.FindControl<NumericUpDown>("BBox").Value));

                    changing = false;
                }
            }
        }

        private void ValueChangedEvent(object sender, NumericUpDownValueChangedEventArgs e)
        {
            if (!initialised)
            {
                return;
            }

            if (!changing)
            {
                changing = true;

                this.FindControl<Slider>("RSlider").Value = this.FindControl<NumericUpDown>("RBox").Value;
                this.FindControl<Slider>("GSlider").Value = this.FindControl<NumericUpDown>("GBox").Value;
                this.FindControl<Slider>("BSlider").Value = this.FindControl<NumericUpDown>("BBox").Value;

                this.FindControl<Canvas>("ColourCanvas").Background = new SolidColorBrush(Color.FromArgb(255, (byte)this.FindControl<NumericUpDown>("RBox").Value, (byte)this.FindControl<NumericUpDown>("GBox").Value, (byte)this.FindControl<NumericUpDown>("BBox").Value));

                changing = false;
            }
        }

        public (byte r, byte g, byte b, double alpha)? Colour = null;

        private void OKClicked(object sender, RoutedEventArgs e)
        {
            Colour = ((byte)this.FindControl<NumericUpDown>("RBox").Value, (byte)this.FindControl<NumericUpDown>("GBox").Value, (byte)this.FindControl<NumericUpDown>("BBox").Value, 1);
            this.Close();
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            Colour = null;
            this.Close();
        }
    }
}
