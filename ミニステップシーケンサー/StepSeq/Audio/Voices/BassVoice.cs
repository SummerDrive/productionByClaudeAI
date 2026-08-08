using StepSeq.Audio.Dsp;

namespace StepSeq.Audio.Voices;

public class BassVoice : Voice
{
    private readonly Phase _osc;
    private readonly BiquadFilter _lp;
    private readonly AmpEnvelope _env;
    private readonly double _freq;
    private readonly WaveShape _wave;
    private readonly double _life;

    private readonly NoiseGen? _noise;
    private readonly BiquadFilter? _transHp;
    private readonly double _transGain, _transDur;

    public BassVoice(int sampleRate, double freq, BassParams p) : base(sampleRate)
    {
        _osc = new Phase(sampleRate);
        _lp = new BiquadFilter(sampleRate);
        _lp.SetLowPass(p.LowpassHz, 0.707);
        _freq = freq;
        _wave = p.Wave;

        _env = new AmpEnvelope
        {
            Attack = p.Attack, Decay = p.Decay, SustainLevel = p.Sustain, Hold = 0, Release = 0.15, Peak = p.Gain
        };
        _life = p.Decay + 0.3;

        if (p.Transient != null)
        {
            _noise = new NoiseGen();
            _transHp = new BiquadFilter(sampleRate);
            _transHp.SetHighPass(p.Transient.HighpassHz, 0.7);
            _transGain = p.Transient.Gain;
            _transDur = p.Transient.Duration;
        }
    }

    public override float NextSample()
    {
        if (Finished) return 0;
        double t = ElapsedSeconds;
        if (t >= _life) { Finished = true; return 0; }

        float osc = _lp.Process(_osc.Next(_freq, _wave));
        float s = osc * (float)_env.ValueAt(t);

        if (_noise != null && t < _transDur)
        {
            double amp = Ramp.Exp(_transGain, 0.001, t, _transDur);
            s += _transHp!.Process(_noise.Next()) * (float)amp;
        }

        ElapsedSamples++;
        return s;
    }
}
