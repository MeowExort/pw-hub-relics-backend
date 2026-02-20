namespace Pw.Hub.Relics.Domain.Entities;

/// <summary>
/// Лот реликвии в магазине
/// </summary>
public class RelicListing
{
    public Guid Id { get; set; }
    
    public int RelicDefinitionId { get; set; }
    public RelicDefinition RelicDefinition { get; set; } = null!;
    
    /// <summary>
    /// Опыт при поглощении
    /// </summary>
    public int AbsorbExperience { get; set; }
    
    /// <summary>
    /// Все характеристики (основная + дополнительные)
    /// </summary>
    public List<RelicAttribute> Attributes { get; set; } = new();
    
    /// <summary>
    /// Уровень заточки
    /// </summary>
    public int EnhancementLevel { get; set; }
    
    /// <summary>
    /// ID персонажа продавца
    /// </summary>
    public long SellerCharacterId { get; set; }
    
    /// <summary>
    /// Позиция в магазине продавца
    /// </summary>
    public int ShopPosition { get; set; }
    
    /// <summary>
    /// Цена в серебре (1 золото = 100 серебра)
    /// </summary>
    public long Price { get; set; }
    
    public int ServerId { get; set; }
    public ServerDefinition Server { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime? SoldAt { get; set; }
    
    /// <summary>
    /// SHA256 хеш атрибутов для быстрого поиска
    /// </summary>
    public string? AttributesHash { get; set; }
    
    /// <summary>
    /// Версия строки для оптимистичной блокировки (PostgreSQL xmin)
    /// </summary>
    public uint RowVersion { get; set; }
}
