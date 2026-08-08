using System;
using System.Collections.Generic;
using NAudio.Wave;
using StepSeq.Audio.Voices;
using StepSeq.Models;

namespace StepSeq.Audio;

/// <summary>
/// NAudioのISampleProviderとして、Read()が呼ばれるたびにサンプル単位で
/// ステップを進行させ、鳴らすべきボイスをその場でトリガーしてミックスする。
/// ・explicitBarSequence が null の場合: ライブ再生モード。state.LoopOn/LoopStart/LoopEnd/Bars に従って
///   無限にループ（またはBarsまで一度再生して自然停止）する。
/// ・explicitBarSequence を渡した場合: オフライン書き出しモード。指定した小節の並びを1回だけ再生する。
/// </summary>
public class SequencerEngine : ISampleProvider
{
    private readonly SequencerState _state;
    private readonly int _sampleRate;
    public WaveFormat WaveFormat { get; }

    public TrackFilter Filter = TrackFilter.All;

    private readonly List<Voice> _activeVoices = new();

    private int _currentStep;
    private int _currentBar;
    private long _samplesUntilNextStep;
    private bool _sequenceFinished;

    private readonly IReadOnlyList<int>? _barSequence;
    private int _barSeqIndex;

    public int CurrentBar => _currentBar;
    public int CurrentStep => _currentStep;
    public bool IsFinished => _sequenceFinished && _activeVoices.Count == 0;

    /// <summary>ループなしのライブ再生が最後まで再生し終えたときに一度だけ呼ばれる（UIスレッドではないので注意）</summary>
    public event Action? PlaybackReachedEnd;

