using Pw.Hub.Relics.Domain.Enums;

namespace Pw.Hub.Relics.Domain.Entities;

/// <summary>
/// Характеристика реликвии (DTO для JSONB хранения)
/// </summary>
public record RelicAttributeDto(
    int AttributeDefinitionId,
    int Value,
    AttributeCategory Category
);
