using StepSeq.Audio.Dsp;

namespace StepSeq.Audio.Voices;

public class SnareVoice : Voice
{
    private readonly Phase _tone;
    private readonly NoiseGen _noise;
    private readonly BiquadFilter _hp;
    private readonly double _toneFreq, _decay, _gain;
    private readonly double _life;

    public SnareVoice(int sampleRate, double toneFreq, double decay, double gain) : base(sampleRate)
    {
        _tone = new Phase(sampleRate);
        _noise = new NoiseGen();
        _hp = new BiquadFilter(sampleRate);
        _hp.SetHighPass(900, 0.8);
        _toneFreq = toneFreq; _decay = decay; _gain = gain;
        _life = decay + 0.06;
    }

    public override float NextSample()
    {
        if (Finished) return 0;
        double t = ElapsedSeconds;
        if (t >= _life) { Finished = true; return 0; }

        double toneAmp = Ramp.Exp(_gain * 0.6, 0.001, t, _decay * 0.6);
        float toneS = _tone.Next(_toneFreq, WaveShape.Triangle) * (float)toneAmp;

        double noiseAmp = Ramp.Exp(_gain, 0.001, t, _decay);
        float noiseS = _hp.Process(_noise.Next()) * (float)noiseAmp;

        ElapsedSamples++;
        return toneS + noiseS;
    }
}