    public SequencerEngine(SequencerState state, int sampleRate, IReadOnlyList<int>? explicitBarSequence = null)
    {
        _state = state;
        _sampleRate = sampleRate;
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2);
        _barSequence = explicitBarSequence;
    }

    public void Start(int? startBar = null)
    {
        _activeVoices.Clear();
        _currentStep = 0;
        _sequenceFinished = false;

        if (_barSequence != null && _barSequence.Count > 0)
        {
            _barSeqIndex = 0;
            _currentBar = _barSequence[0];
        }
        else
        {
            _currentBar = startBar ?? (_state.LoopOn ? _state.LoopStart : 1);
        }

        _samplesUntilNextStep = 0; // 最初のサンプルで即トリガー
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int channels = WaveFormat.Channels;
        int frames = count / channels;

        for (int i = 0; i < frames; i++)
        {
            if (!_sequenceFinished && _samplesUntilNextStep <= 0)
            {
                TriggerStep(_currentBar, _currentStep);
                AdvanceStep();
            }
            else
            {
                _samplesUntilNextStep--;
            }

            float sum = 0f;
            for (int v = _activeVoices.Count - 1; v >= 0; v--)
            {
                var voice = _activeVoices[v];
                sum += voice.NextSample();
                if (voice.Finished) _activeVoices.RemoveAt(v);
            }

            sum *= (float)_state.MasterVolume;
            sum = (float)Math.Tanh(sum); // ソフトクリップで過大入力による割れを防ぐ

            int idx = offset + i * channels;
            for (int c = 0; c < channels; c++) buffer[idx + c] = sum;

            if (_sequenceFinished && _activeVoices.Count == 0)
            {
                // 残りは無音のまま埋める
                for (int j = i + 1; j < frames; j++)
                {
                    int idx2 = offset + j * channels;
                    for (int c = 0; c < channels; c++) buffer[idx2 + c] = 0f;
                }
                break;
            }
        }

        return frames * channels;
    }

    private void AdvanceStep()
    {
        double stepSeconds = _state.StepSeconds;
        double stepSamples = stepSeconds * _sampleRate;
        double swingOffset = (_state.SwingPercent / 100.0) * 0.5 * stepSamples;

        // 偶数ステップ→奇数ステップへの間隔を伸ばし、奇数→偶数を縮めることで
        // 「裏拍が少し遅れる」スウィングを、1小節あたりのトータル長を変えずに再現する。
        double interval = (_currentStep % 2 == 0) ? stepSamples + swingOffset : stepSamples - swingOffset;
        _samplesUntilNextStep = (long)Math.Max(1, interval);

        _currentStep++;
        if (_currentStep >= 16)
        {
            _currentStep = 0;

            if (_barSequence != null)
            {
                _barSeqIndex++;
                if (_barSeqIndex >= _barSequence.Count)
                {
                    _sequenceFinished = true;
                }
                else
                {
                    _currentBar = _barSequence[_barSeqIndex];
                }
            }
            else if (_state.LoopOn)
            {
                _currentBar = (_currentBar >= _state.LoopEnd) ? _state.LoopStart : _currentBar + 1;
            }
            else
            {
                _currentBar++;
                if (_currentBar > _state.Bars)
                {
                    _sequenceFinished = true;
                    PlaybackReachedEnd?.Invoke();
                }
            }
        }
    }

    private void TriggerStep(int bar, int step)
    {
        if (Filter.Drum) TriggerDrum(bar, step);
        if (Filter.Bass) TriggerBass(bar, step);
        if (Filter.Keys) TriggerKeys(bar, step);
    }

    private void TriggerDrum(int bar, int step)
    {
        if (!_state.Drum.Has(bar)) return;
        var rows = _state.Drum.GetOrCreate(bar);
        string kitId = _state.ActiveInstrument[TrackType.Drum];
        var mod = KitStyles.ByInstrumentId.TryGetValue(kitId, out var m) ? m : new KitStyleMod();
        double trackVol = _state.TrackVolume[TrackType.Drum];

        for (int r = 0; r < DrumRows.Count; r++)
        {
            if (!rows[r][step]) continue;
            var info = DrumRows.All[r];
            var p = info.Params.Clone();
            p.Decay *= mod.DecayMul;
            p.Gain *= mod.GainMul * trackVol;

            Voice voice = info.Kind switch
            {
                DrumVoiceKind.Kick => new KickVoice(_sampleRate, p.F0, p.F1, p.Decay, p.Gain),
                DrumVoiceKind.Snare => new SnareVoice(_sampleRate, p.Tone, p.Decay, p.Gain),
                DrumVoiceKind.Hat => new HatVoice(_sampleRate, p.Decay, p.Gain, p.HighpassHz),
                _ => throw new InvalidOperationException()
            };
            _activeVoices.Add(voice);
        }
    }

    private void TriggerBass(int bar, int step)
    {
        var barDict = _state.Bass.GetOrCreate(bar);
        if (barDict.Count == 0) return;
        string instId = _state.ActiveInstrument[TrackType.Bass];
        if (!BassSounds.ById.TryGetValue(instId, out var baseParams)) return;
        double trackVol = _state.TrackVolume[TrackType.Bass];

        foreach (var kv in barDict)
        {
            int midi = kv.Key;
            bool[] steps = kv.Value;
            if (step >= steps.Length || !steps[step]) continue;

            var p = new BassParams
            {
                Wave = baseParams.Wave, LowpassHz = baseParams.LowpassHz, Attack = baseParams.Attack,
                Decay = baseParams.Decay, Sustain = baseParams.Sustain, Gain = baseParams.Gain * trackVol,
                Transient = baseParams.Transient
            };
            double freq = FreqFromMidi(midi);
            _activeVoices.Add(new BassVoice(_sampleRate, freq, p));
        }
    }

    private void TriggerKeys(int bar, int step)
    {
        var barDict = _state.Keys.GetOrCreate(bar);
        if (barDict.Count == 0) return;
        string instId = _state.ActiveInstrument[TrackType.Keys];
        if (!KeysSounds.VoiceTypeById.TryGetValue(instId, out var voiceType)) return;
        if (!KeysSounds.EnvById.TryGetValue(instId, out var baseEnv)) return;
        double trackVol = _state.TrackVolume[TrackType.Keys];

        int holdSteps = _state.Sustain.HoldStepsFrom(bar, step);
        bool pedalOn = holdSteps > 0;
        double stepSeconds = _state.StepSeconds;

        double hold, release;
        if (pedalOn)
        {
            hold = Math.Max(0, holdSteps * stepSeconds - baseEnv.Attack - baseEnv.Decay);
            release = KeysSounds.ReleasePedalOff.GetValueOrDefault(instId, 0.3);
        }
        else
        {
            hold = Math.Max(0, stepSeconds * 0.7 - baseEnv.Attack - baseEnv.Decay);
            release = KeysSounds.ReleaseStaccato.GetValueOrDefault(instId, 0.1);
        }

        var env = new KeysEnvParams
        {
            Attack = baseEnv.Attack, Decay = baseEnv.Decay, Sustain = baseEnv.Sustain,
            Peak = baseEnv.Peak * trackVol
        };

        foreach (var kv in barDict)
        {
            int midi = kv.Key;
            bool[] steps = kv.Value;
            if (step >= steps.Length || !steps[step]) continue;

            double freq = FreqFromMidi(midi);
            _activeVoices.Add(new KeysVoice(_sampleRate, freq, voiceType, env, hold, release));
        }
    }

    public static double FreqFromMidi(int midi) => 440.0 * Math.Pow(2, (midi - 69) / 12.0);
}
