using System.Collections.Generic;

namespace StepSeq.Models;

public readonly record struct NoteRowInfo(int Midi, string Name, bool Black, bool Root);

public static class NoteRange
{
    private static readonly string[] Names = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    public static string NameOf(int midi)
    {
        int octave = midi / 12 - 1;
        return Names[((midi % 12) + 12) % 12] + octave;
    }

    /// <summary>high から low へ降順（画面上で高い音が上に来るように）で行リストを作る</summary>
    public static List<NoteRowInfo> Build(int low, int high)
    {
        var list = new List<NoteRowInfo>();
        for (int m = high; m >= low; m--)
        {
            string n = Names[((m % 12) + 12) % 12];
            list.Add(new NoteRowInfo(m, NameOf(m), n.Contains('#'), n == "C"));
        }
        return list;
    }
}
