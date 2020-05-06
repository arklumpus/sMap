using Accord;
using Avalonia;
using Avalonia.Controls;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace sMap_GUI
{
    class NumericMenuItem
    {
        public double Minimum { get; set; } = double.NegativeInfinity;
        public double Maximum { get; set; } = double.PositiveInfinity;

        private double _value = 0;
        public double Value
        {
            get { return _value; }
            set
            {
                _value = value;
                UpdateValue();
            }
        }
        public double Increment { get; set; } = 1;

        private string _formatString = "";
        public string FormatString
        {
            get { return _formatString; }
            set
            {
                _formatString = value;
                UpdateValue();
            }
        }

        public MenuItem Item { get; }

        public event EventHandler ValueChanged;

        public object Icon
        {
            get
            {
                return Item.Icon;
            }

            set
            {
                Item.Icon = value;
            }
        }

        public NumericMenuItem(Window parent) : base()
        {
            Item = new MenuItem() { Width = 100 };
            Item.Header = new TextBlock();
            UpdateValue();

            Item.Click += async (s, e) =>
            {
                Window win = new Window();
                win.Title = "Edit value";

                win.Width = 250;
                win.Height = 100;

                Grid grd = new Grid() { Margin = new Avalonia.Thickness(10) };
                grd.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
                grd.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));
                grd.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
                grd.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));
                grd.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));

                grd.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));
                grd.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));

                NumericUpDown nud = new NumericUpDown() { Minimum = Minimum, Maximum = Maximum, Value = Value, Increment = Increment, FormatString = FormatString, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                Grid.SetColumnSpan(nud, 5);

                grd.Children.Add(nud);

                Button okButton = new Button() { Content = "OK", Width = 100, Margin = new Thickness(0, 10, 5, 0) };
                Grid.SetColumn(okButton, 1);
                Grid.SetRow(okButton, 1);
                grd.Children.Add(okButton);

                Button cancelButton = new Button() { Content = "Cancel", Width = 100, Margin = new Thickness(5, 10, 0, 0) };
                Grid.SetColumn(cancelButton, 3);
                Grid.SetRow(cancelButton, 1);
                grd.Children.Add(cancelButton);

                bool result = false;

                cancelButton.Click += (s, e) =>
                {
                    win.Close();
                };

                okButton.Click += (s, e) =>
                {
                    result = true;
                    win.Close();
                };

                win.Content = grd;
                await win.ShowDialog(parent);

                if (result)
                {
                    this.Value = nud.Value;
                    ValueChanged?.Invoke(this, new EventArgs());
                }
            };
        }

        private void UpdateValue()
        {
            ((TextBlock)Item.Header).Text = this.Value.ToString(FormatString);
        }
    }
}
