using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace StepSeq.Audio;

public static class OfflineRenderer
{
    /// <summary>
    /// 指定した小節の並びを1回だけレンダリングしてWAVファイルとして保存する。
    /// barSequence の例: 全体書き出しなら {1,2,3,4}、ループ範囲のみなら {2,3}。
    /// </summary>
    public static void RenderToWav(string path, SequencerState state, IReadOnlyList<int> barSequence, TrackFilter filter)
    {
        if (barSequence.Count == 0) throw new ArgumentException("書き出す小節がありません");

        const int sampleRate = 44100;
        var engine = new SequencerEngine(state, sampleRate, barSequence) { Filter = filter };
        engine.Start();

        double totalSeconds = barSequence.Count * 16 * state.StepSeconds + 2.5; // 余韻ぶんの余白
        long totalFrames = (long)(totalSeconds * sampleRate);

        using var writer = new WaveFileWriter(path, WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2));
        float[] buffer = new float[4096];
        long framesWritten = 0;

        while (framesWritten < totalFrames)
        {
            int framesToRequest = (int)Math.Min(buffer.Length / 2, totalFrames - framesWritten);
            int floatsRequested = framesToRequest * 2;
            int floatsRead = engine.Read(buffer, 0, floatsRequested);
            if (floatsRead <= 0) break;

            writer.WriteSamples(buffer, 0, floatsRead);
            framesWritten += floatsRead / 2;

            if (engine.IsFinished) break;
        }
    }
}
