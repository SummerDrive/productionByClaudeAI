using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using StepSeq.Models;

namespace StepSeq.Views;

public partial class DrumGridView : UserControl
{
    public DrumGridView()
    {
        InitializeComponent();
    }

    /// <summary>bar分の12行x16ステップのON/OFF状態を渡して描画。セルクリック時に onToggle(row, step) を呼ぶ。</summary>
    public void Render(bool[][] data, Action<int, int> onToggle)
    {
        var lineBrush = (Brush)FindResource("LineBrush");
        var beatBrush = (Brush)FindResource("BgCellBeatBrush");
        var cellBrush = (Brush)FindResource("BgCellBrush");
        var onBrush = (Brush)FindResource("AccentDrumBrush");
        var textBrush = (Brush)FindResource("TextBrush");

        Root.Children.Clear();

        for (int r = 0; r < DrumRows.Count; r++)
        {
            var rowGrid = new Grid { Height = 34 };
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var label = new TextBlock
            {
                Text = DrumRows.All[r].Name,
                Foreground = textBrush,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(label, 0);
            rowGrid.Children.Add(label);

            var stepsPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(stepsPanel, 1);

            for (int s = 0; s < DrumRows.Steps; s++)
            {
                bool on = data[r][s];
                var cell = new Border
                {
                    Width = 26,
                    Height = 26,
                    Margin = new Thickness(0, 0, 2, 0),
                    CornerRadius = new CornerRadius(3),
                    BorderThickness = new Thickness(1),
                    BorderBrush = on ? onBrush : lineBrush,
                    Background = on ? onBrush : (s % 4 == 0 ? beatBrush : cellBrush),
                    Cursor = Cursors.Hand
                };
                int rr = r, ss = s;
                cell.MouseLeftButtonUp += (_, _) => onToggle(rr, ss);
                stepsPanel.Children.Add(cell);
            }
            rowGrid.Children.Add(stepsPanel);

            var rowBorder = new Border
            {
                BorderBrush = lineBrush,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Child = rowGrid
            };
            Root.Children.Add(rowBorder);
        }
    }
}
