using Pw.Hub.Relics.Domain.Enums;

namespace Pw.Hub.Relics.Domain.Entities;

/// <summary>
/// Справочник реликвий
/// </summary>
public class RelicDefinition
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Уровень души (1-5)
    /// </summary>
    public int SoulLevel { get; set; }
    
    /// <summary>
    /// Тип души (Покоя или Тяньюя)
    /// </summary>
    public SoulType SoulType { get; set; }
    
    public int SlotTypeId { get; set; }
    public SlotType SlotType { get; set; } = null!;
    
    public Race Race { get; set; }
    
    /// <summary>
    /// URI иконки реликвии
    /// </summary>
    public string? IconUri { get; set; }

    /// <summary>
    /// Сопоставление ID аддона и его максимального значения (из profession_group_ext)
    /// </summary>
    public Dictionary<int, int>? MainAttributeScaling { get; set; }
}
