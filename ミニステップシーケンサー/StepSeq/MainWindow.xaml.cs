using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using StepSeq.Audio;
using StepSeq.Models;
using StepSeq.Views;
using StepSeq.Windows;

namespace StepSeq;

public partial class MainWindow : Window
{
    private readonly SequencerState _state = new();
    private readonly DrumGridView _drumGridView = new();
    private readonly PianoRollView _pianoRollView = new();

    private TrackType _currentTrack = TrackType.Drum;
    private int _currentBar = 1;

    private int _bassLowMidi = 36, _bassHighMidi = 60;
    private int _keysLowMidi = 36, _keysHighMidi = 60;

    private readonly Dictionary<TrackType, Button> _trackTabButtons = new();
    private DispatcherTimer? _statusTimer;

    public MainWindow()
    {
        InitializeComponent();

        _pianoRollView.OctaveUpRequested += () => ShiftOctave(12);
        _pianoRollView.OctaveDownRequested += () => ShiftOctave(-12);

        BuildTrackTabs();
        RenderBarTabs();
        SwitchTrack(TrackType.Drum);
        UpdateLoopToggleVisual();

        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
        _statusTimer.Tick += (_, _) => SyncPlaybackUi();
        _statusTimer.Start();

        Closed += (_, _) => AudioPlayer.Instance.Stop();
    }

    // ===================== トラックタブ =====================

    private void BuildTrackTabs()
    {
        TrackTabPanel.Children.Clear();
        _trackTabButtons.Clear();

        AddTrackTab(TrackType.Drum, "ドラムキット", (Brush)FindResource("AccentDrumBrush"));
        AddTrackTab(TrackType.Bass, "ベース", (Brush)FindResource("AccentBassBrush"));
        AddTrackTab(TrackType.Keys, "キーボード", (Brush)FindResource("AccentKeysBrush"));
    }

