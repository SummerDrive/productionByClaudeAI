namespace StepSeq.Audio.Voices;

/// <summary>
/// トリガーされた1音を表す。NextSample() を呼ぶたびに1サンプル分の波形を返し、
/// 内部時間を1サンプル進める。寿命が尽きたら Finished=true になり、
/// MixingEngine 側でリストから取り除かれる。
/// </summary>
public abstract class Voice
{
    protected readonly int SampleRate;
    protected long ElapsedSamples;
    public bool Finished { get; protected set; }

    protected Voice(int sampleRate)
    {
        SampleRate = sampleRate;
    }

    protected double ElapsedSeconds => (double)ElapsedSamples / SampleRate;

    /// <summary>1サンプル分の波形(-1..1程度)を返し、内部時間を進める</summary>
    public abstract float NextSample();
}
