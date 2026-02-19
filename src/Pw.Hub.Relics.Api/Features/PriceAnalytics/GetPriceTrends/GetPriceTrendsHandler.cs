using MediatR;
using Microsoft.EntityFrameworkCore;
using Pw.Hub.Relics.Domain.Enums;
using Pw.Hub.Relics.Infrastructure.Data;

namespace Pw.Hub.Relics.Api.Features.PriceAnalytics.GetPriceTrends;

public class GetPriceTrendsHandler : IRequestHandler<GetPriceTrendsQuery, GetPriceTrendsResponse>
{
    private readonly RelicsDbContext _dbContext;

    public GetPriceTrendsHandler(RelicsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetPriceTrendsResponse> Handle(GetPriceTrendsQuery request, CancellationToken cancellationToken)
    {
        // Валидация периода (максимум 1 месяц)
        var maxEndDate = request.StartDate.AddMonths(1);
        var endDate = request.EndDate > maxEndDate ? maxEndDate : request.EndDate;

        var query = _dbContext.RelicListings
            .Include(r => r.RelicDefinition)
            .Include(r => r.Attributes)
                .ThenInclude(a => a.AttributeDefinition)
            .Where(r => r.CreatedAt >= request.StartDate && r.CreatedAt <= endDate)
            .AsQueryable();

        // Применение фильтров
        if (request.RelicDefinitionId.HasValue)
        {
            query = query.Where(r => r.RelicDefinitionId == request.RelicDefinitionId.Value);
        }

        if (request.SoulLevel.HasValue)
        {
            query = query.Where(r => r.RelicDefinition.SoulLevel == request.SoulLevel.Value);
        }

        if (request.SoulType.HasValue)
        {
            var soulType = (SoulType)request.SoulType.Value;
            query = query.Where(r => r.RelicDefinition.SoulType == soulType);
        }

        if (request.ServerId.HasValue)
        {
            query = query.Where(r => r.ServerId == request.ServerId.Value);
        }

        if (request.MainAttribute != null)
        {
            query = query.Where(r => r.Attributes.Any(a =>
                a.Category == AttributeCategory.Main &&
                a.AttributeDefinitionId == request.MainAttribute.Id &&
                (!request.MainAttribute.MinValue.HasValue || a.Value >= request.MainAttribute.MinValue.Value) &&
                (!request.MainAttribute.MaxValue.HasValue || a.Value <= request.MainAttribute.MaxValue.Value)));
        }

        if (request.AdditionalAttributes is { Count: > 0 })
        {
            foreach (var attrFilter in request.AdditionalAttributes)
            {
                query = query.Where(r => r.Attributes.Any(a =>
                    a.Category == AttributeCategory.Additional &&
                    a.AttributeDefinitionId == attrFilter.Id &&
                    (!attrFilter.MinValue.HasValue || a.Value >= attrFilter.MinValue.Value) &&
                    (!attrFilter.MaxValue.HasValue || a.Value <= attrFilter.MaxValue.Value)));
            }
        }

        // Получение данных
        var listings = await query
            .Select(r => new ListingPriceData(r.Price, r.CreatedAt))
            .ToListAsync(cancellationToken);

        if (listings.Count == 0)
        {
            return CreateEmptyResponse(request, endDate);
        }

        // Группировка по периодам
        var groupBy = request.GroupBy?.ToLower() ?? "day";
        var dataPoints = GroupDataPoints(listings, groupBy);

        // Расчет статистики
        var overallMin = listings.Min(l => l.Price);
        var overallMax = listings.Max(l => l.Price);
        var overallAverage = (long)listings.Average(l => l.Price);

        var firstPeriodAvg = dataPoints.FirstOrDefault()?.AveragePrice ?? 0;
        var lastPeriodAvg = dataPoints.LastOrDefault()?.AveragePrice ?? 0;
        var priceChange = lastPeriodAvg - firstPeriodAvg;
        var priceChangePercent = firstPeriodAvg > 0 ? (double)priceChange / firstPeriodAvg * 100 : 0;

        // Получение информации о фильтрах
        var filters = await BuildFiltersDto(request, cancellationToken);

        return new GetPriceTrendsResponse
        {
            Filters = filters,
            Period = new PeriodDto(request.StartDate, endDate),
            DataPoints = dataPoints,
            Statistics = new StatisticsDto
            {
                OverallAverage = overallAverage,
                OverallMin = overallMin,
                OverallMax = overallMax,
                TotalListings = listings.Count,
                PriceChange = priceChange,
                PriceChangePercent = Math.Round(priceChangePercent, 2)
            }
        };
    }

    private List<DataPointDto> GroupDataPoints(IEnumerable<ListingPriceData> listings, string groupBy)
    {
        var grouped = groupBy switch
        {
            "hour" => listings.GroupBy(l => new DateTime(
                l.CreatedAt.Year,
                l.CreatedAt.Month,
                l.CreatedAt.Day,
                l.CreatedAt.Hour, 0, 0)),
            "week" => listings.GroupBy(l => GetStartOfWeek(l.CreatedAt)),
            _ => listings.GroupBy(l => l.CreatedAt.Date) // day
        };

        return grouped
            .OrderBy(g => g.Key)
            .Select(g => new DataPointDto
            {
                Timestamp = g.Key,
                AveragePrice = (long)g.Average(x => x.Price),
                MinPrice = g.Min(x => x.Price),
                MaxPrice = g.Max(x => x.Price),
                Count = g.Count()
            })
            .ToList();
    }

    private record ListingPriceData(long Price, DateTime CreatedAt);

    private static DateTime GetStartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }

