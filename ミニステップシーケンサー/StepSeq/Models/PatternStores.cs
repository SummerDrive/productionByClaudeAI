using System;
using System.Collections.Generic;
using System.Linq;

namespace StepSeq.Models;

/// <summary>ドラムグリッド: 小節番号 -> [行(12)][ステップ(16)]</summary>
public class DrumPatternStore
{
    private readonly Dictionary<int, bool[][]> _bars = new();

    public bool[][] GetOrCreate(int bar)
    {
        if (!_bars.TryGetValue(bar, out var arr))
        {
            arr = new bool[DrumRows.Count][];
            for (int r = 0; r < DrumRows.Count; r++) arr[r] = new bool[DrumRows.Steps];
            _bars[bar] = arr;
        }
        return arr;
    }

    public bool[][] Clone(int bar)
    {
        var src = GetOrCreate(bar);
        var copy = new bool[src.Length][];
        for (int i = 0; i < src.Length; i++) copy[i] = (bool[])src[i].Clone();
        return copy;
    }

    public void Set(int bar, bool[][] data) => _bars[bar] = data;
    public bool Has(int bar) => _bars.ContainsKey(bar);
    public void Clear() => _bars.Clear();
    public IReadOnlyDictionary<int, bool[][]> Raw => _bars;
    public void LoadRaw(Dictionary<int, bool[][]> data) { _bars.Clear(); foreach (var kv in data) _bars[kv.Key] = kv.Value; }
}

/// <summary>ベース/キーボード用: 小節番号 -> (MIDIノート番号 -> ステップ(16))</summary>
public class PitchPatternStore
{
    private readonly Dictionary<int, Dictionary<int, bool[]>> _bars = new();

    public Dictionary<int, bool[]> GetOrCreate(int bar)
    {
        if (!_bars.TryGetValue(bar, out var dict))
        {
            dict = new Dictionary<int, bool[]>();
            _bars[bar] = dict;
        }
        return dict;
    }

    public bool[] GetOrCreateNote(int bar, int midi)
    {
        var barDict = GetOrCreate(bar);
        if (!barDict.TryGetValue(midi, out var steps))
        {
            steps = new bool[16];
            barDict[midi] = steps;
        }
        return steps;
    }

    public Dictionary<int, bool[]> Clone(int bar)
    {
        var src = GetOrCreate(bar);
        return src.ToDictionary(kv => kv.Key, kv => (bool[])kv.Value.Clone());
    }

    public void Set(int bar, Dictionary<int, bool[]> data) => _bars[bar] = data;
    public void Clear() => _bars.Clear();
    public IReadOnlyDictionary<int, Dictionary<int, bool[]>> Raw => _bars;
    public void LoadRaw(Dictionary<int, Dictionary<int, bool[]>> data) { _bars.Clear(); foreach (var kv in data) _bars[kv.Key] = kv.Value; }
}

/// <summary>キーボードのサスティンペダル: 小節番号 -> ステップ(16)</summary>
public class SustainStore
{
    private readonly Dictionary<int, bool[]> _bars = new();

    public bool[] GetOrCreate(int bar)
    {
        if (!_bars.TryGetValue(bar, out var arr)) { arr = new bool[16]; _bars[bar] = arr; }
        return arr;
    }

    public bool[] Clone(int bar) => (bool[])GetOrCreate(bar).Clone();
    public void Set(int bar, bool[] data) => _bars[bar] = data;
    public void Clear() => _bars.Clear();
    public IReadOnlyDictionary<int, bool[]> Raw => _bars;
    public void LoadRaw(Dictionary<int, bool[]> data) { _bars.Clear(); foreach (var kv in data) _bars[kv.Key] = kv.Value; }

    /// <summary>そのステップからペダルが連続してONになっているステップ数を数える（0=OFF）</summary>
    public int HoldStepsFrom(int bar, int step)
    {
        var arr = GetOrCreate(bar);
        if (!arr[step]) return 0;
        int n = 0;
        for (int s = step; s < arr.Length; s++)
        {
            if (arr[s]) n++; else break;
        }
        return n;
    }
}
