using System;

namespace StepSeq.Audio.Dsp;

public static class Ramp
{
    /// <summary>t(0..duration)に応じて start から end へ指数的に変化する値を返す(t>=durationではend)</summary>
    public static double Exp(double start, double end, double t, double duration)
    {
        if (duration <= 0) return end;
        double ratio = Math.Clamp(t / duration, 0, 1);
        if (start <= 0) start = 0.0001;
        if (end <= 0) end = 0.0001;
        return start * Math.Pow(end / start, ratio);
    }

    public static double Linear(double start, double end, double t, double duration)
    {
        if (duration <= 0) return end;
        double ratio = Math.Clamp(t / duration, 0, 1);
        return start + (end - start) * ratio;
    }
}
