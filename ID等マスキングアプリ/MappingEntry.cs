using System.Text.Json.Serialization;

namespace MaskingTool;

/// <summary>
/// 元の文字列と、変換後の匿名化文字列の対応を1件表す。
/// </summary>
public sealed class MappingEntry
{
    [JsonPropertyName("original")]
    public string Original { get; set; } = string.Empty;

    [JsonPropertyName("masked")]
    public string Masked { get; set; } = string.Empty;

    [JsonPropertyName("assignedChar")]
    public char AssignedChar { get; set; }

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("length")]
    public int Length { get; set; }
}
