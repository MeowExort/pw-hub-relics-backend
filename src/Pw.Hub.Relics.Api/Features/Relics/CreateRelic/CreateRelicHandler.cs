using MediatR;
using Microsoft.EntityFrameworkCore;
using Pw.Hub.Relics.Api.BackgroundJobs;
using Pw.Hub.Relics.Domain.Entities;
using Pw.Hub.Relics.Domain.Enums;
using Pw.Hub.Relics.Infrastructure.Data;

namespace Pw.Hub.Relics.Api.Features.Relics.CreateRelic;

public class CreateRelicHandler : IRequestHandler<CreateRelicCommand, CreateRelicResult>
{
    private readonly RelicsDbContext _dbContext;
    private readonly INotificationProcessor _notificationProcessor;

    public CreateRelicHandler(RelicsDbContext dbContext, INotificationProcessor notificationProcessor)
    {
        _dbContext = dbContext;
        _notificationProcessor = notificationProcessor;
    }

    public async Task<CreateRelicResult> Handle(CreateRelicCommand request, CancellationToken cancellationToken)
    {
        // 0. Убедиться, что все AttributeDefinition существуют
        var attributeIds = new List<int> { request.MainAttribute.AttributeDefinitionId };
        if (request.AdditionalAttributes != null)
        {
            attributeIds.AddRange(request.AdditionalAttributes.Select(a => a.AttributeDefinitionId));
        }
        await EnsureAttributeDefinitionsExistAsync(attributeIds.Distinct().ToList(), cancellationToken);

        // 1. Найти RelicDefinition по параметрам
        var relicDefinition = await _dbContext.RelicDefinitions
            .FirstOrDefaultAsync(rd =>
                rd.SoulLevel == request.SoulLevel &&
                rd.SoulType == (SoulType)request.SoulType &&
                rd.SlotTypeId == request.SlotTypeId &&
                rd.Race == (Race)request.Race, cancellationToken);

        if (relicDefinition == null)
        {
            throw new InvalidOperationException(
                $"Relic definition not found for SoulLevel={request.SoulLevel}, SoulType={request.SoulType}, SlotTypeId={request.SlotTypeId}, Race={request.Race}");
        }

        // 2. Найти существующий лот по уникальному ключу (продавец + позиция + сервер)
        var existingListing = await _dbContext.RelicListings
            .FirstOrDefaultAsync(rl =>
                rl.SellerCharacterId == request.SellerCharacterId &&
                rl.ShopPosition == request.ShopPosition &&
                rl.ServerId == request.ServerId, cancellationToken);

        if (existingListing != null)
        {
            // Обновить существующий лот
            existingListing.LastSeenAt = DateTime.UtcNow;
            existingListing.IsActive = true;
            existingListing.RelicDefinitionId = relicDefinition.Id;
            existingListing.AbsorbExperience = request.AbsorbExperience;
            existingListing.EnhancementLevel = request.EnhancementLevel;
            existingListing.Price = request.Price;

            // Обновить атрибуты
            existingListing.JsonAttributes = CreateAttributesDto(request);

            await _dbContext.SaveChangesAsync(cancellationToken);
            return new CreateRelicResult(existingListing.Id, IsUpdated: true);
        }

        // 3. Создать новый лот
        var newListing = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinitionId = relicDefinition.Id,
            AbsorbExperience = request.AbsorbExperience,
            EnhancementLevel = request.EnhancementLevel,
            SellerCharacterId = request.SellerCharacterId,
            ShopPosition = request.ShopPosition,
            Price = request.Price,
            ServerId = request.ServerId,
            CreatedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow,
            IsActive = true,
            JsonAttributes = CreateAttributesDto(request)
        };

        _dbContext.RelicListings.Add(newListing);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Загрузить RelicDefinition для уведомлений
        newListing.RelicDefinition = relicDefinition;
        
        // Обработать уведомления для нового лота (fire and forget)
        _ = _notificationProcessor.ProcessNewListingAsync(newListing, CancellationToken.None);

        return new CreateRelicResult(newListing.Id, IsUpdated: false);
    }

    private static List<RelicAttributeDto> CreateAttributesDto(CreateRelicCommand request)
    {
        var attributes = new List<RelicAttributeDto>
        {
            new(
                request.MainAttribute.AttributeDefinitionId,
                request.MainAttribute.Value,
                AttributeCategory.Main
            )
        };

        if (request.AdditionalAttributes != null)
        {
            foreach (var attr in request.AdditionalAttributes)
            {
                attributes.Add(new RelicAttributeDto(
                    attr.AttributeDefinitionId,
                    attr.Value,
                    AttributeCategory.Additional
                ));
            }
        }

        return attributes;
    }

    private async Task EnsureAttributeDefinitionsExistAsync(List<int> attributeIds, CancellationToken cancellationToken)
    {
        if (attributeIds.Count == 0)
            return;

        var existingIds = await _dbContext.AttributeDefinitions
            .Where(ad => attributeIds.Contains(ad.Id))
            .Select(ad => ad.Id)
            .ToListAsync(cancellationToken);

        var missingIds = attributeIds.Except(existingIds).ToList();

        if (missingIds.Count > 0)
        {
            foreach (var id in missingIds)
            {
                _dbContext.AttributeDefinitions.Add(new AttributeDefinition
                {
                    Id = id,
                    Name = $"Attribute_{id}"
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
