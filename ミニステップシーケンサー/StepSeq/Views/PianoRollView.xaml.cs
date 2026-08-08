using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using StepSeq.Models;

namespace StepSeq.Views;

public partial class PianoRollView : UserControl
{
    public event Action? OctaveUpRequested;
    public event Action? OctaveDownRequested;

    public PianoRollView()
    {
        InitializeComponent();
    }

    public void Render(
        List<NoteRowInfo> rows,
        Dictionary<int, bool[]> barPattern,
        bool[]? sustainSteps,
        TrackType trackType,
        string octaveLabel,
        Action<int, int> onNoteToggle,
        Action<int>? onSustainToggle)
    {
        var lineBrush = (Brush)FindResource("LineBrush");
        var lineStrongBrush = (Brush)FindResource("LineStrongBrush");
        var beatBrush = (Brush)FindResource("BgCellBeatBrush");
        var cellBrush = (Brush)FindResource("BgCellBrush");
        var blackRowBrush = new SolidColorBrush(Color.FromRgb(0x1b, 0x1d, 0x23));
        var textBrush = (Brush)FindResource("TextBrush");
        var textMutedBrush = (Brush)FindResource("TextMutedBrush");
        var textDimBrush = (Brush)FindResource("TextDimBrush");
        var accent = trackType == TrackType.Keys ? (Brush)FindResource("AccentKeysBrush") : (Brush)FindResource("AccentBassBrush");
        var accentKeys = (Brush)FindResource("AccentKeysBrush");

        // ---- オクターブバー ----
        OctaveBar.Children.Clear();
        var downBtn = new Button { Content = "▼", Style = (Style)FindResource("StepperButton"), Width = 22, Height = 22 };
        downBtn.Click += (_, _) => OctaveDownRequested?.Invoke();
        var label = new TextBlock
        {
            Text = octaveLabel, FontFamily = new FontFamily("Consolas"), Foreground = textBrush,
            Margin = new Thickness(10, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center, FontSize = 11
        };
        var upBtn = new Button { Content = "▲", Style = (Style)FindResource("StepperButton"), Width = 22, Height = 22 };
        upBtn.Click += (_, _) => OctaveUpRequested?.Invoke();
        var hint = new TextBlock
        {
            Text = "半音単位でクロマチック入力", Foreground = textDimBrush, FontSize = 11,
            Margin = new Thickness(16, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center
        };
        OctaveBar.Children.Add(downBtn);
        OctaveBar.Children.Add(label);
        OctaveBar.Children.Add(upBtn);
        OctaveBar.Children.Add(hint);

        // ---- 音程行 ----
        RowsPanel.Children.Clear();
        foreach (var row in rows)
        {
            var rowGrid = new Grid { Height = 20 };
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var noteLabel = new TextBlock
            {
                Text = row.Name,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                Foreground = row.Root ? textBrush : (row.Black ? textDimBrush : textMutedBrush),
                FontWeight = row.Root ? FontWeights.Bold : FontWeights.Normal
            };
            Grid.SetColumn(noteLabel, 0);
            rowGrid.Children.Add(noteLabel);

            var stepsPanel = new StackPanel { Orientation = Orientation.Horizontal };
            Grid.SetColumn(stepsPanel, 1);

            bool[]? noteSteps = barPattern.TryGetValue(row.Midi, out var s) ? s : null;
            var rowBaseBrush = row.Black ? blackRowBrush : cellBrush;

            for (int step = 0; step < 16; step++)
            {
                bool on = noteSteps != null && noteSteps[step];
                var cell = new Border
                {
                    Width = 26,
                    Height = 18,
                    Margin = new Thickness(0, 0, 2, 0),
                    CornerRadius = new CornerRadius(2),
                    BorderThickness = new Thickness(1),
                    BorderBrush = on ? accent : lineBrush,
                    Background = on ? accent : (step % 4 == 0 ? beatBrush : rowBaseBrush),
                    Cursor = Cursors.Hand
                };
                int midi = row.Midi, st = step;
                cell.MouseLeftButtonUp += (_, _) => onNoteToggle(midi, st);
                stepsPanel.Children.Add(cell);
            }
            rowGrid.Children.Add(stepsPanel);
            RowsPanel.Children.Add(rowGrid);
        }

        // ---- サスティンレーン（キーボードのみ） ----
        if (trackType == TrackType.Keys && sustainSteps != null && onSustainToggle != null)
        {
            SustainLaneRoot.Visibility = Visibility.Visible;
            SustainLaneRoot.BorderBrush = lineStrongBrush;
            SustainLaneRoot.BorderThickness = new Thickness(0, 1, 0, 0);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var labelPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var dot = new Ellipse { Width = 14, Height = 14, Stroke = accentKeys, StrokeThickness = 2 };
            var sLabel = new TextBlock
            {
                Text = "サスティンペダル", Foreground = accentKeys, FontSize = 11,
                Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center
            };
            labelPanel.Children.Add(dot);
            labelPanel.Children.Add(sLabel);
            Grid.SetColumn(labelPanel, 0);
            grid.Children.Add(labelPanel);

            var sustainStepsPanel = new StackPanel { Orientation = Orientation.Horizontal };
            Grid.SetColumn(sustainStepsPanel, 1);
            for (int step = 0; step < 16; step++)
            {
                bool on = sustainSteps[step];
                var cell = new Border
                {
                    Width = 26,
                    Height = 16,
                    Margin = new Thickness(0, 0, 2, 0),
                    CornerRadius = new CornerRadius(2),
                    BorderThickness = new Thickness(1),
                    BorderBrush = on ? accentKeys : lineStrongBrush,
                    Background = on ? accentKeys : (step % 4 == 0 ? beatBrush : cellBrush),
                    Cursor = Cursors.Hand
                };
                int st = step;
                cell.MouseLeftButtonUp += (_, _) => onSustainToggle(st);
                sustainStepsPanel.Children.Add(cell);
            }
            grid.Children.Add(sustainStepsPanel);

            SustainLaneRoot.Child = grid;
        }
        else
        {
            SustainLaneRoot.Visibility = Visibility.Collapsed;
            SustainLaneRoot.Child = null;
        }
    }
}