    private async Task<FiltersDto> BuildFiltersDto(GetPriceTrendsQuery request, CancellationToken cancellationToken)
    {
        AttributeInfoDto? mainAttribute = null;
        List<AttributeInfoDto>? additionalAttributes = null;
        RelicDefinitionInfoDto? relicDefinition = null;

        if (request.MainAttribute != null)
        {
            var attr = await _dbContext.AttributeDefinitions
                .FirstOrDefaultAsync(a => a.Id == request.MainAttribute.Id, cancellationToken);
            if (attr != null)
            {
                mainAttribute = new AttributeInfoDto(attr.Id, attr.Name);
            }
        }

        if (request.AdditionalAttributes is { Count: > 0 })
        {
            var ids = request.AdditionalAttributes.Select(a => a.Id).ToList();
            var attrs = await _dbContext.AttributeDefinitions
                .Where(a => ids.Contains(a.Id))
                .ToListAsync(cancellationToken);
            additionalAttributes = attrs.Select(a => new AttributeInfoDto(a.Id, a.Name)).ToList();
        }

        if (request.RelicDefinitionId.HasValue)
        {
            var relic = await _dbContext.RelicDefinitions
                .FirstOrDefaultAsync(r => r.Id == request.RelicDefinitionId.Value, cancellationToken);
            if (relic != null)
            {
                relicDefinition = new RelicDefinitionInfoDto(relic.Id, relic.Name);
            }
        }

        return new FiltersDto
        {
            MainAttribute = mainAttribute,
            AdditionalAttributes = additionalAttributes,
            RelicDefinition = relicDefinition,
            SoulLevel = request.SoulLevel,
            SoulType = request.SoulType
        };
    }

    private GetPriceTrendsResponse CreateEmptyResponse(GetPriceTrendsQuery request, DateTime endDate)
    {
        return new GetPriceTrendsResponse
        {
            Filters = new FiltersDto
            {
                MainAttribute = null,
                AdditionalAttributes = null,
                RelicDefinition = null,
                SoulLevel = request.SoulLevel,
                SoulType = request.SoulType
            },
            Period = new PeriodDto(request.StartDate, endDate),
            DataPoints = new List<DataPointDto>(),
            Statistics = new StatisticsDto
            {
                OverallAverage = 0,
                OverallMin = 0,
                OverallMax = 0,
                TotalListings = 0,
                PriceChange = 0,
                PriceChangePercent = 0
            }
        };
    }
}