    private void AddTrackTab(TrackType type, string label, Brush dotColor)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        panel.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = dotColor, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center });
        panel.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontSize = 13 });

        var btn = new Button
        {
            Content = panel,
            Padding = new Thickness(20, 10, 20, 10),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0, 0, 0, 2),
            BorderBrush = Brushes.Transparent,
            Cursor = System.Windows.Input.Cursors.Hand,
            Foreground = (Brush)FindResource("TextMutedBrush")
        };
        btn.Template = FlatBorderTemplate();
        btn.Click += (_, _) => SwitchTrack(type);
        _trackTabButtons[type] = btn;
        TrackTabPanel.Children.Add(btn);
    }

    private ControlTemplate FlatBorderTemplate()
    {
        var template = new ControlTemplate(typeof(Button));
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
        borderFactory.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
        borderFactory.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
        borderFactory.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
        var presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
        presenterFactory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenterFactory.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        borderFactory.AppendChild(presenterFactory);
        template.VisualTree = borderFactory;
        return template;
    }

    private void UpdateTrackTabVisuals()
    {
        var accentByTrack = new Dictionary<TrackType, Brush>
        {
            [TrackType.Drum] = (Brush)FindResource("AccentDrumBrush"),
            [TrackType.Bass] = (Brush)FindResource("AccentBassBrush"),
            [TrackType.Keys] = (Brush)FindResource("AccentKeysBrush"),
        };
        var mutedText = (Brush)FindResource("TextMutedBrush");
        var normalText = (Brush)FindResource("TextBrush");

        foreach (var kv in _trackTabButtons)
        {
            bool active = kv.Key == _currentTrack;
            kv.Value.BorderBrush = active ? accentByTrack[kv.Key] : Brushes.Transparent;
            kv.Value.Foreground = active ? normalText : mutedText;
        }
    }

    private void SwitchTrack(TrackType type)
    {
        _currentTrack = type;
        UpdateTrackTabVisuals();

        string accentKey = type switch
        {
            TrackType.Drum => "AccentDrumBrush",
            TrackType.Bass => "AccentBassBrush",
            TrackType.Keys => "AccentKeysBrush",
            _ => "AccentDrumBrush"
        };
        Resources["InstrumentAccentBrush"] = (Brush)FindResource(accentKey);

        RenderInstruments();

        if (type == TrackType.Drum)
        {
            EditorHost.Content = _drumGridView;
            RefreshDrumGrid();
        }
        else
        {
            EditorHost.Content = _pianoRollView;
            RefreshPianoRoll();
        }

        UpdateStatusNote();
        RefreshCopySelectors();
    }

    // ===================== 音色選択 =====================

    private void RenderInstruments()
    {
        InstrumentPanel.Children.Clear();
        var accent = (Brush)FindResource("InstrumentAccentBrush");
        var baseBg = (Brush)FindResource("BgBaseBrush");
        var activeBg = (Brush)FindResource("BgPanelBrush");
        var mutedText = new SolidColorBrush(Color.FromRgb(0xB8, 0xBE, 0xC8)); // 非選択時も読みやすい明るめのグレー
        var normalText = (Brush)FindResource("TextBrush");
        var lineStrong = (Brush)FindResource("LineStrongBrush");

        foreach (var inst in InstrumentCatalog.ByTrack[_currentTrack])
        {
            bool active = _state.ActiveInstrument[_currentTrack] == inst.Id;
            var btn = new Button
            {
                Content = inst.Name,
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 8, 0),
                Background = active ? activeBg : baseBg,
                Foreground = active ? normalText : mutedText,
                BorderBrush = active ? accent : lineStrong,
                BorderThickness = new Thickness(1),
                FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal,
                Cursor = System.Windows.Input.Cursors.Hand,
                Template = FlatBorderTemplateRounded()
            };
            string id = inst.Id;
            btn.Click += (_, _) =>
            {
                _state.ActiveInstrument[_currentTrack] = id;
                RenderInstruments();
                UpdateStatusNote();
            };
            InstrumentPanel.Children.Add(btn);
        }
    }

    private ControlTemplate FlatBorderTemplateRounded()
    {
        var template = new ControlTemplate(typeof(Button));
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        borderFactory.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
        borderFactory.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
        borderFactory.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
        borderFactory.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
        var presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
        presenterFactory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenterFactory.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        borderFactory.AppendChild(presenterFactory);
        template.VisualTree = borderFactory;
        return template;
    }

    // ===================== ドラムグリッド / ピアノロール =====================

    private void RefreshDrumGrid()
    {
        var data = _state.Drum.GetOrCreate(_currentBar);
        _drumGridView.Render(data, OnDrumStepToggle);
    }

    private void OnDrumStepToggle(int row, int step)
    {
        var data = _state.Drum.GetOrCreate(_currentBar);
        data[row][step] = !data[row][step];
        RefreshDrumGrid();
    }

    private void RefreshPianoRoll()
    {
        bool isKeys = _currentTrack == TrackType.Keys;
        var store = isKeys ? _state.Keys : _state.Bass;
        int low = isKeys ? _keysLowMidi : _bassLowMidi;
        int high = isKeys ? _keysHighMidi : _bassHighMidi;
        var rows = NoteRange.Build(low, high);
        var barPattern = store.GetOrCreate(_currentBar);
        bool[]? sustain = isKeys ? _state.Sustain.GetOrCreate(_currentBar) : null;
        string label = $"オクターブ: {NoteRange.NameOf(low)} – {NoteRange.NameOf(high)}";

        _pianoRollView.Render(rows, barPattern, sustain, _currentTrack, label, OnNoteToggle, isKeys ? OnSustainToggle : null);
    }

    private void OnNoteToggle(int midi, int step)
    {
        var store = _currentTrack == TrackType.Keys ? _state.Keys : _state.Bass;
        var steps = store.GetOrCreateNote(_currentBar, midi);
        steps[step] = !steps[step];
        RefreshPianoRoll();
    }

    private void OnSustainToggle(int step)
    {
        var arr = _state.Sustain.GetOrCreate(_currentBar);
        arr[step] = !arr[step];
        RefreshPianoRoll();
    }

    private void ShiftOctave(int delta)
    {
        if (_currentTrack == TrackType.Bass)
        {
            _bassLowMidi = Math.Clamp(_bassLowMidi + delta, 0, 103);
            _bassHighMidi = _bassLowMidi + 24;
        }
        else if (_currentTrack == TrackType.Keys)
        {
            _keysLowMidi = Math.Clamp(_keysLowMidi + delta, 0, 103);
            _keysHighMidi = _keysLowMidi + 24;
        }
        else return;

        RefreshPianoRoll();
    }

    private void RefreshCurrentGrid()
    {
        if (_currentTrack == TrackType.Drum) RefreshDrumGrid(); else RefreshPianoRoll();
    }

    // ===================== バータブ =====================

    private void RenderBarTabs()
    {
        BarTabPanel.Children.Clear();
        var activeBg = (Brush)FindResource("BgCellBeatBrush");
        var normalBg = (Brush)FindResource("BgPanel2Brush");
        var normalText = (Brush)FindResource("TextMutedBrush");
        var activeText = (Brush)FindResource("TextBrush");
        var lineStrong = (Brush)FindResource("LineStrongBrush");
        var loopBrush = (Brush)FindResource("AccentLoopBrush");

        for (int b = 1; b <= _state.Bars; b++)
        {
            bool inLoop = b >= _state.LoopStart && b <= _state.LoopEnd;
            bool active = b == _currentBar;
            var btn = new Button
            {
                Content = b.ToString(),
                Width = 28,
                Height = 24,
                Margin = new Thickness(0, 0, 4, 0),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                Background = active ? activeBg : normalBg,
                Foreground = active ? activeText : normalText,
                BorderBrush = inLoop ? loopBrush : lineStrong,
                BorderThickness = new Thickness(1, 1, 1, inLoop ? 3 : 1),
                Cursor = System.Windows.Input.Cursors.Hand,
                Template = FlatBorderTemplateRounded()
            };
            int bb = b;
            btn.Click += (_, _) =>
            {
                _currentBar = bb;
                RenderBarTabs();
                RefreshCurrentGrid();
                UpdateStatusNote();
                RefreshCopySelectors();
            };
            BarTabPanel.Children.Add(btn);
        }

        LoopSummaryText.Text = $"🔁 Bar {_state.LoopStart}–{_state.LoopEnd}";
        RefreshCopySelectors();
    }

    // ===================== 小節コピー =====================

    private void RefreshCopySelectors()
    {
        CopySourceCombo.Items.Clear();
        CopyTargetCombo.Items.Clear();

        CopyTargetCombo.Items.Add(new ComboBoxItem { Content = "すべての小節", Tag = null });
        for (int b = 1; b <= _state.Bars; b++)
        {
            CopySourceCombo.Items.Add(new ComboBoxItem { Content = $"Bar {b}", Tag = b });
            CopyTargetCombo.Items.Add(new ComboBoxItem { Content = $"Bar {b}", Tag = b });
        }

        CopySourceCombo.SelectedIndex = Math.Clamp(_currentBar - 1, 0, Math.Max(0, _state.Bars - 1));
        CopyTargetCombo.SelectedIndex = 0;

        CopyHintText.Text = $"現在のトラック（{TrackName(_currentTrack)}）のパターンのみコピーします";
    }

    private void CopyExecButton_Click(object sender, RoutedEventArgs e)
    {
        if (CopySourceCombo.SelectedItem is not ComboBoxItem srcItem || srcItem.Tag is not int src)
        {
            MessageBox.Show(this, "コピー元の小節を選択してください。", "小節コピー");
            return;
        }
        if (CopyTargetCombo.SelectedItem is not ComboBoxItem tgtItem)
        {
            MessageBox.Show(this, "コピー先を選択してください。", "小節コピー");
            return;
        }

        List<int> targets;
        if (tgtItem.Tag is int singleTarget)
        {
            targets = new List<int> { singleTarget }.Where(b => b != src).ToList();
        }
        else
        {
            targets = Enumerable.Range(1, _state.Bars).Where(b => b != src).ToList();
        }

        if (targets.Count == 0)
        {
            StatusNoteText.Text = "コピー元と同じ小節は指定できません。";
            return;
        }

        foreach (int t in targets)
        {
            switch (_currentTrack)
            {
                case TrackType.Drum:
                    _state.Drum.Set(t, _state.Drum.Clone(src));
                    break;
                case TrackType.Bass:
                    _state.Bass.Set(t, _state.Bass.Clone(src));
                    break;
                case TrackType.Keys:
                    _state.Keys.Set(t, _state.Keys.Clone(src));
                    _state.Sustain.Set(t, _state.Sustain.Clone(src));
                    break;
            }
        }

        if (targets.Contains(_currentBar)) RefreshCurrentGrid();
        RenderBarTabs();

        string targetDesc = tgtItem.Tag == null ? "全小節" : $"Bar {tgtItem.Tag}";
        StatusNoteText.Text = $"[{TrackName(_currentTrack)}] Bar {src} を {targetDesc} にコピーしました。";
    }

    private static string TrackName(TrackType t) => t switch
    {
        TrackType.Drum => "ドラムキット",
        TrackType.Bass => "ベース",
        TrackType.Keys => "キーボード",
        _ => ""
    };

    private void UpdateStatusNote()
    {
        string instName = InstrumentCatalog.ByTrack[_currentTrack].First(i => i.Id == _state.ActiveInstrument[_currentTrack]).Name;
        StatusNoteText.Text = $"Bar {_currentBar} を編集中（{TrackName(_currentTrack)} / {instName}）";
    }

    // ===================== トランスポート: テンポ/小節/スウィング/マスター =====================

    private void TempoUp_Click(object sender, RoutedEventArgs e) => SetTempo(_state.Tempo + 1);
    private void TempoDown_Click(object sender, RoutedEventArgs e) => SetTempo(_state.Tempo - 1);

    private void SetTempo(int value)
    {
        _state.Tempo = Math.Clamp(value, 20, 300);
        TempoBox.Text = _state.Tempo.ToString();
    }

    private void TempoBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        // 数字以外の入力はそもそも受け付けない（誤入力でコミットに失敗するのを防ぐ）
        e.Handled = !e.Text.All(char.IsDigit);
    }

    private void TempoBox_GotFocus(object sender, RoutedEventArgs e)
    {
        TempoBox.SelectAll();
    }

    private void TempoBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            CommitTempoBox();
            System.Windows.Input.Keyboard.ClearFocus();
            e.Handled = true;
        }
    }
    private void TempoBox_LostFocus(object sender, RoutedEventArgs e) => CommitTempoBox();

    private void CommitTempoBox()
    {
        string text = TempoBox.Text.Trim();
        if (int.TryParse(text, out int v)) SetTempo(v);
        else TempoBox.Text = _state.Tempo.ToString();
    }

    private void BarsUp_Click(object sender, RoutedEventArgs e) => SetBars(_state.Bars + 1);
    private void BarsDown_Click(object sender, RoutedEventArgs e) => SetBars(_state.Bars - 1);

    private void SetBars(int value)
    {
        _state.Bars = Math.Clamp(value, 1, 64);
        BarsText.Text = _state.Bars.ToString();
        _state.LoopEnd = Math.Min(_state.LoopEnd, _state.Bars);
        _state.LoopStart = Math.Min(_state.LoopStart, _state.LoopEnd);
        LoopStartText.Text = _state.LoopStart.ToString();
        LoopEndText.Text = _state.LoopEnd.ToString();
        _currentBar = Math.Min(_currentBar, _state.Bars);
        RenderBarTabs();
        RefreshCurrentGrid();
        UpdateStatusNote();
    }

    private void SwingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _state.SwingPercent = (int)e.NewValue;
        if (SwingText != null) SwingText.Text = $"{_state.SwingPercent}%";
    }

    private void MasterSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _state.MasterVolume = e.NewValue / 100.0;
        if (MasterText != null) MasterText.Text = $"{(int)e.NewValue}%";
    }

    private void DrumVolSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _state.TrackVolume[TrackType.Drum] = e.NewValue / 100.0;
        if (DrumVolText != null) DrumVolText.Text = $"{(int)e.NewValue}%";
    }
    private void BassVolSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _state.TrackVolume[TrackType.Bass] = e.NewValue / 100.0;
        if (BassVolText != null) BassVolText.Text = $"{(int)e.NewValue}%";
    }
    private void KeysVolSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _state.TrackVolume[TrackType.Keys] = e.NewValue / 100.0;
        if (KeysVolText != null) KeysVolText.Text = $"{(int)e.NewValue}%";
    }

    // ===================== ループ =====================

    private void LoopToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _state.LoopOn = !_state.LoopOn;
        UpdateLoopToggleVisual();
    }

    private void UpdateLoopToggleVisual()
    {
        if (_state.LoopOn)
        {
            LoopToggleButton.Background = (Brush)FindResource("AccentLoopDimBrush");
            LoopToggleButton.Foreground = (Brush)FindResource("AccentLoopBrush");
            LoopToggleButton.BorderBrush = (Brush)FindResource("AccentLoopBrush");
        }
        else
        {
            LoopToggleButton.Background = (Brush)FindResource("BgPanel2Brush");
            LoopToggleButton.Foreground = (Brush)FindResource("TextMutedBrush");
            LoopToggleButton.BorderBrush = (Brush)FindResource("LineStrongBrush");
        }
    }

    private void LoopStartUp_Click(object sender, RoutedEventArgs e) => SetLoopStart(_state.LoopStart + 1);
    private void LoopStartDown_Click(object sender, RoutedEventArgs e) => SetLoopStart(_state.LoopStart - 1);
    private void LoopEndUp_Click(object sender, RoutedEventArgs e) => SetLoopEnd(_state.LoopEnd + 1);
    private void LoopEndDown_Click(object sender, RoutedEventArgs e) => SetLoopEnd(_state.LoopEnd - 1);

    private void SetLoopStart(int value)
    {
        _state.LoopStart = Math.Clamp(value, 1, _state.LoopEnd);
        LoopStartText.Text = _state.LoopStart.ToString();
        RenderBarTabs();
    }
    private void SetLoopEnd(int value)
    {
        _state.LoopEnd = Math.Clamp(value, _state.LoopStart, _state.Bars);
        LoopEndText.Text = _state.LoopEnd.ToString();
        RenderBarTabs();
    }

    // ===================== 再生 =====================

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (AudioPlayer.Instance.IsPlaying)
        {
            AudioPlayer.Instance.Stop();
        }
        else
        {
            AudioPlayer.Instance.Start(_state, _state.LoopOn ? _state.LoopStart : 1);
        }
        SyncPlaybackUi();
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        AudioPlayer.Instance.Stop();
        PlaybackStatusText.Text = "";
        SyncPlaybackUi();
    }

    private void SyncPlaybackUi()
    {
        bool playing = AudioPlayer.Instance.IsPlaying;
        PlayButton.Content = playing ? "⏸" : "▶";
        PlayButton.Background = playing ? new SolidColorBrush(Color.FromRgb(0x3a, 0x5a, 0x3f)) : (Brush)FindResource("BgPanel2Brush");
        PlayButton.Foreground = playing ? new SolidColorBrush(Color.FromRgb(0xc7, 0xf0, 0xce)) : (Brush)FindResource("TextBrush");

        if (playing && AudioPlayer.Instance.Engine is { } engine)
        {
            PlaybackStatusText.Text = $"● Bar {engine.CurrentBar} / Step {engine.CurrentStep + 1}";
        }
        else if (!playing)
        {
            PlaybackStatusText.Text = "";
        }
    }

    // ===================== 保存 / 読み込み =====================

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "プロジェクトを保存",
            Filter = "JSONファイル (*.json)|*.json",
            DefaultExt = ".json",
            FileName = "stepseq_project.json"
        };
        if (dialog.ShowDialog(this) != true) return;

        var data = new ProjectData
        {
            Tempo = _state.Tempo,
            Bars = _state.Bars,
            SwingPercent = _state.SwingPercent,
            LoopOn = _state.LoopOn,
            LoopStart = _state.LoopStart,
            LoopEnd = _state.LoopEnd,
            MasterVolume = _state.MasterVolume,
            BassLowMidi = _bassLowMidi,
            KeysLowMidi = _keysLowMidi,
            ActiveInstrument = _state.ActiveInstrument.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            TrackVolume = _state.TrackVolume.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            DrumData = _state.Drum.Raw.ToDictionary(kv => kv.Key, kv => kv.Value),
            BassData = _state.Bass.Raw.ToDictionary(kv => kv.Key, kv => kv.Value),
            KeysData = _state.Keys.Raw.ToDictionary(kv => kv.Key, kv => kv.Value),
            SustainData = _state.Sustain.Raw.ToDictionary(kv => kv.Key, kv => kv.Value),
        };

        try
        {
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dialog.FileName, json);
            StatusNoteText.Text = $"{System.IO.Path.GetFileName(dialog.FileName)} として保存しました。";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "保存に失敗しました: " + ex.Message, "エラー");
        }
    }

    private void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "プロジェクトを読み込み",
            Filter = "JSONファイル (*.json)|*.json"
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            string json = File.ReadAllText(dialog.FileName);
            var data = JsonSerializer.Deserialize<ProjectData>(json);
            if (data == null) throw new Exception("ファイルの内容を解釈できませんでした。");
            ApplyProjectData(data);
            StatusNoteText.Text = $"{System.IO.Path.GetFileName(dialog.FileName)} を読み込みました。";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "読み込みに失敗しました: " + ex.Message, "エラー");
        }
    }

    private void ApplyProjectData(ProjectData data)
    {
        _state.Tempo = data.Tempo;
        _state.Bars = data.Bars;
        _state.SwingPercent = data.SwingPercent;
        _state.LoopOn = data.LoopOn;
        _state.LoopStart = data.LoopStart;
        _state.LoopEnd = data.LoopEnd;
        _state.MasterVolume = data.MasterVolume;
        _bassLowMidi = data.BassLowMidi; _bassHighMidi = _bassLowMidi + 24;
        _keysLowMidi = data.KeysLowMidi; _keysHighMidi = _keysLowMidi + 24;

        foreach (var kv in data.ActiveInstrument)
            if (Enum.TryParse<TrackType>(kv.Key, out var tt)) _state.ActiveInstrument[tt] = kv.Value;
        foreach (var kv in data.TrackVolume)
            if (Enum.TryParse<TrackType>(kv.Key, out var tt)) _state.TrackVolume[tt] = kv.Value;

        _state.Drum.LoadRaw(data.DrumData ?? new Dictionary<int, bool[][]>());
        _state.Bass.LoadRaw(data.BassData ?? new Dictionary<int, Dictionary<int, bool[]>>());
        _state.Keys.LoadRaw(data.KeysData ?? new Dictionary<int, Dictionary<int, bool[]>>());
        _state.Sustain.LoadRaw(data.SustainData ?? new Dictionary<int, bool[]>());

        _currentBar = 1;

        TempoBox.Text = _state.Tempo.ToString();
        BarsText.Text = _state.Bars.ToString();
        SwingSlider.Value = _state.SwingPercent;
        SwingText.Text = $"{_state.SwingPercent}%";
        MasterSlider.Value = _state.MasterVolume * 100;
        MasterText.Text = $"{(int)(_state.MasterVolume * 100)}%";
        LoopStartText.Text = _state.LoopStart.ToString();
        LoopEndText.Text = _state.LoopEnd.ToString();
        UpdateLoopToggleVisual();

        DrumVolSlider.Value = _state.TrackVolume[TrackType.Drum] * 100;
        BassVolSlider.Value = _state.TrackVolume[TrackType.Bass] * 100;
        KeysVolSlider.Value = _state.TrackVolume[TrackType.Keys] * 100;
        DrumVolText.Text = $"{(int)(_state.TrackVolume[TrackType.Drum] * 100)}%";
        BassVolText.Text = $"{(int)(_state.TrackVolume[TrackType.Bass] * 100)}%";
        KeysVolText.Text = $"{(int)(_state.TrackVolume[TrackType.Keys] * 100)}%";

        RenderBarTabs();
        SwitchTrack(TrackType.Drum);
    }

    // ===================== 書き出し =====================

    private void OpenExportButton_Click(object sender, RoutedEventArgs e)
    {
        var win = new ExportWindow(_state, _state.Bars, _state.LoopStart, _state.LoopEnd) { Owner = this };
        win.ShowDialog();
    }
}
