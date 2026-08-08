using StepSeq.Audio.Dsp;

namespace StepSeq.Audio.Voices;

public class KeysVoice : Voice
{
    private readonly KeysVoiceType _type;
    private readonly double _freq;
    private readonly AmpEnvelope _env;
    private readonly double _life = 0;

    // Piano: 倍音3本
    private readonly Phase[]? _pianoOsc = null;
    private static readonly double[] PianoHarmonics = { 1, 2, 3 };
    private static readonly double[] PianoGains = { 1.0, 0.18, 0.09 };

    // Rhodes/DX: キャリア+モジュレータ によるFM
    private readonly Phase? _fmCarrier = null;
    private readonly Phase? _fmMod = null;
    private readonly double _fmModRatio = 0;      // モジュレータ周波数 = freq * ratio
    private readonly double _fmIndexStart = 0;    // モジュレーション量(Hz)の開始値
    private readonly double _fmAttackTime = 0;    // モジュレーションが1Hzまで落ち着く時間(音色固有・音の長さに依存しない)

    // Strings/Pad: デチューンした複数オシレータ + ローパス
    private readonly Phase[]? _detuneOsc = null;
    private readonly WaveShape[]? _detuneWave = null;
    private static readonly double[] StringsCents = { 0, -6, 6 };
    private static readonly double[] PadCents = { 0, -8, 7 };
    private readonly BiquadFilter? _lp = null;
    private readonly bool _isPad = false;
    private readonly double _padSweepTime = 0;
    private int _filterUpdateCounter;

    public KeysVoice(int sampleRate, double freq, KeysVoiceType type, KeysEnvParams envParams, double hold, double release)
        : base(sampleRate)
    {
        _type = type;
        _freq = freq;
        _env = new AmpEnvelope
        {
            Attack = envParams.Attack, Decay = envParams.Decay, SustainLevel = envParams.Sustain,
            Hold = hold, Release = release, Peak = envParams.Peak
        };

        switch (type)
        {
            case KeysVoiceType.Piano:
                _pianoOsc = new[] { new Phase(sampleRate), new Phase(sampleRate), new Phase(sampleRate) };
                _life = _env.TotalSeconds + 0.1;
                break;

            case KeysVoiceType.Rhodes:
                _fmCarrier = new Phase(sampleRate);
                _fmMod = new Phase(sampleRate);
                _fmModRatio = 2.0;
                _fmIndexStart = freq * 0.6;
                _fmAttackTime = 0.4;
                _life = _env.TotalSeconds + 0.5;
                break;

            case KeysVoiceType.Dx:
                _fmCarrier = new Phase(sampleRate);
                _fmMod = new Phase(sampleRate);
                _fmModRatio = 14.0;
                _fmIndexStart = freq * 3.5;
                _fmAttackTime = 0.25;
                _life = _env.TotalSeconds + 0.5;
                break;

            case KeysVoiceType.Strings:
                _detuneOsc = new[] { new Phase(sampleRate), new Phase(sampleRate), new Phase(sampleRate) };
                _detuneWave = new[] { WaveShape.Sawtooth, WaveShape.Sawtooth, WaveShape.Sawtooth };
                _lp = new BiquadFilter(sampleRate);
                _lp.SetLowPass(2200, 0.707);
                _isPad = false;
                _life = _env.TotalSeconds + 0.6;
                break;

            case KeysVoiceType.Pad:
                _detuneOsc = new[] { new Phase(sampleRate), new Phase(sampleRate), new Phase(sampleRate) };
                _detuneWave = new[] { WaveShape.Sawtooth, WaveShape.Triangle, WaveShape.Triangle };
                _lp = new BiquadFilter(sampleRate);
                _isPad = true;
                _padSweepTime = envParams.Attack + envParams.Decay + hold;
                _life = _env.TotalSeconds + 1.0;
                break;
        }
    }

    public override float NextSample()
    {
        if (Finished) return 0;
        double t = ElapsedSeconds;
        if (t >= _life) { Finished = true; return 0; }

        float raw = _type switch
        {
            KeysVoiceType.Piano => SamplePiano(),
            KeysVoiceType.Rhodes => SampleFm(),
            KeysVoiceType.Dx => SampleFm(),
            KeysVoiceType.Strings => SampleDetune(t),
            KeysVoiceType.Pad => SampleDetune(t),
            _ => 0f
        };

        float s = raw * (float)_env.ValueAt(t);
        ElapsedSamples++;
        return s;
    }

    private float SamplePiano()
    {
        float sum = 0;
        for (int i = 0; i < _pianoOsc!.Length; i++)
            sum += _pianoOsc[i].Next(_freq * PianoHarmonics[i], WaveShape.Triangle) * (float)PianoGains[i];
        return sum;
    }

    private float SampleFm()
    {
        double t = ElapsedSeconds;
        double modIndexHz = Ramp.Exp(_fmIndexStart, 1.0, t, _fmAttackTime);
        float modSample = _fmMod!.Next(_freq * _fmModRatio, WaveShape.Sine);
        double instFreq = _freq + modSample * modIndexHz;
        return _fmCarrier!.Next(instFreq, WaveShape.Sine);
    }

    private float SampleDetune(double t)
    {
        var cents = _isPad ? PadCents : StringsCents;
        float sum = 0;
        for (int i = 0; i < _detuneOsc!.Length; i++)
        {
            double detFreq = _freq * System.Math.Pow(2, cents[i] / 1200.0);
            sum += _detuneOsc[i].Next(detFreq, _detuneWave![i]);
        }

        if (_isPad)
        {
            // 32サンプルおきにローパスのカットオフを更新（毎サンプルの三角関数計算を避けて軽くする）
            if (_filterUpdateCounter++ % 32 == 0)
            {
                double cutoff = Ramp.Linear(400, 1600, t, System.Math.Max(0.05, _padSweepTime));
                _lp!.SetLowPass(cutoff, 0.707);
            }
        }
        return _lp!.Process(sum);
    }
}
