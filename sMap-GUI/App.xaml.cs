using Avalonia;
using Avalonia.Markup.Xaml;

namespace sMap_GUI
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
