using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace sMap_GUI
{
    public class TipContainer : UserControl
    {
        public enum Tips
        {
            InputDataFile = 1,
            TreeFile = 2,
            SummaryTree = 3,
            DependencyModel = 4,
            PisRatesCondProbs = 5,
            RunningAnalysis = 6,
            AnalysisResults = 7
        }

        public TipContainer()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        SolidColorBrush[] brushes = new SolidColorBrush[] {
            null,
            new SolidColorBrush(Color.FromArgb(255, 163, 73, 164)),
            new SolidColorBrush(Color.FromArgb(255, 163, 73, 164)),
            new SolidColorBrush(Color.FromArgb(255, 163, 73, 164)),
            new SolidColorBrush(Color.FromArgb(255, 63, 72, 204)),
            new SolidColorBrush(Color.FromArgb(255, 0, 162, 232)),
            new SolidColorBrush(Color.FromArgb(255, 255, 127, 39)),
            new SolidColorBrush(Color.FromArgb(255, 237, 28, 36))
        };

        int currTip = 0;

        public void SetTip(Tips tip)
        {
            int ind = (int)tip;
            currTip = ind;

            for (int i = 1; i <= 7; i++)
            {
                this.FindControl<TextBlock>("Tip" + i.ToString() + "Header").IsVisible = false;
                this.FindControl<StackPanel>("Tip" + i.ToString()).IsVisible = false;
            }

            this.FindControl<TextBlock>("Tip" + ind.ToString() + "Header").IsVisible = true;
            this.FindControl<StackPanel>("Tip" + ind.ToString()).IsVisible = true;

            this.FindControl<TextBlock>("Tip" + ind.ToString() + "Header").Foreground = brushes[ind];
            this.FindControl<TextBlock>("TipsHeader").Foreground = brushes[ind];
            this.FindControl<Path>("BulbPath").Fill = brushes[ind];

            if (ind == 1)
            {
                this.FindControl<Button>("PreviousTipButton").IsVisible = false;
            }
            else
            {
                this.FindControl<Button>("PreviousTipButton").IsVisible = true;
            }

            if (ind == 7)
            {
                this.FindControl<Button>("NextTipButton").IsVisible = false;
            }
            else
            {
                this.FindControl<Button>("NextTipButton").IsVisible = true;
            }
        }

        private void PreviousTipClicked(object sender, RoutedEventArgs e)
        {
            this.SetTip((Tips)(currTip - 1));
        }

        private void NextTipClicked(object sender, RoutedEventArgs e)
        {
            this.SetTip((Tips)(currTip + 1));
        }
    }
}
