using MediatR;

namespace Pw.Hub.Relics.Api.Features.Optimization.CalculateMostProfitableQuest;

/// <summary>
/// Запрос на расчет самого выгодного квеста на реликвии
/// </summary>
public record CalculateMostProfitableQuestQuery(int ServerId) : IRequest<CalculateMostProfitableQuestResponse>;

/// <summary>
/// Ответ с рекомендациями по квестам
/// </summary>
public record CalculateMostProfitableQuestResponse
{
    public int ServerId { get; init; }
    public required string ServerName { get; init; }
    public DateTime CalculatedAt { get; init; }
    public required List<QuestRecommendationDto> Recommendations { get; init; }
    public required List<LevelOneRecommendationDto> LevelOneRecommendations { get; init; }
}

/// <summary>
/// Рекомендация по квесту (уровни 2-5)
/// </summary>
public record QuestRecommendationDto
{
    public int Rank { get; init; }
    public int SoulType { get; init; }
    public required string SoulTypeName { get; init; }
    public int TargetSoulLevel { get; init; }
    public long QuestCost { get; init; }
    public required string QuestCostFormatted { get; init; }
    public long ExpectedReward { get; init; }
    public required string ExpectedRewardFormatted { get; init; }
    public long ExpectedProfit { get; init; }
    public required string ExpectedProfitFormatted { get; init; }
    public double ProfitPercent { get; init; }
    public required Dictionary<int, LevelPriceInfoDto> PriceBreakdown { get; init; }
}

/// <summary>
/// Рекомендация для квеста уровня 1 (бесплатно)
/// </summary>
public record LevelOneRecommendationDto
{
    public int Rank { get; init; }
    public int SoulType { get; init; }
    public required string SoulTypeName { get; init; }
    public long ExpectedReward { get; init; }
    public required string ExpectedRewardFormatted { get; init; }
    public required Dictionary<string, long> AvgMinPriceByRace { get; init; }
    public int ListingsCount { get; init; }
}

/// <summary>
/// Информация о ценах для уровня
/// </summary>
public record LevelPriceInfoDto
{
    public long AvgMinPrice { get; init; }
    public required Dictionary<string, long> MinPriceByRace { get; init; }
    public int ListingsCount { get; init; }
}
