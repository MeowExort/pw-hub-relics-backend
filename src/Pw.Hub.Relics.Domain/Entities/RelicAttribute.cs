using Pw.Hub.Relics.Domain.Enums;

namespace Pw.Hub.Relics.Domain.Entities;

/// <summary>
/// Характеристика реликвии
/// </summary>
public class RelicAttribute
{
    public Guid Id { get; set; }
    public Guid RelicListingId { get; set; }
    
    public int AttributeDefinitionId { get; set; }
    public AttributeDefinition AttributeDefinition { get; set; } = null!;
    
    public int Value { get; set; }
    
    /// <summary>
    /// Категория: основная или дополнительная
    /// </summary>
    public AttributeCategory Category { get; set; }
}
