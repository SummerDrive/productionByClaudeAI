using System;

namespace StepSeq.Audio.Dsp;

public enum WaveShape { Sine, Triangle, Sawtooth }

/// <summary>1つの発振器の位相を保持し、サンプルごとに波形値(-1..1)を返す。</summary>
public class Phase
{
    private double _phase;
    private readonly int _sampleRate;

    public Phase(int sampleRate) { _sampleRate = sampleRate; }

    public float Next(double freqHz, WaveShape shape)
    {
        double value = shape switch
        {
            WaveShape.Sine => Math.Sin(_phase),
            WaveShape.Triangle => (2.0 / Math.PI) * Math.Asin(Math.Sin(_phase)),
            WaveShape.Sawtooth => 2.0 * (_phase / (2 * Math.PI) - Math.Floor(_phase / (2 * Math.PI) + 0.5)),
            _ => 0
        };
        _phase += 2 * Math.PI * freqHz / _sampleRate;
        if (_phase > 2 * Math.PI) _phase -= 2 * Math.PI;
        return (float)value;
    }

    /// <summary>FM: キャリア位相をモジュレータの出力ぶんだけ余分に進める</summary>
    public float NextFm(double freqHz, double modOffsetRadians)
    {
        double value = Math.Sin(_phase + modOffsetRadians);
        _phase += 2 * Math.PI * freqHz / _sampleRate;
        if (_phase > 2 * Math.PI) _phase -= 2 * Math.PI;
        return (float)value;
    }
}

public class NoiseGen
{
    private readonly Random _rng = new();
    public float Next() => (float)(_rng.NextDouble() * 2.0 - 1.0);
}
