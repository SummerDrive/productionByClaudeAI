using StepSeq.Audio.Dsp;

namespace StepSeq.Audio.Voices;

public class KickVoice : Voice
{
    private readonly Phase _osc;
    private readonly double _f0, _f1, _decay, _gain;
    private readonly double _pitchTime;
    private readonly double _life;

    public KickVoice(int sampleRate, double f0, double f1, double decay, double gain) : base(sampleRate)
    {
        _osc = new Phase(sampleRate);
        _f0 = f0; _f1 = f1 <= 0 ? 1 : f1; _decay = decay; _gain = gain;
        _pitchTime = decay * 0.7;
        _life = decay + 0.05;
    }

    public override float NextSample()
    {
        if (Finished) return 0;
        double t = ElapsedSeconds;
        if (t >= _life) { Finished = true; return 0; }

        double freq = Ramp.Exp(_f0, _f1, t, _pitchTime);
        double amp = Ramp.Exp(_gain, 0.001, t, _decay);
        float s = _osc.Next(freq, WaveShape.Sine) * (float)amp;

        ElapsedSamples++;
        return s;
    }
}
