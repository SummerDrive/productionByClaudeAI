using System;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace StepSeq.Audio;

/// <summary>アプリ全体で1つだけ使うライブ再生用プレイヤー。</summary>
public class AudioPlayer : IDisposable
{
    public static readonly AudioPlayer Instance = new();

    private WasapiOut? _output;
    public SequencerEngine? Engine { get; private set; }
    public bool IsPlaying { get; private set; }

    private AudioPlayer() { }

    public void Start(SequencerState state, int? startBar = null)
    {
        Stop();

        Engine = new SequencerEngine(state, 44100);
        Engine.PlaybackReachedEnd += () => IsPlaying = false;
        Engine.Start(startBar);

        _output = new WasapiOut(AudioClientShareMode.Shared, 60);
        _output.Init(Engine.ToWaveProvider());
        _output.Play();
        IsPlaying = true;
    }

    public void Stop()
    {
        try { _output?.Stop(); } catch { /* デバイスが既に閉じられている場合など */ }
        _output?.Dispose();
        _output = null;
        Engine = null;
        IsPlaying = false;
    }

    public void Dispose() => Stop();
}
