using System.Collections.Generic;
using StepSeq.Models;

namespace StepSeq.Audio;

/// <summary>
/// MainWindow が保持し、SequencerEngine（ライブ再生）と OfflineRenderer（書き出し）の
/// 両方から共有参照される、再生に必要な状態一式。
/// </summary>
public class SequencerState
{
    public int Tempo = 128;
    public int Bars = 4;
    public int SwingPercent = 20;
    public bool LoopOn = true;
    public int LoopStart = 1;
    public int LoopEnd = 4;

    public double MasterVolume = 0.8;

    public readonly Dictionary<TrackType, double> TrackVolume = new()
    {
        [TrackType.Drum] = 1.0,
        [TrackType.Bass] = 1.0,
        [TrackType.Keys] = 1.0,
    };

    public readonly Dictionary<TrackType, string> ActiveInstrument = new()
    {
        [TrackType.Drum] = "rock",
        [TrackType.Bass] = "wood",
        [TrackType.Keys] = "piano",
    };

    public readonly DrumPatternStore Drum = new();
    public readonly PitchPatternStore Bass = new();
    public readonly PitchPatternStore Keys = new();
    public readonly SustainStore Sustain = new();

    public double StepSeconds => 60.0 / Tempo / 4.0;
}

public struct TrackFilter
{
    public bool Drum;
    public bool Bass;
    public bool Keys;
    public static TrackFilter All => new() { Drum = true, Bass = true, Keys = true };
}
