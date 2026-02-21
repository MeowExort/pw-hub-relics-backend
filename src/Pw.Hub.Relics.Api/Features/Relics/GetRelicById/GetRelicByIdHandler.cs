using MediatR;
using Microsoft.EntityFrameworkCore;
using Pw.Hub.Relics.Api.Features.Relics.SearchRelics;
using Pw.Hub.Relics.Domain.Enums;
using Pw.Hub.Relics.Infrastructure.Data;
using Pw.Hub.Relics.Shared.Helpers;

namespace Pw.Hub.Relics.Api.Features.Relics.GetRelicById;

public class GetRelicByIdHandler : IRequestHandler<GetRelicByIdQuery, RelicListingDto?>
{
    private readonly RelicsDbContext _dbContext;

    public GetRelicByIdHandler(RelicsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RelicListingDto?> Handle(GetRelicByIdQuery request, CancellationToken cancellationToken)
    {
        var relic = await _dbContext.RelicListings
            .Include(r => r.RelicDefinition)
                .ThenInclude(rd => rd.SlotType)
            .Include(r => r.Server)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (relic == null)
        {
            return null;
        }

        var attrDefs = await _dbContext.AttributeDefinitions
            .AsNoTracking()
            .ToDictionaryAsync(ad => ad.Id, ad => ad.Name, cancellationToken);

        var mainAttr = relic.JsonAttributes.FirstOrDefault(a => a.Category == AttributeCategory.Main);
        var additionalAttrs = relic.JsonAttributes.Where(a => a.Category == AttributeCategory.Additional).ToList();

        return new RelicListingDto
        {
            Id = relic.Id,
            RelicDefinition = new RelicDefinitionDto
            {
                Id = relic.RelicDefinition.Id,
                Name = relic.RelicDefinition.Name,
                SoulLevel = relic.RelicDefinition.SoulLevel,
                SoulType = (int)relic.RelicDefinition.SoulType,
                SlotType = new SlotTypeDto(relic.RelicDefinition.SlotType.Id, relic.RelicDefinition.SlotType.Name),
                Race = (int)relic.RelicDefinition.Race,
                IconUri = relic.RelicDefinition.IconUri
            },
            AbsorbExperience = relic.AbsorbExperience,
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
            EnhancementLevel = relic.EnhancementLevel,
            Price = relic.Price,
            PriceFormatted = PriceHelper.FormatPrice(relic.Price),
            Server = new ServerDto(relic.Server.Id, relic.Server.Name, relic.Server.Key),
            CreatedAt = relic.CreatedAt
        };
    }
}
