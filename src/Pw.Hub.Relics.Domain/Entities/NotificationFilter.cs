using Pw.Hub.Relics.Domain.Enums;

namespace Pw.Hub.Relics.Domain.Entities;

/// <summary>
/// Фильтр уведомлений пользователя
/// </summary>
public class NotificationFilter
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    
    // Критерии фильтрации
    public SoulType? SoulType { get; set; }
    public int? SlotTypeId { get; set; }
    public Race? Race { get; set; }
    public int? SoulLevel { get; set; }
    public int? MainAttributeId { get; set; }
    
    /// <summary>
    /// ID требуемых дополнительных характеристик
    /// </summary>
    public List<int> RequiredAdditionalAttributeIds { get; set; } = new();
    
    /// <summary>
    /// Минимальная цена в серебре
    /// </summary>
    public long? MinPrice { get; set; }
    
    /// <summary>
    /// Максимальная цена в серебре
    /// </summary>
    public long? MaxPrice { get; set; }
    
    public int? ServerId { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
