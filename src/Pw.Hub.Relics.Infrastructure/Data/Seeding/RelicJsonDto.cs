using System.Text.Json.Serialization;

namespace Pw.Hub.Relics.Infrastructure.Data.Seeding;

/// <summary>
/// DTO для десериализации реликвии из JSON файла
/// </summary>
public class RelicJsonDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("file_icon")]
    public string FileIcon { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public int Category { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("rarity")]
    public int Rarity { get; set; }

    [JsonPropertyName("race")]
    public int Race { get; set; }

    [JsonPropertyName("profession_group")]
    public List<ProfessionGroupDto>? ProfessionGroups { get; set; }
}

public class ProfessionGroupDto
{
    [JsonPropertyName("group_id")]
    public int GroupId { get; set; }

    [JsonPropertyName("profession_group_ext")]
    public List<ProfessionGroupExtDto>? Extensions { get; set; }
}

public class ProfessionGroupExtDto
{
    [JsonPropertyName("ext_ID")]
    public int ExtId { get; set; }

    [JsonPropertyName("prop_value")]
    public int PropValue { get; set; }
}
