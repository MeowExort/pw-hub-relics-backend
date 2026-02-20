using MediatR;
using Pw.Hub.Relics.Domain.Enums;

namespace Pw.Hub.Relics.Api.Features.Optimization.CalculateCheapestEnhancement;

/// <summary>
/// Команда для расчета самого дешевого способа заточки
/// </summary>
public record CalculateCheapestEnhancementCommand : IRequest<CalculateCheapestEnhancementResponse>
{
    /// <summary>
    /// Целевой уровень заточки
    /// </summary>
    public int TargetEnhancementLevel { get; init; }
    
    /// <summary>
    /// Текущий опыт
    /// </summary>
    public int CurrentExperience { get; init; }
    
    /// <summary>
    /// ID сервера
    /// </summary>
    public int ServerId { get; init; }
    
    /// <summary>
    /// Тип души (Покоя или Тяньюя) - реликвии разных типов нельзя поглощать друг в друга
    /// </summary>
    public SoulType SoulType { get; init; }
}

/// <summary>
/// Ответ с рекомендациями по заточке
/// </summary>
public record CalculateCheapestEnhancementResponse
{
    public int TargetLevel { get; init; }
    public int RequiredExperience { get; init; }
    public int CurrentExperience { get; init; }
    public int MissingExperience { get; init; }
    public required List<EnhancementRecommendationDto> Recommendations { get; init; }
    public int TotalRelicsNeeded { get; init; }
    public long TotalCost { get; init; }
    public required string TotalCostFormatted { get; init; }
    public double AveragePricePerExperience { get; init; }
}

/// <summary>
/// Рекомендация по покупке реликвии для заточки
/// </summary>
public record EnhancementRecommendationDto
{
    public Guid RelicListingId { get; init; }
    public required string RelicName { get; init; }
    public int AbsorbExperience { get; init; }
    public long Price { get; init; }
    public double PricePerExperience { get; init; }
    public int CumulativeExperience { get; init; }
    public long CumulativeCost { get; init; }
}
