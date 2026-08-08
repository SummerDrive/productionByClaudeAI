using System.Collections.Generic;
using StepSeq.Audio.Dsp;

namespace StepSeq.Audio;

public class BassParams
{
    public WaveShape Wave;
    public double LowpassHz;
    public double Attack;
    public double Decay;
    public double Sustain;
    public double Gain;
    public TransientParams? Transient;
}

public class TransientParams
{
    public double Gain;
    public double HighpassHz;
    public double Duration;
}

public static class BassSounds
{
    public static readonly Dictionary<string, BassParams> ById = new()
    {
        ["wood"] = new BassParams
        {
            Wave = WaveShape.Triangle, LowpassHz = 900, Attack = 0.012, Decay = 0.35, Sustain = 0.3, Gain = 0.62,
            Transient = new TransientParams { Gain = 0.22, HighpassHz = 280, Duration = 0.04 }
        },
        ["pick"] = new BassParams
        {
            Wave = WaveShape.Sawtooth, LowpassHz = 2400, Attack = 0.004, Decay = 0.22, Sustain = 0.25, Gain = 0.4,
            Transient = new TransientParams { Gain = 0.25, HighpassHz = 2000, Duration = 0.03 }
        },
        ["finger"] = new BassParams
        {
            Wave = WaveShape.Sine, LowpassHz = 1300, Attack = 0.007, Decay = 0.3, Sustain = 0.35, Gain = 0.68,
            Transient = new TransientParams { Gain = 0.26, HighpassHz = 400, Duration = 0.035 }
        },
    };
}

public enum KeysVoiceType { Piano, Rhodes, Dx, Strings, Pad }

public class KeysEnvParams
{
    public double Attack;
    public double Decay;
    public double Peak;
    public double Sustain;
}

public static class KeysSounds
{
    public static readonly Dictionary<string, KeysVoiceType> VoiceTypeById = new()
    {
        ["piano"] = KeysVoiceType.Piano,
        ["rhodes"] = KeysVoiceType.Rhodes,
        ["dx"] = KeysVoiceType.Dx,
        ["strings"] = KeysVoiceType.Strings,
        ["pad"] = KeysVoiceType.Pad,
    };

    public static readonly Dictionary<string, KeysEnvParams> EnvById = new()
    {
        ["piano"]   = new KeysEnvParams{ Attack=0.004, Decay=0.35, Peak=0.35, Sustain=0.20 },
        ["rhodes"]  = new KeysEnvParams{ Attack=0.015, Decay=0.40, Peak=0.40, Sustain=0.25 },
        ["dx"]      = new KeysEnvParams{ Attack=0.003, Decay=0.35, Peak=0.35, Sustain=0.15 },
        ["strings"] = new KeysEnvParams{ Attack=0.12,  Decay=0.30, Peak=0.28, Sustain=0.85 },
        ["pad"]     = new KeysEnvParams{ Attack=0.20,  Decay=0.30, Peak=0.25, Sustain=0.90 },
    };

    /// <summary>ペダルOFF時（打鍵の瞬間だけ）: 次のステップにかぶらないよう短く切る</summary>
    public static readonly Dictionary<string, double> ReleaseStaccato = new()
    {
        ["piano"]=0.20, ["rhodes"]=0.10, ["dx"]=0.06, ["strings"]=0.15, ["pad"]=0.20,
    };

    /// <summary>ペダルON→OFFに切り替わった瞬間: ダンパーが下りる自然な余韻</summary>
    public static readonly Dictionary<string, double> ReleasePedalOff = new()
    {
        ["piano"]=0.60, ["rhodes"]=0.40, ["dx"]=0.25, ["strings"]=0.60, ["pad"]=0.90,
    };
}
