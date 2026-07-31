using System.Text;

namespace MaskingTool;

/// <summary>
/// 登録済みマッピングを使い、入力テキスト内の文字列を匿名化文字列に置換する。
/// </summary>
public static class Masker
{
    /// <summary>
    /// input内に含まれる entries の Original を、対応する Masked に置換して返す。
    /// 短い文字列が長い文字列の一部を誤って置換しないよう、Originalが長い順に処理する。
    /// 大文字小文字は区別する（Ordinal比較）。
    /// </summary>
    public static string Mask(string input, IReadOnlyList<MappingEntry> entries)
    {
        if (string.IsNullOrEmpty(input) || entries.Count == 0)
        {
            return input;
        }

        var ordered = entries
            .Where(e => e.Original.Length > 0)
            .OrderByDescending(e => e.Original.Length)
            .ToList();

        var result = new StringBuilder(input);

        foreach (var entry in ordered)
        {
            result.Replace(entry.Original, entry.Masked);
        }

        return result.ToString();
    }
}
