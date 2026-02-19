namespace Pw.Hub.Relics.Domain.Entities;

/// <summary>
/// История цен (для материализованного представления)
/// </summary>
public class PriceHistory
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// ID реликвии из справочника
    /// </summary>
    public int RelicDefinitionId { get; set; }
    
    /// <summary>
    /// ID основной характеристики (обязательное поле)
    /// </summary>
    public int MainAttributeId { get; set; }
    
    /// <summary>
    /// ID дополнительных характеристик
    /// </summary>
    public List<int> AdditionalAttributeIds { get; set; } = new();
    
    /// <summary>
    /// Цена в серебре (1 золото = 100 серебра)
    /// </summary>
    public long Price { get; set; }
    
    /// <summary>
    /// ID сервера
    /// </summary>
    public int ServerId { get; set; }
    
    /// <summary>
    /// Временная метка
    /// </summary>
    public DateTime Timestamp { get; set; }
}
