using System.Text.Json;

namespace MaskingTool;

/// <summary>
/// マッピング一覧をユーザーのAppDataフォルダにJSONとして保存・読込する。
/// 既定では %APPDATA%\MaskingTool\mapping.json に保存される。
/// </summary>
public sealed class MappingStore
{
    private static readonly char[] CharCycle = ['x', 'y', 'z'];

    private readonly string _filePath;

    public MappingStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MaskingTool",
            "mapping.json");
    }

    public List<MappingEntry> Load()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        var json = File.ReadAllText(_filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<MappingEntry>>(json) ?? [];
    }

    public void Save(List<MappingEntry> entries)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }

    /// <summary>
    /// 入力された複数行の文字列から、未登録のものだけを新規エントリとして追加する。
    /// 割当文字(x/y/z)と番号は「登録済み件数」からの連番で決まる。
    /// </summary>
    public List<MappingEntry> AddEntries(List<MappingEntry> existing, IEnumerable<string> rawLines)
    {
        var result = new List<MappingEntry>(existing);
        var existingOriginals = new HashSet<string>(existing.Select(e => e.Original));

        var index = existing.Count;

        foreach (var raw in rawLines)
        {
            var original = raw.Trim();
            if (original.Length == 0)
            {
                continue;
            }

            if (existingOriginals.Contains(original))
            {
                // 既に登録済みの文字列はスキップ（重複登録しない）
                continue;
            }

            var assignedChar = CharCycle[index % CharCycle.Length];
            var number = (index / CharCycle.Length) + 1;
            var masked = new string(assignedChar, original.Length) + number;

            result.Add(new MappingEntry
            {
                Original = original,
                Masked = masked,
                AssignedChar = assignedChar,
                Number = number,
                Length = original.Length,
            });

            existingOriginals.Add(original);
            index++;
        }

        return result;
    }
}
