using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System.Collections.Generic;

namespace sMap_GUI
{
    public class ViewStepSizesWindow : Window
    {
        public ViewStepSizesWindow()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        public enum WindowType
        {
            BlueHeader, GreenHeader
        }

        public ViewStepSizesWindow(double[] stepSizes, List<string> parameterNames, List<string> realParameterNames, WindowType type)
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            if (type == WindowType.BlueHeader)
            {
                this.FindControl<StackPanel>("BlueHeader").IsVisible = true;
            }
            else
            {
                this.FindControl<StackPanel>("GreenHeader").IsVisible = true;
            }

            Grid mainContainer = this.FindControl<Grid>("StepSizesContainer");

            int parNameInd = 1;

            for (int i = 0; i < stepSizes.Length; i++)
            {
                mainContainer.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));
                
                if (i % 2 == 1)
                {
                    Canvas can = new Canvas() { Background = new SolidColorBrush(Color.FromArgb(255, 240, 240, 240)) };
                    Grid.SetRow(can, i);
                    Grid.SetColumnSpan(can, 4);
                    mainContainer.Children.Add(can);
                }

                string parName = parameterNames[parNameInd];
                string toolTip = "";

                if (realParameterNames[parNameInd][0] == 'M')
                {
                    string ind = realParameterNames[parNameInd].Substring(0, realParameterNames[parNameInd].IndexOf("{"));

                    parName = ind + " {...}";


                    while (realParameterNames.Count > parNameInd && realParameterNames[parNameInd].Substring(0, realParameterNames[parNameInd].IndexOf("{")) == ind)
                    {
                        toolTip += parameterNames[parNameInd] + ", ";
                        parNameInd++;
                    }

                    toolTip = toolTip.Substring(0, toolTip.Length - 2) ;
                }
                else
                {
                    parNameInd++;
                }

                TextBlock blk = new TextBlock() { Text = parName, FontWeight = Avalonia.Media.FontWeight.Bold, Margin = new Thickness(5, 5, 10, 5), TextAlignment = TextAlignment.Right, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                Grid.SetRow(blk, i);
                Grid.SetColumn(blk, 1);

                if (toolTip != "")
                {
                    ToolTip.SetTip(blk, new TextBlock() { Text = toolTip, FontWeight = FontWeight.Regular });
                }

                mainContainer.Children.Add(blk);

                TextBox blk2 = new TextBox() { Text = stepSizes[i].ToString(System.Globalization.CultureInfo.InvariantCulture), Margin = new Thickness(5, 5, 10, 5), Padding = new Thickness(0), BorderBrush = null, Background = null };
                Grid.SetRow(blk2, i);
                Grid.SetColumn(blk2, 2);
                mainContainer.Children.Add(blk2);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
