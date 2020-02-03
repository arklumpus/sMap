using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;

namespace sMap_GUI
{
    public class RenameStatesWindow : Window
    {
        public bool Result { get; private set; } = false;

        public RenameStatesWindow()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        string[] AllStates;
        TextBox[] NewNameBoxes;

        public Dictionary<string, string> RenamedStates { get; set; }

        public RenameStatesWindow(string[] allStates, Dictionary<string, string> renamedStates = null)
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            AllStates = allStates;

            RenamedStates = new Dictionary<string, string>();

            if (renamedStates != null)
            {
                for (int i = 0; i < allStates.Length; i++)
                {
                    if (renamedStates.TryGetValue(allStates[i], out string newName))
                    {
                        RenamedStates[allStates[i]] = newName;
                    }
                }
            }

            Grid statesContainer = this.FindControl<Grid>("statesContainer");

            NewNameBoxes = new TextBox[allStates.Length];

            for (int i = 0; i < allStates.Length; i++)
            {
                if (!RenamedStates.ContainsKey(allStates[i]))
                {
                    int firstInd = 0;

                    while (RenamedStates.ContainsValue(firstInd.ToString()))
                    {
                        firstInd++;
                    }

                    RenamedStates[allStates[i]] = firstInd.ToString();
                }

                statesContainer.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

                if (i % 2 == 0)
                {
                    Canvas can = new Canvas() { Background = new Avalonia.Media.SolidColorBrush(0xFFF0F0F0) };
                    Grid.SetRow(can, i);
                    Grid.SetColumnSpan(can, 2);
                    statesContainer.Children.Add(can);
                }

                TextBlock block = new TextBlock() { Text = allStates[i], TextAlignment = Avalonia.Media.TextAlignment.Center, Margin = new Thickness(0, 10, 0, 10), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };

                Grid.SetRow(block, i);

                statesContainer.Children.Add(block);

                TextBox box = new TextBox() { Text = RenamedStates[allStates[i]], Background = null, Margin = new Thickness(0, 10, 0, 10), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, TextAlignment = Avalonia.Media.TextAlignment.Center };
                NewNameBoxes[i] = box;
                Grid.SetRow(box, i);
                Grid.SetColumn(box, 1);
                statesContainer.Children.Add(box);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OKClicked(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < AllStates.Length; i++)
            {
                RenamedStates[AllStates[i]] = NewNameBoxes[i].Text;
            }

            this.Result = true;
            this.Close();
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            this.Result = false;
            this.Close();
        }
    }
}
