using StepSeq.Audio.Dsp;

namespace StepSeq.Audio.Voices;

public class HatVoice : Voice
{
    private readonly NoiseGen _noise;
    private readonly BiquadFilter _hp;
    private readonly double _decay, _gain;
    private readonly double _life;

    public HatVoice(int sampleRate, double decay, double gain, double highpassHz) : base(sampleRate)
    {
        _noise = new NoiseGen();
        _hp = new BiquadFilter(sampleRate);
        _hp.SetHighPass(highpassHz, 0.7);
        _decay = decay; _gain = gain;
        _life = decay + 0.03;
    }

    public override float NextSample()
    {
        if (Finished) return 0;
        double t = ElapsedSeconds;
        if (t >= _life) { Finished = true; return 0; }

        double amp = Ramp.Exp(_gain, 0.001, t, _decay);
        float s = _hp.Process(_noise.Next()) * (float)amp;

        ElapsedSamples++;
        return s;
    }
}
