using MediatR;
using Microsoft.EntityFrameworkCore;
using Pw.Hub.Relics.Infrastructure.Data;
using Pw.Hub.Relics.Shared.Helpers;

namespace Pw.Hub.Relics.Api.Features.Optimization.CalculateCheapestEnhancement;

public class CalculateCheapestEnhancementHandler : IRequestHandler<CalculateCheapestEnhancementCommand, CalculateCheapestEnhancementResponse>
{
    private readonly RelicsDbContext _dbContext;

    public CalculateCheapestEnhancementHandler(RelicsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CalculateCheapestEnhancementResponse> Handle(
        CalculateCheapestEnhancementCommand request, 
        CancellationToken cancellationToken)
    {
        // Получить кривую опыта для расчета требуемого опыта
        var enhancementCurve = await _dbContext.EnhancementCurves
            .Where(e => e.Level <= request.TargetEnhancementLevel)
            .OrderBy(e => e.Level)
            .ToListAsync(cancellationToken);

        var requiredExperience = enhancementCurve.Sum(e => e.RequiredExperience);
        var missingExperience = Math.Max(0, requiredExperience - request.CurrentExperience);

        if (missingExperience == 0)
        {
            return new CalculateCheapestEnhancementResponse
            {
                TargetLevel = request.TargetEnhancementLevel,
                RequiredExperience = requiredExperience,
                CurrentExperience = request.CurrentExperience,
                MissingExperience = 0,
                Recommendations = new List<EnhancementRecommendationDto>(),
                TotalRelicsNeeded = 0,
                TotalCost = 0,
                TotalCostFormatted = PriceHelper.FormatPrice(0),
                AveragePricePerExperience = 0
            };
        }

        // Получить все активные реликвии того же типа души, отсортированные по цене за опыт
        var availableRelics = await _dbContext.RelicListings
            .Include(r => r.RelicDefinition)
            .Where(r => r.IsActive 
                && r.ServerId == request.ServerId 
                && r.AbsorbExperience > 0
                && r.RelicDefinition.SoulType == request.SoulType)
            .OrderBy(r => (double)r.Price / r.AbsorbExperience)
            .Select(r => new RelicCandidate
            {
                Id = r.Id,
                RelicName = r.RelicDefinition.Name,
                AbsorbExperience = r.AbsorbExperience,
                Price = r.Price
            })
            .ToListAsync(cancellationToken);

        // Используем оптимизированный алгоритм для минимизации перебора опыта
        var recommendations = FindOptimalRelicCombination(availableRelics, missingExperience);

        var totalExperience = recommendations.Sum(r => r.AbsorbExperience);
        var totalCost = recommendations.Sum(r => r.Price);

        var averagePricePerExperience = totalExperience > 0
            ? Math.Round((double)totalCost / totalExperience, 2)
            : 0;

        return new CalculateCheapestEnhancementResponse
        {
            TargetLevel = request.TargetEnhancementLevel,
            RequiredExperience = requiredExperience,
            CurrentExperience = request.CurrentExperience,
            MissingExperience = missingExperience,
            Recommendations = recommendations,
            TotalRelicsNeeded = recommendations.Count,
            TotalCost = totalCost,
            TotalCostFormatted = PriceHelper.FormatPrice(totalCost),
            AveragePricePerExperience = averagePricePerExperience
        };
    }

    /// <summary>
    /// Находит оптимальную комбинацию реликвий с минимальной ценой и минимальным перебором опыта.
    /// Алгоритм: жадно набираем реликвии по цене/опыт, но на последнем шаге выбираем
    /// реликвию с минимальным перебором опыта среди тех, что позволяют достичь цели.
    /// </summary>
    private static List<EnhancementRecommendationDto> FindOptimalRelicCombination(
        List<RelicCandidate> availableRelics, 
        int missingExperience)
    {
        var recommendations = new List<EnhancementRecommendationDto>();
        var accumulatedExperience = 0;
        var accumulatedCost = 0L;
        var usedRelicIds = new HashSet<Guid>();

        while (accumulatedExperience < missingExperience && availableRelics.Count > 0)
        {
            var remainingExperience = missingExperience - accumulatedExperience;
            
            // Фильтруем только неиспользованные реликвии
            var unusedRelics = availableRelics.Where(r => !usedRelicIds.Contains(r.Id)).ToList();
            
            if (unusedRelics.Count == 0)
                break;

            RelicCandidate? selectedRelic = null;

            // Ищем реликвии, которые могут закрыть оставшийся опыт
            var finishingRelics = unusedRelics
                .Where(r => r.AbsorbExperience >= remainingExperience)
                .ToList();

            if (finishingRelics.Count > 0)
            {
                // Среди реликвий, которые могут закрыть цель, выбираем по критерию:
                // минимальная цена с учётом того, что лишний опыт "сгорает"
                // Т.е. выбираем реликвию с минимальной ценой среди тех, что дают минимальный перебор
                // или с лучшим соотношением цена/нужный_опыт
                
                // Сначала пробуем найти реликвию с минимальным перебором и приемлемой ценой
                var minOverflow = finishingRelics.Min(r => r.AbsorbExperience - remainingExperience);
                var maxAcceptableOverflow = Math.Max(minOverflow, remainingExperience * 0.5); // Допускаем перебор до 50% от оставшегося
                
                var acceptableFinishingRelics = finishingRelics
                    .Where(r => r.AbsorbExperience - remainingExperience <= maxAcceptableOverflow)
                    .ToList();

                // Из приемлемых выбираем самую дешёвую
                selectedRelic = acceptableFinishingRelics
                    .OrderBy(r => r.Price)
                    .First();
            }
            else
            {
                // Если ни одна реликвия не может закрыть цель, берём с лучшим соотношением цена/опыт
                selectedRelic = unusedRelics
                    .OrderBy(r => (double)r.Price / r.AbsorbExperience)
                    .First();
            }

            usedRelicIds.Add(selectedRelic.Id);
            accumulatedExperience += selectedRelic.AbsorbExperience;
            accumulatedCost += selectedRelic.Price;

            recommendations.Add(new EnhancementRecommendationDto
            {
                RelicListingId = selectedRelic.Id,
                RelicName = selectedRelic.RelicName,
                AbsorbExperience = selectedRelic.AbsorbExperience,
                Price = selectedRelic.Price,
                PricePerExperience = Math.Round((double)selectedRelic.Price / selectedRelic.AbsorbExperience, 2),
                CumulativeExperience = accumulatedExperience,
                CumulativeCost = accumulatedCost
            });
        }

        return recommendations;
    }

    private class RelicCandidate
    {
        public Guid Id { get; init; }
        public required string RelicName { get; init; }
        public int AbsorbExperience { get; init; }
        public long Price { get; init; }
    }
}
