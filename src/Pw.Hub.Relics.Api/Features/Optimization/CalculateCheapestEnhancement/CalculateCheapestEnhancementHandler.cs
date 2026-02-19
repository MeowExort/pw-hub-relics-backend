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

        // Получить все активные реликвии, отсортированные по цене за опыт
        var availableRelics = await _dbContext.RelicListings
            .Include(r => r.RelicDefinition)
            .Where(r => r.IsActive && r.ServerId == request.ServerId && r.AbsorbExperience > 0)
            .OrderBy(r => (double)r.Price / r.AbsorbExperience)
            .Select(r => new
            {
                r.Id,
                RelicName = r.RelicDefinition.Name,
                r.AbsorbExperience,
                r.Price
            })
            .ToListAsync(cancellationToken);

        var recommendations = new List<EnhancementRecommendationDto>();
        var accumulatedExperience = 0;
        var accumulatedCost = 0L;

        foreach (var relic in availableRelics)
        {
            if (accumulatedExperience >= missingExperience)
                break;

            accumulatedExperience += relic.AbsorbExperience;
            accumulatedCost += relic.Price;

            recommendations.Add(new EnhancementRecommendationDto
            {
                RelicListingId = relic.Id,
                RelicName = relic.RelicName,
                AbsorbExperience = relic.AbsorbExperience,
                Price = relic.Price,
                PricePerExperience = Math.Round((double)relic.Price / relic.AbsorbExperience, 2),
                CumulativeExperience = accumulatedExperience,
                CumulativeCost = accumulatedCost
            });
        }

        var averagePricePerExperience = accumulatedExperience > 0
            ? Math.Round((double)accumulatedCost / accumulatedExperience, 2)
            : 0;

        return new CalculateCheapestEnhancementResponse
        {
            TargetLevel = request.TargetEnhancementLevel,
            RequiredExperience = requiredExperience,
            CurrentExperience = request.CurrentExperience,
            MissingExperience = missingExperience,
            Recommendations = recommendations,
            TotalRelicsNeeded = recommendations.Count,
            TotalCost = accumulatedCost,
            TotalCostFormatted = PriceHelper.FormatPrice(accumulatedCost),
            AveragePricePerExperience = averagePricePerExperience
        };
    }
}
