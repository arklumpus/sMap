using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Logging.Serilog;
using Utils;
using System.Linq;
using Avalonia.Media;
using Avalonia.Controls;
using System.Threading;
using SlimTreeNode;

namespace sMap_GUI
{
    public static class Extensions
    {
        public static void ScrollToBottom(this ScrollViewer sv)
        {
            sv.Offset = new Vector(sv.Offset.X, sv.Extent.Height - sv.Viewport.Height);
        }
    }
   
    class Program
    {
        public static bool IsMac
        {
            get
            {
                return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX);
            }
        }

        public static bool IsWindows
        {
            get
            {
                return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
            }
        }


        public static bool IsLinux
        {
            get
            {
                return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);
            }
        }


        public static SolidColorBrush GetTransparentBrush((int r, int g, int b, double a) col)
        {
            return new SolidColorBrush(Color.FromArgb((byte)(col.a * 255), (byte)col.r, (byte)col.g, (byte)col.b));
        }

        public static SolidColorBrush GetBrush((int r, int g, int b, double a) col)
        {
            return new SolidColorBrush(Color.FromArgb(255, (byte)(col.r * col.a + 255 * (1 - col.a)), (byte)(col.g * col.a + 255 * (1 - col.a)), (byte)(col.b * col.a + 255 * (1 - col.a))));
        }

        public static SolidColorBrush GetDarkBrush((int r, int g, int b, double a) col)
        {
            return new SolidColorBrush(Color.FromArgb(255, (byte)(col.r * col.a + 0 * (1 - col.a)), (byte)(col.g * col.a + 0 * (1 - col.a)), (byte)(col.b * col.a + 0 * (1 - col.a))));
        }


        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args) => BuildAvaloniaApp().Start(AppMain, args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToDebug();

        // Your application's entry point. Here you can initialize your MVVM framework, DI
        // container, etc.
        private static void AppMain(Application app, string[] args)
        {
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            app.Run(new StartWindow());
        }
    }
}
