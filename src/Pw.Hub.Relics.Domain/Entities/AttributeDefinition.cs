namespace Pw.Hub.Relics.Domain.Entities;

/// <summary>
/// Справочник характеристик реликвий
/// </summary>
public class AttributeDefinition
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
