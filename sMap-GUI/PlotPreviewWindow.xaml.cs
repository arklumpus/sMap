using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace sMap_GUI
{
    public class PlotPreviewWindow : Window
    {
        public PlotPreviewWindow()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        string FileName;

        public PlotPreviewWindow(string fileName)
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            FileName = fileName;


        }

        string tmpFileName;

        private async void WindowOpened(object sender, EventArgs e)
        {
            string ghostScript = GetGhostScript();

            tmpFileName = Path.GetTempFileName() + ".png";

            string arguments = "-sDEVICE=pngalpha -sOutputFile=\"" + tmpFileName + "\" -r144 -dBATCH -dNOPAUSE -dFirstPage=1 -dLastPage=1 " + FileName;

            ProcessStartInfo info = new ProcessStartInfo(ghostScript, arguments);

            EventWaitHandle handle = new EventWaitHandle(false, EventResetMode.ManualReset);

            RenderingProgressWindow win = new RenderingProgressWindow(handle);

            Thread thr = new Thread(() =>
            {
                handle.WaitOne();

                Process proc = Process.Start(info);

                proc.WaitForExit();

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    win.Close();
                });
            });

            thr.Start();

            await win.ShowDialog(this);

            this.FindControl<Image>("PlotContainer").Source = new Avalonia.Media.Imaging.Bitmap(tmpFileName);
        }

        string GetGhostScript()
        {
            string ghostScript = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                ghostScript = Path.Combine(ghostScript, "gswin64c.exe");
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                ghostScript = Path.Combine(ghostScript, "gs-927-linux-x86_64");
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
            {
                ghostScript = Path.Combine(ghostScript, "gs-mac");
            }

            return ghostScript;
        }

        private void FitButtonClicked(object sender, RoutedEventArgs e)
        {
            this.FindControl<ZoomBorder>("TreeContainer").Uniform();
        }

        private void PropertyChangedEvent(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == WidthProperty || e.Property == HeightProperty)
            {
                Thread thr = new Thread(() =>
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        this.FindControl<ZoomBorder>("TreeContainer").Uniform();
                    });
                });

                thr.Start();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void SavePlotClicked(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog() { Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Name = "PDF document", Extensions = new List<string>() { "pdf" } }, new FileDialogFilter() { Name = "PNG image", Extensions = new List<string>() { "png" } } }, Title = "Save plot" };

            string result = await dialog.ShowAsync(this);

            if (!string.IsNullOrEmpty(result))
            {



                if (result.EndsWith(".pdf"))
                {
                    if (File.Exists(result))
                    {
                        File.Delete(result);
                    }

                    File.Move(FileName, result);
                    File.Delete(tmpFileName);
                    this.Close();
                }
                else if (result.EndsWith(".png"))
                {
                    string ghostScript = GetGhostScript();

                    string arguments = "-sDEVICE=pngalpha -sOutputFile=\"" + result + "\" -r300 -dBATCH -dNOPAUSE -dFirstPage=1 -dLastPage=1 " + FileName;

                    ProcessStartInfo info = new ProcessStartInfo(ghostScript, arguments);

                    EventWaitHandle handle = new EventWaitHandle(false, EventResetMode.ManualReset);

                    RenderingProgressWindow win = new RenderingProgressWindow(handle);

                    Thread thr = new Thread(() =>
                    {
                        handle.WaitOne();

                        Process proc = Process.Start(info);

                        proc.WaitForExit();

                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            win.Close();
                        });
                    });

                    thr.Start();

                    await win.ShowDialog(this);

                    File.Delete(FileName);
                    File.Delete(tmpFileName);
                    this.Close();
                }
                else
                {
                    MessageBox box = new MessageBox("Warning", "Unknown file extension!\nPlease make sure the file name ends with \".pdf\" if you want to save a PDF document or in \".png\" if you want to save a PNG image!");
                    await box.ShowDialog(this);
                }
            }
        }
    }
}
