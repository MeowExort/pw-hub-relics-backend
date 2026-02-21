using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Pw.Hub.Relics.Domain.Enums;
using Pw.Hub.Relics.Infrastructure.Data;
using System.Text;
using System.Text.Json;

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
        // Конвертация дат в UTC для PostgreSQL
        var startDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc);
        var requestEndDate = DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc);
        
        // Валидация периода (максимум 1 месяц)
        var maxEndDate = startDate.AddMonths(1);
        var endDate = requestEndDate > maxEndDate ? maxEndDate : requestEndDate;

        var connection = _dbContext.Database.GetDbConnection();
        
        // Открываем соединение, если оно закрыто (требуется для Dapper)
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var whereBuilder = new StringBuilder("WHERE rl.created_at >= @StartDate AND rl.created_at <= @EndDate");
        var parameters = new DynamicParameters();
        parameters.Add("StartDate", startDate);
        parameters.Add("EndDate", endDate);

        // Применение фильтров
        if (request.RelicDefinitionId.HasValue)
        {
            whereBuilder.Append(" AND rl.relic_definition_id = @RelicDefinitionId");
            parameters.Add("RelicDefinitionId", request.RelicDefinitionId.Value);
        }

        if (request.SoulLevel.HasValue)
        {
            whereBuilder.Append(" AND rd.soul_level = @SoulLevel");
            parameters.Add("SoulLevel", request.SoulLevel.Value);
        }

        if (request.SoulType.HasValue)
        {
            whereBuilder.Append(" AND rd.soul_type = @SoulType");
            parameters.Add("SoulType", request.SoulType.Value);
        }

        if (request.ServerId.HasValue)
        {
            whereBuilder.Append(" AND rl.server_id = @ServerId");
            parameters.Add("ServerId", request.ServerId.Value);
        }

        if (request.MainAttribute != null)
        {
            var hasMinValue = request.MainAttribute.MinValue.HasValue;
            var hasMaxValue = request.MainAttribute.MaxValue.HasValue;
            
            if (hasMinValue || hasMaxValue)
            {
                var conditions = new StringBuilder();
                conditions.Append($@"EXISTS (
                    SELECT 1 
                    FROM jsonb_to_recordset(rl.json_attributes) as x(""AttributeDefinitionId"" int, ""Value"" int, ""Category"" int) 
                    WHERE x.""AttributeDefinitionId"" = @MainAttrId 
                      AND x.""Category"" = {(int)AttributeCategory.Main}");
                
                if (hasMinValue)
                {
                    conditions.Append(" AND x.\"Value\" >= @MainAttrMinValue");
                    parameters.Add("MainAttrMinValue", request.MainAttribute.MinValue!.Value);
                }
                if (hasMaxValue)
                {
                    conditions.Append(" AND x.\"Value\" <= @MainAttrMaxValue");
                    parameters.Add("MainAttrMaxValue", request.MainAttribute.MaxValue!.Value);
                }
                conditions.Append(")");
                
                whereBuilder.Append(" AND ");
                whereBuilder.Append(conditions);
                parameters.Add("MainAttrId", request.MainAttribute.Id);
            }
            else
            {
                // Просто проверяем наличие атрибута
                whereBuilder.Append(" AND rl.json_attributes @> @MainAttrJson::jsonb");
                var mainAttrJson = JsonSerializer.Serialize(new[] 
                { 
                    new { AttributeDefinitionId = request.MainAttribute.Id, Category = (int)AttributeCategory.Main } 
                });
                parameters.Add("MainAttrJson", mainAttrJson);
            }
        }

        if (request.AdditionalAttributes is { Count: > 0 })
        {
            for (int i = 0; i < request.AdditionalAttributes.Count; i++)
            {
                var attr = request.AdditionalAttributes[i];
                var pId = $"AttrId_{i}";
                var hasMinValue = attr.MinValue.HasValue;
                var hasMaxValue = attr.MaxValue.HasValue;
                
                if (hasMinValue || hasMaxValue)
                {
                    var conditions = new StringBuilder();
                    conditions.Append($@"EXISTS (
                        SELECT 1 
                        FROM jsonb_to_recordset(rl.json_attributes) as x(""AttributeDefinitionId"" int, ""Value"" int, ""Category"" int) 
                        WHERE x.""AttributeDefinitionId"" = @{pId} 
                          AND x.""Category"" = {(int)AttributeCategory.Additional}");
                    
                    if (hasMinValue)
                    {
                        var pMin = $"AttrMin_{i}";
                        conditions.Append($" AND x.\"Value\" >= @{pMin}");
                        parameters.Add(pMin, attr.MinValue!.Value);
                    }
                    if (hasMaxValue)
                    {
                        var pMax = $"AttrMax_{i}";
                        conditions.Append($" AND x.\"Value\" <= @{pMax}");
                        parameters.Add(pMax, attr.MaxValue!.Value);
                    }
                    conditions.Append(")");
                    
                    whereBuilder.Append(" AND ");
                    whereBuilder.Append(conditions);
                }
                else
                {
                    whereBuilder.Append($@" AND EXISTS (
                        SELECT 1 
                        FROM jsonb_to_recordset(rl.json_attributes) as x(""AttributeDefinitionId"" int, ""Value"" int, ""Category"" int) 
                        WHERE x.""AttributeDefinitionId"" = @{pId} 
                          AND x.""Category"" = {(int)AttributeCategory.Additional}
                    )");
                }
                parameters.Add(pId, attr.Id);
            }
        }

        // Основной запрос
        var sql = $@"
            SELECT rl.price, rl.created_at
            FROM relic_listings rl
            JOIN relic_definitions rd ON rl.relic_definition_id = rd.id
            {whereBuilder}";

        var rawListings = await connection.QueryAsync<ListingPriceData>(sql, parameters);
        var listings = rawListings.ToList();

        if (listings.Count == 0)
        {
            return CreateEmptyResponse(request, startDate, endDate);
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
            Period = new PeriodDto(startDate, endDate),
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

    private class ListingPriceData
    {
        public long Price { get; set; }
        public DateTime Created_At { get; set; }
        
        // Alias for cleaner code access
        public DateTime CreatedAt => Created_At;
    }

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

    private GetPriceTrendsResponse CreateEmptyResponse(GetPriceTrendsQuery request, DateTime startDate, DateTime endDate)
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
            Period = new PeriodDto(startDate, endDate),
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
