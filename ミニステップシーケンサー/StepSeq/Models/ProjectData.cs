using System.Collections.Generic;

namespace StepSeq.Models;

/// <summary>プロジェクトファイル(JSON)の中身。System.Text.Json でそのままシリアライズ/デシリアライズする。</summary>
public class ProjectData
{
    public int Tempo { get; set; } = 128;
    public int Bars { get; set; } = 4;
    public int SwingPercent { get; set; } = 20;
    public bool LoopOn { get; set; } = true;
    public int LoopStart { get; set; } = 1;
    public int LoopEnd { get; set; } = 4;

    public Dictionary<string, string> ActiveInstrument { get; set; } = new();
    public Dictionary<string, double> TrackVolume { get; set; } = new();
    public double MasterVolume { get; set; } = 0.8;

    public int BassLowMidi { get; set; } = 36;
    public int KeysLowMidi { get; set; } = 36;

    public Dictionary<int, bool[][]> DrumData { get; set; } = new();
    public Dictionary<int, Dictionary<int, bool[]>> BassData { get; set; } = new();
    public Dictionary<int, Dictionary<int, bool[]>> KeysData { get; set; } = new();
    public Dictionary<int, bool[]> SustainData { get; set; } = new();
}
