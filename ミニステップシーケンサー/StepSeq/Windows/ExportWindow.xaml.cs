using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using StepSeq.Audio;

namespace StepSeq.Windows;

public partial class ExportWindow : Window
{
    private readonly SequencerState _state;
    private readonly int _bars;
    private readonly int _loopStart;
    private readonly int _loopEnd;
    private string _savePath;

    public ExportWindow(SequencerState state, int bars, int loopStart, int loopEnd)
    {
        InitializeComponent();
        _state = state;
        _bars = bars;
        _loopStart = loopStart;
        _loopEnd = loopEnd;

        RangeAllOption.Content = $"全体（Bar 1–{bars}）";
        RangeLoopOption.Content = $"ループ範囲のみ（Bar {loopStart}–{loopEnd}）";

        _savePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "pattern_01.wav");
        SavePathBox.Text = _savePath;
    }

    private void FormatOption_Changed(object sender, RoutedEventArgs e)
    {
        if (SavePathBox == null) return;
        string ext = WavOption.IsChecked == true ? ".wav" : ".mid";
        _savePath = System.IO.Path.ChangeExtension(_savePath, ext);
        SavePathBox.Text = _savePath;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        bool isWav = WavOption.IsChecked == true;
        var dialog = new SaveFileDialog
        {
            Title = "書き出し先を選択",
            Filter = isWav ? "WAVファイル (*.wav)|*.wav" : "MIDIファイル (*.mid)|*.mid",
            DefaultExt = isWav ? ".wav" : ".mid",
            FileName = System.IO.Path.GetFileName(_savePath),
            InitialDirectory = System.IO.Path.GetDirectoryName(_savePath)
        };
        if (dialog.ShowDialog(this) == true)
        {
            _savePath = dialog.FileName;
            SavePathBox.Text = _savePath;
        }
    }

    private List<int> BuildBarSequence()
    {
        var list = new List<int>();
        if (RangeAllOption.IsChecked == true)
        {
            for (int b = 1; b <= _bars; b++) list.Add(b);
        }
        else
        {
            for (int b = _loopStart; b <= _loopEnd; b++) list.Add(b);
        }
        return list;
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (MidOption.IsChecked == true)
        {
            StatusText.Text = "MIDI書き出しはこのバージョンでは未実装です（次のバージョンで対応予定）。";
            return;
        }

        var filter = new TrackFilter
        {
            Drum = IncludeDrumCheck.IsChecked == true,
            Bass = IncludeBassCheck.IsChecked == true,
            Keys = IncludeKeysCheck.IsChecked == true
        };
        var barSequence = BuildBarSequence();
        if (barSequence.Count == 0)
        {
            StatusText.Text = "書き出す小節がありません。";
            return;
        }

        ExportButton.IsEnabled = false;
        CancelButton.IsEnabled = false;
        StatusText.Text = "書き出し中…";

        string path = _savePath;
        try
        {
            await Task.Run(() => OfflineRenderer.RenderToWav(path, _state, barSequence, filter));
            StatusText.Text = $"{System.IO.Path.GetFileName(path)} を書き出しました。";
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            StatusText.Text = "書き出しに失敗しました: " + ex.Message;
        }
        finally
        {
            ExportButton.IsEnabled = true;
            CancelButton.IsEnabled = true;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
