using System;

namespace StepSeq.Audio.Dsp;

public enum BiquadType { LowPass, HighPass }

/// <summary>Robert Bristow-Johnson の Audio EQ Cookbook に基づく単純な2次フィルタ。</summary>
public class BiquadFilter
{
    private double _b0, _b1, _b2, _a1, _a2;
    private double _x1, _x2, _y1, _y2;
    private readonly int _sampleRate;

    public BiquadFilter(int sampleRate)
    {
        _sampleRate = sampleRate;
        SetLowPass(4000, 0.707);
    }

    public void SetLowPass(double freqHz, double q) => Configure(BiquadType.LowPass, freqHz, q);
    public void SetHighPass(double freqHz, double q) => Configure(BiquadType.HighPass, freqHz, q);

    public void Configure(BiquadType type, double freqHz, double q)
    {
        freqHz = Math.Clamp(freqHz, 20, _sampleRate * 0.45);
        double w0 = 2 * Math.PI * freqHz / _sampleRate;
        double alpha = Math.Sin(w0) / (2 * q);
        double cosw0 = Math.Cos(w0);

        double b0, b1, b2, a0, a1, a2;
        if (type == BiquadType.LowPass)
        {
            b0 = (1 - cosw0) / 2;
            b1 = 1 - cosw0;
            b2 = (1 - cosw0) / 2;
        }
        else
        {
            b0 = (1 + cosw0) / 2;
            b1 = -(1 + cosw0);
            b2 = (1 + cosw0) / 2;
        }
        a0 = 1 + alpha;
        a1 = -2 * cosw0;
        a2 = 1 - alpha;

        _b0 = b0 / a0; _b1 = b1 / a0; _b2 = b2 / a0;
        _a1 = a1 / a0; _a2 = a2 / a0;
    }

    public float Process(float x)
    {
        double y = _b0 * x + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;
        _x2 = _x1; _x1 = x;
        _y2 = _y1; _y1 = y;
        return (float)y;
    }
}
