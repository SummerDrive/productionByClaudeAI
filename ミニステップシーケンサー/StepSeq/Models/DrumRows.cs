using System.Collections.Generic;

namespace StepSeq.Models;

public enum DrumVoiceKind { Kick, Snare, Hat }

/// <summary>ドラム1音色ぶんの基本パラメータ。種類によって使うフィールドが異なる（JS版のp:{...}に対応）</summary>
public class DrumVoiceParams
{
    public double F0;      // Kick: 開始周波数
    public double F1;      // Kick: 終端周波数
    public double Tone;    // Snare: トーン周波数
    public double Decay = 0.2;
    public double Gain = 0.8;
    public double HighpassHz = 8000; // Hat: ハイパスカットオフ

    public DrumVoiceParams Clone() => (DrumVoiceParams)MemberwiseClone();
}

public class DrumRowInfo
{
    public string Name { get; init; } = "";
    public DrumVoiceKind Kind { get; init; }
    public DrumVoiceParams Params { get; init; } = new();
}

public static class DrumRows
{
    public static readonly List<DrumRowInfo> All = new()
    {
        new DrumRowInfo{ Name="Kick",      Kind=DrumVoiceKind.Kick,  Params=new DrumVoiceParams{ F0=150, F1=50,  Decay=0.25, Gain=0.9 } },
        new DrumRowInfo{ Name="Snare",     Kind=DrumVoiceKind.Snare, Params=new DrumVoiceParams{ Tone=200, Decay=0.16, Gain=0.75 } },
        new DrumRowInfo{ Name="Clap",      Kind=DrumVoiceKind.Snare, Params=new DrumVoiceParams{ Tone=340, Decay=0.12, Gain=0.6 } },
        new DrumRowInfo{ Name="Closed HH", Kind=DrumVoiceKind.Hat,   Params=new DrumVoiceParams{ Decay=0.045, Gain=0.35, HighpassHz=9000 } },
        new DrumRowInfo{ Name="Open HH",   Kind=DrumVoiceKind.Hat,   Params=new DrumVoiceParams{ Decay=0.22,  Gain=0.30, HighpassHz=7000 } },
        new DrumRowInfo{ Name="Low Tom",   Kind=DrumVoiceKind.Kick,  Params=new DrumVoiceParams{ F0=180, F1=90,  Decay=0.30, Gain=0.7 } },
        new DrumRowInfo{ Name="Mid Tom",   Kind=DrumVoiceKind.Kick,  Params=new DrumVoiceParams{ F0=240, F1=130, Decay=0.26, Gain=0.7 } },
        new DrumRowInfo{ Name="Hi Tom",    Kind=DrumVoiceKind.Kick,  Params=new DrumVoiceParams{ F0=320, F1=180, Decay=0.22, Gain=0.7 } },
        new DrumRowInfo{ Name="Crash",     Kind=DrumVoiceKind.Hat,   Params=new DrumVoiceParams{ Decay=0.9, Gain=0.28, HighpassHz=5000 } },
        new DrumRowInfo{ Name="Ride",      Kind=DrumVoiceKind.Hat,   Params=new DrumVoiceParams{ Decay=0.5, Gain=0.22, HighpassHz=6000 } },
        new DrumRowInfo{ Name="Perc 1",    Kind=DrumVoiceKind.Snare, Params=new DrumVoiceParams{ Tone=520, Decay=0.08, Gain=0.4 } },
        new DrumRowInfo{ Name="Perc 2",    Kind=DrumVoiceKind.Snare, Params=new DrumVoiceParams{ Tone=680, Decay=0.06, Gain=0.35 } },
    };

    public const int Count = 12;
    public const int Steps = 16;
}

/// <summary>ドラムキット(音色)ごとの、全行に一律で掛かる補正</summary>
public class KitStyleMod
{
    public double DecayMul = 1.0;
    public double GainMul = 1.0;
}

public static class KitStyles
{
    public static readonly Dictionary<string, KitStyleMod> ByInstrumentId = new()
    {
        ["rock"]   = new KitStyleMod{ DecayMul=1.0, GainMul=1.0 },
        ["techno"] = new KitStyleMod{ DecayMul=1.3, GainMul=1.05 },
        ["hiphop"] = new KitStyleMod{ DecayMul=1.7, GainMul=1.1 },
    };
}
