using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace sMap_GUI
{
    public class AddButton : UserControl
    {
        public enum ButtonTypes
        {
            Add, Remove, Up, Down
        }

        public static readonly StyledProperty<ButtonTypes> TypeProperty = AvaloniaProperty.Register<AddButton, ButtonTypes>(nameof(Type));

        public ButtonTypes Type
        {
            get { return GetValue(TypeProperty); }
            set { SetValue(TypeProperty, value); }
        }

        public AddButton()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
