using MediatR;
using Microsoft.EntityFrameworkCore;
using Pw.Hub.Relics.Domain.Enums;
using Pw.Hub.Relics.Infrastructure.Data;
using Pw.Hub.Relics.Shared.Helpers;

namespace Pw.Hub.Relics.Api.Features.Optimization.CalculateMostProfitableQuest;

public class CalculateMostProfitableQuestHandler : IRequestHandler<CalculateMostProfitableQuestQuery, CalculateMostProfitableQuestResponse>
{
    private readonly RelicsDbContext _dbContext;

    public CalculateMostProfitableQuestHandler(RelicsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CalculateMostProfitableQuestResponse> Handle(
        CalculateMostProfitableQuestQuery request,
        CancellationToken cancellationToken)
    {
        // Получить информацию о сервере
        var server = await _dbContext.ServerDefinitions
            .FirstOrDefaultAsync(s => s.Id == request.ServerId, cancellationToken);

        var serverName = server?.Name ?? "Unknown";

        // Получить минимальные цены по комбинациям SoulType + SoulLevel + Race
        var priceStatsByRace = await _dbContext.RelicListings
            .Include(r => r.RelicDefinition)
            .Where(r => r.IsActive && r.ServerId == request.ServerId)
            .GroupBy(r => new
            {
                r.RelicDefinition.SoulType,
                r.RelicDefinition.SoulLevel,
                r.RelicDefinition.Race
            })
            .Select(g => new
            {
                g.Key.SoulType,
                g.Key.SoulLevel,
                g.Key.Race,
                MinPrice = g.Min(r => r.Price),
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        // Рассчитать среднюю минимальную цену для каждого SoulType + SoulLevel
        var avgMinPriceStats = priceStatsByRace
            .GroupBy(s => new { s.SoulType, s.SoulLevel })
            .Select(g => new LevelStatsData(
                g.Key.SoulType,
                g.Key.SoulLevel,
                (long)g.Average(r => r.MinPrice),
                g.ToDictionary(r => r.Race, r => r.MinPrice),
                g.Sum(r => r.Count)
            ))
            .ToList();

        // Группировать по SoulType
        var groupedStats = avgMinPriceStats
            .GroupBy(s => s.SoulType)
            .ToList();

        var recommendations = new List<QuestRecommendationDto>();
        var levelOneRecommendations = new List<LevelOneRecommendationDto>();

        foreach (var group in groupedStats)
        {
            var soulType = group.Key;
            var soulTypeName = GetSoulTypeName(soulType);
            var levelStats = group.ToDictionary(s => s.SoulLevel);

            // Рекомендации для уровня 1 (бесплатно)
            if (levelStats.TryGetValue(1, out var level1Stats))
            {
                levelOneRecommendations.Add(new LevelOneRecommendationDto
                {
                    Rank = 0, // Будет установлен позже
                    SoulType = (int)soulType,
                    SoulTypeName = soulTypeName,
                    ExpectedReward = level1Stats.AvgMinPrice,
                    ExpectedRewardFormatted = PriceHelper.FormatPrice(level1Stats.AvgMinPrice),
                    AvgMinPriceByRace = level1Stats.MinPriceByRace.ToDictionary(
                        kvp => GetRaceName(kvp.Key),
                        kvp => kvp.Value),
                    ListingsCount = level1Stats.TotalCount
                });
            }

            // Рекомендации для уровней 2-5
            for (int targetLevel = 2; targetLevel <= 5; targetLevel++)
            {
                var prevLevel = targetLevel - 1;

                if (!levelStats.TryGetValue(prevLevel, out var prevLevelStats) ||
                    !levelStats.TryGetValue(targetLevel, out var targetLevelStats))
                {
                    continue; // Нет данных для расчета
                }

                // Затраты = средняя минимальная цена предыдущего уровня
                var questCost = prevLevelStats.AvgMinPrice;

                // Доход = средняя минимальная цена текущего уровня
                var expectedReward = targetLevelStats.AvgMinPrice;

                var profit = expectedReward - questCost;

                if (profit > 0)
                {
                    var priceBreakdown = BuildPriceBreakdown(levelStats);

                    recommendations.Add(new QuestRecommendationDto
                    {
                        Rank = 0, // Будет установлен позже
                        SoulType = (int)soulType,
                        SoulTypeName = soulTypeName,
                        TargetSoulLevel = targetLevel,
                        QuestCost = questCost,
                        QuestCostFormatted = PriceHelper.FormatPrice(questCost),
                        ExpectedReward = expectedReward,
                        ExpectedRewardFormatted = PriceHelper.FormatPrice(expectedReward),
                        ExpectedProfit = profit,
                        ExpectedProfitFormatted = PriceHelper.FormatPrice(profit),
                        ProfitPercent = Math.Round((double)profit / questCost * 100, 1),
                        PriceBreakdown = priceBreakdown
                    });
                }
            }
        }

        // Сортировать по профиту и установить ранги
        recommendations = recommendations
            .OrderByDescending(r => r.ExpectedProfit)
            .Select((r, i) => r with { Rank = i + 1 })
            .ToList();

        levelOneRecommendations = levelOneRecommendations
            .OrderByDescending(r => r.ExpectedReward)
            .Select((r, i) => r with { Rank = i + 1 })
            .ToList();

        return new CalculateMostProfitableQuestResponse
        {
            ServerId = request.ServerId,
            ServerName = serverName,
            CalculatedAt = DateTime.UtcNow,
            Recommendations = recommendations,
            LevelOneRecommendations = levelOneRecommendations
        };
    }

    private Dictionary<int, LevelPriceInfoDto> BuildPriceBreakdown(
        Dictionary<int, LevelStatsData> levelStats)
    {
        var result = new Dictionary<int, LevelPriceInfoDto>();

        foreach (var kvp in levelStats)
        {
            result[kvp.Key] = new LevelPriceInfoDto
            {
                AvgMinPrice = kvp.Value.AvgMinPrice,
                MinPriceByRace = kvp.Value.MinPriceByRace
                    .ToDictionary(r => GetRaceName(r.Key), r => r.Value),
                ListingsCount = kvp.Value.TotalCount
            };
        }

        return result;
    }

    private static string GetSoulTypeName(SoulType soulType) => soulType switch
    {
        SoulType.Peace => "Покоя",
        SoulType.Tianya => "Тяньюя",
        _ => "Unknown"
    };

    private static string GetRaceName(Race race) => race switch
    {
        Race.Human => "human",
        Race.Untamed => "untamed",
        Race.Winged => "winged",
        Race.Tideborn => "tideborn",
        Race.Earthguard => "earthguard",
        Race.Nightshade => "nightshade",
        _ => "unknown"
    };

    private record LevelStatsData(
        SoulType SoulType,
        int SoulLevel,
        long AvgMinPrice,
        Dictionary<Race, long> MinPriceByRace,
        int TotalCount);
}
