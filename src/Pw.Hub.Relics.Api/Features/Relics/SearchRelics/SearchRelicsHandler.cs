using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Pw.Hub.Relics.Domain.Enums;
using Pw.Hub.Relics.Infrastructure.Data;
using Pw.Hub.Relics.Shared.Helpers;
using System.Text;
using System.Text.Json;
using DomainRelicAttributeDto = Pw.Hub.Relics.Domain.Entities.RelicAttributeDto;

namespace Pw.Hub.Relics.Api.Features.Relics.SearchRelics;

public class SearchRelicsHandler : IRequestHandler<SearchRelicsQuery, SearchRelicsResponse>
{
    private readonly RelicsDbContext _dbContext;

    public SearchRelicsHandler(RelicsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SearchRelicsResponse> Handle(SearchRelicsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(request.PageSize, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);
        
        var connection = _dbContext.Database.GetDbConnection();

        var whereBuilder = new StringBuilder("WHERE rl.is_active = true");
        var parameters = new DynamicParameters();

        if (request.SoulType.HasValue)
        {
            whereBuilder.Append(" AND rd.soul_type = @SoulType");
            parameters.Add("SoulType", request.SoulType.Value);
        }

        if (request.SlotTypeId.HasValue)
        {
            whereBuilder.Append(" AND rd.slot_type_id = @SlotTypeId");
            parameters.Add("SlotTypeId", request.SlotTypeId.Value);
        }

        if (request.Race.HasValue)
        {
            whereBuilder.Append(" AND rd.race = @Race");
            parameters.Add("Race", request.Race.Value);
        }

        if (request.SoulLevel.HasValue)
        {
            whereBuilder.Append(" AND rd.soul_level = @SoulLevel");
            parameters.Add("SoulLevel", request.SoulLevel.Value);
        }

        if (request.MainAttributeId.HasValue)
        {
            // Поиск по JSONB массиву для MainAttribute
            whereBuilder.Append(" AND rl.json_attributes @> @MainAttrJson::jsonb");
            var mainAttrJson = JsonSerializer.Serialize(new[] 
            { 
                new { AttributeDefinitionId = request.MainAttributeId.Value, Category = (int)AttributeCategory.Main } 
            });
            parameters.Add("MainAttrJson", mainAttrJson);
        }

        if (request.AdditionalAttributes is { Count: > 0 })
        {
            for (int i = 0; i < request.AdditionalAttributes.Count; i++)
            {
                var attr = request.AdditionalAttributes[i];
                var pId = $"AttrId_{i}";
                var pMin = $"AttrMin_{i}";
                
                // Используем jsonb_to_recordset для поиска значения
                whereBuilder.Append($@" AND EXISTS (
                    SELECT 1 
                    FROM jsonb_to_recordset(rl.json_attributes) as x(""AttributeDefinitionId"" int, ""Value"" int, ""Category"" int) 
                    WHERE x.""AttributeDefinitionId"" = @{pId} 
                      AND x.""Category"" = {(int)AttributeCategory.Additional} 
                      AND (@{pMin} IS NULL OR x.""Value"" >= @{pMin})
                )");
                
                parameters.Add(pId, attr.Id);
                parameters.Add(pMin, attr.MinValue);
            }
        }

        if (request.MinPrice.HasValue)
        {
            whereBuilder.Append(" AND rl.price >= @MinPrice");
            parameters.Add("MinPrice", request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            whereBuilder.Append(" AND rl.price <= @MaxPrice");
            parameters.Add("MaxPrice", request.MaxPrice.Value);
        }

        if (request.ServerId.HasValue)
        {
            whereBuilder.Append(" AND rl.server_id = @ServerId");
            parameters.Add("ServerId", request.ServerId.Value);
        }

        if (request.MinEnhancementLevel.HasValue)
        {
            whereBuilder.Append(" AND rl.enhancement_level >= @MinEnhancementLevel");
            parameters.Add("MinEnhancementLevel", request.MinEnhancementLevel.Value);
        }

        if (request.MaxEnhancementLevel.HasValue)
        {
            whereBuilder.Append(" AND rl.enhancement_level <= @MaxEnhancementLevel");
            parameters.Add("MaxEnhancementLevel", request.MaxEnhancementLevel.Value);
        }

        if (request.MinAbsorbExperience.HasValue)
        {
            whereBuilder.Append(" AND rl.absorb_experience >= @MinAbsorbExperience");
            parameters.Add("MinAbsorbExperience", request.MinAbsorbExperience.Value);
        }

        if (request.MaxAbsorbExperience.HasValue)
        {
            whereBuilder.Append(" AND rl.absorb_experience <= @MaxAbsorbExperience");
            parameters.Add("MaxAbsorbExperience", request.MaxAbsorbExperience.Value);
        }

        // Подсчет общего количества
        var countSql = $@"
            SELECT COUNT(*) 
            FROM relic_listings rl
            JOIN relic_definitions rd ON rl.relic_definition_id = rd.id
            {whereBuilder}";

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        // Сортировка
        var sortBuilder = new StringBuilder();
        var sortDirection = request.SortDirection?.ToLower() == "asc" ? "ASC" : "DESC";
        var sortBy = request.SortBy?.ToLower();

        if (sortBy == "price")
        {
            sortBuilder.Append($"ORDER BY rl.price {sortDirection}");
        }
        else if (sortBy == "enhancementlevel")
        {
            sortBuilder.Append($"ORDER BY rl.enhancement_level {sortDirection}, rl.price DESC");
        }
        else if (sortBy == "attributevalue" && request.SortAttributeId.HasValue)
        {
            sortBuilder.Append($@"ORDER BY (
                SELECT (x.""Value"")::int 
                FROM jsonb_to_recordset(rl.json_attributes) as x(""AttributeDefinitionId"" int, ""Value"" int, ""Category"" int)
                WHERE x.""AttributeDefinitionId"" = @SortAttributeId AND x.""Category"" = {(int)AttributeCategory.Additional}
                LIMIT 1
            ) {sortDirection}, rl.price DESC");
            parameters.Add("SortAttributeId", request.SortAttributeId.Value);
        }
        else
        {
            sortBuilder.Append("ORDER BY rl.created_at DESC, rl.price DESC");
        }

        // Основной запрос
        var sql = $@"
            SELECT 
                rl.id, rl.relic_definition_id, rl.absorb_experience, rl.json_attributes::text as JsonAttributesRaw, 
                rl.enhancement_level, rl.price, rl.server_id, rl.created_at,
                rd.id as RelicId, rd.name as RelicName, rd.soul_level as RelicSoulLevel, rd.soul_type as RelicSoulType, rd.race as RelicRace, rd.icon_uri as RelicIconUri,
                st.id as SlotTypeId, st.name as SlotTypeName,
                sd.id as ServerId, sd.name as ServerName, sd.key as ServerKey
            FROM relic_listings rl
            JOIN relic_definitions rd ON rl.relic_definition_id = rd.id
            JOIN slot_types st ON rd.slot_type_id = st.id
            JOIN server_definitions sd ON rl.server_id = sd.id
            {whereBuilder}
            {sortBuilder}
            OFFSET @Offset LIMIT @Limit";

        parameters.Add("Offset", (pageNumber - 1) * pageSize);
        parameters.Add("Limit", pageSize);

        var rawItems = await connection.QueryAsync<dynamic>(sql, parameters);

        // Загрузка определений атрибутов (можно закэшировать, но пока так)
        var attrDefs = await _dbContext.AttributeDefinitions
            .AsNoTracking()
            .ToDictionaryAsync(ad => ad.Id, ad => ad.Name, cancellationToken);

        var itemDtos = new List<RelicListingDto>();

        foreach (var row in rawItems)
        {
            var jsonAttributes = JsonSerializer.Deserialize<List<DomainRelicAttributeDto>>((string)row.JsonAttributesRaw) ?? new List<DomainRelicAttributeDto>();
            var mainAttr = jsonAttributes.FirstOrDefault(a => a.Category == AttributeCategory.Main);
            var additionalAttrs = jsonAttributes.Where(a => a.Category == AttributeCategory.Additional).ToList();

            itemDtos.Add(new RelicListingDto
            {
                Id = row.id,
                RelicDefinition = new RelicDefinitionDto
                {
                    Id = row.RelicId,
                    Name = row.RelicName,
                    SoulLevel = row.RelicSoulLevel,
                    SoulType = row.RelicSoulType,
                    SlotType = new SlotTypeDto(row.SlotTypeId, row.SlotTypeName),
                    Race = row.RelicRace,
                    IconUri = row.RelicIconUri
                },
                AbsorbExperience = row.absorb_experience,
                MainAttribute = mainAttr != null
                    ? new RelicAttributeDto
                    {
                        AttributeDefinition = new AttributeDefinitionDto(
                            mainAttr.AttributeDefinitionId,
                            attrDefs.GetValueOrDefault(mainAttr.AttributeDefinitionId, "Unknown")),
                        Value = mainAttr.Value
                    }
                    : new RelicAttributeDto
                    {
                        AttributeDefinition = new AttributeDefinitionDto(0, "Unknown"),
                        Value = 0
                    },
                AdditionalAttributes = additionalAttrs.Select(a => new RelicAttributeDto
                {
                    AttributeDefinition = new AttributeDefinitionDto(
                        a.AttributeDefinitionId,
                        attrDefs.GetValueOrDefault(a.AttributeDefinitionId, "Unknown")),
                    Value = a.Value
                }).ToList(),
                EnhancementLevel = row.enhancement_level,
                Price = row.price,
                PriceFormatted = PriceHelper.FormatPrice(row.price),
                Server = new ServerDto(row.ServerId, row.ServerName, row.ServerKey),
                CreatedAt = row.created_at
            });
        }

        return new SearchRelicsResponse
        {
            Items = itemDtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }
}
