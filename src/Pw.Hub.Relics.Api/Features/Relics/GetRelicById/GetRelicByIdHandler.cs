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
            .Include(r => r.Attributes)
                .ThenInclude(a => a.AttributeDefinition)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (relic == null)
        {
            return null;
        }

        var mainAttr = relic.Attributes.FirstOrDefault(a => a.Category == AttributeCategory.Main);
        var additionalAttrs = relic.Attributes.Where(a => a.Category == AttributeCategory.Additional).ToList();

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
                        mainAttr.AttributeDefinition.Id,
                        mainAttr.AttributeDefinition.Name),
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
                    a.AttributeDefinition.Id,
                    a.AttributeDefinition.Name),
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
