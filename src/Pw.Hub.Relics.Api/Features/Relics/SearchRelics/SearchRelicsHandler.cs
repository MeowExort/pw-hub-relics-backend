using MediatR;
using Microsoft.EntityFrameworkCore;
using Pw.Hub.Relics.Domain.Enums;
using Pw.Hub.Relics.Infrastructure.Data;
using Pw.Hub.Relics.Shared.Helpers;

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
        // Ограничение размера страницы
        var pageSize = Math.Min(request.PageSize, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        var query = _dbContext.RelicListings
            .Include(r => r.RelicDefinition)
                .ThenInclude(rd => rd.SlotType)
            .Include(r => r.Server)
            .Where(r => r.IsActive)
            .AsNoTracking()
            .AsQueryable();

        // Применение фильтров
        if (request.SoulType.HasValue)
        {
            var soulType = (SoulType)request.SoulType.Value;
            query = query.Where(r => r.RelicDefinition.SoulType == soulType);
        }

        if (request.SlotTypeId.HasValue)
        {
            query = query.Where(r => r.RelicDefinition.SlotTypeId == request.SlotTypeId.Value);
        }

        if (request.Race.HasValue)
        {
            var race = (Race)request.Race.Value;
            query = query.Where(r => r.RelicDefinition.Race == race);
        }

        if (request.SoulLevel.HasValue)
        {
            query = query.Where(r => r.RelicDefinition.SoulLevel == request.SoulLevel.Value);
        }

        if (request.MainAttributeId.HasValue)
        {
            query = query.Where(r => r.JsonAttributes.Any(a => 
                a.Category == AttributeCategory.Main && 
                a.AttributeDefinitionId == request.MainAttributeId.Value));
        }

        if (request.AdditionalAttributes is { Count: > 0 })
        {
            foreach (var attr in request.AdditionalAttributes)
            {
                query = query.Where(r => r.JsonAttributes.Any(a => 
                    a.Category == AttributeCategory.Additional && 
                    a.AttributeDefinitionId == attr.Id &&
                    (!attr.MinValue.HasValue || a.Value >= attr.MinValue.Value)));
            }
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(r => r.Price >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(r => r.Price <= request.MaxPrice.Value);
        }

        if (request.ServerId.HasValue)
        {
            query = query.Where(r => r.ServerId == request.ServerId.Value);
        }

        if (request.MinEnhancementLevel.HasValue)
        {
            query = query.Where(r => r.EnhancementLevel >= request.MinEnhancementLevel.Value);
        }

        if (request.MaxEnhancementLevel.HasValue)
        {
            query = query.Where(r => r.EnhancementLevel <= request.MaxEnhancementLevel.Value);
        }

        if (request.MinAbsorbExperience.HasValue)
        {
            query = query.Where(r => r.AbsorbExperience >= request.MinAbsorbExperience.Value);
        }

        if (request.MaxAbsorbExperience.HasValue)
        {
            query = query.Where(r => r.AbsorbExperience <= request.MaxAbsorbExperience.Value);
        }

        // Подсчет общего количества
        var totalCount = await query.CountAsync(cancellationToken);

        // Пагинация и сортировка
        var sortDirection = request.SortDirection?.ToLower() == "asc" ? "asc" : "desc";
        var sortBy = request.SortBy?.ToLower();

        IOrderedQueryable<Domain.Entities.RelicListing> orderedQuery;

        if (sortBy == "price")
        {
            orderedQuery = sortDirection == "asc" 
                ? query.OrderBy(r => r.Price) 
                : query.OrderByDescending(r => r.Price);
        }
        else if (sortBy == "enhancementlevel")
        {
            orderedQuery = sortDirection == "asc" 
                ? query.OrderBy(r => r.EnhancementLevel).ThenByDescending(r => r.Price) 
                : query.OrderByDescending(r => r.EnhancementLevel).ThenByDescending(r => r.Price);
        }
        else if (sortBy == "attributevalue" && request.SortAttributeId.HasValue)
        {
            if (sortDirection == "asc")
            {
                orderedQuery = query.OrderBy(r => r.JsonAttributes
                        .Where(a => a.AttributeDefinitionId == request.SortAttributeId.Value && a.Category == AttributeCategory.Additional)
                        .Select(a => (int?)a.Value)
                        .FirstOrDefault() ?? 0)
                    .ThenByDescending(r => r.Price);
            }
            else
            {
                orderedQuery = query.OrderByDescending(r => r.JsonAttributes
                        .Where(a => a.AttributeDefinitionId == request.SortAttributeId.Value && a.Category == AttributeCategory.Additional)
                        .Select(a => (int?)a.Value)
                        .FirstOrDefault() ?? 0)
                    .ThenByDescending(r => r.Price);
            }
        }
        else
        {
            // По умолчанию сортировка по CreatedAt (как было)
            // Но в требовании сказано: "По умолчанию вторым параметром сортировки должна быть цена."
            // А основным по умолчанию оставим CreatedAt, если не указано иное.
            orderedQuery = query.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Price);
        }

        var items = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Предварительная загрузка всех определений атрибутов для маппинга
        var attrDefs = await _dbContext.AttributeDefinitions
            .AsNoTracking()
            .ToDictionaryAsync(ad => ad.Id, ad => ad.Name, cancellationToken);

        // Маппинг в DTO
        var itemDtos = items.Select(r =>
        {
            var mainAttr = r.JsonAttributes.FirstOrDefault(a => a.Category == AttributeCategory.Main);
            var additionalAttrs = r.JsonAttributes.Where(a => a.Category == AttributeCategory.Additional).ToList();

            return new RelicListingDto
            {
                Id = r.Id,
                RelicDefinition = new RelicDefinitionDto
                {
                    Id = r.RelicDefinition.Id,
                    Name = r.RelicDefinition.Name,
                    SoulLevel = r.RelicDefinition.SoulLevel,
                    SoulType = (int)r.RelicDefinition.SoulType,
                    SlotType = new SlotTypeDto(r.RelicDefinition.SlotType.Id, r.RelicDefinition.SlotType.Name),
                    Race = (int)r.RelicDefinition.Race,
                    IconUri = r.RelicDefinition.IconUri
                },
                AbsorbExperience = r.AbsorbExperience,
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
                EnhancementLevel = r.EnhancementLevel,
                Price = r.Price,
                PriceFormatted = PriceHelper.FormatPrice(r.Price),
                Server = new ServerDto(r.Server.Id, r.Server.Name, r.Server.Key),
                CreatedAt = r.CreatedAt
            };
        }).ToList();

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
