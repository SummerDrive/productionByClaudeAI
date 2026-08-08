using System.Collections.Generic;

namespace StepSeq.Models;

public enum TrackType { Drum, Bass, Keys }

public record Instrument(string Id, string Name);

public static class InstrumentCatalog
{
    public static readonly Dictionary<TrackType, List<Instrument>> ByTrack = new()
    {
        [TrackType.Drum] = new List<Instrument>
        {
            new("rock",   "ロック"),
            new("techno", "テクノ"),
            new("hiphop", "ヒップホップ"),
        },
        [TrackType.Bass] = new List<Instrument>
        {
            new("wood",   "ウッドベース"),
            new("pick",   "エレキベース（ピック弾き）"),
            new("finger", "エレキベース（指弾き）"),
        },
        [TrackType.Keys] = new List<Instrument>
        {
            new("piano",   "ピアノ"),
            new("rhodes",  "ローズ"),
            new("dx",      "DX系エレピ"),
            new("strings", "ストリングス"),
            new("pad",     "パッド系シンセ"),
        },
    };

    public static Instrument Default(TrackType t) => ByTrack[t][0];
}
