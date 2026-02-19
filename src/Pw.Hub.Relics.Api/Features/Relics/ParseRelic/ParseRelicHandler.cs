using MediatR;
using Microsoft.EntityFrameworkCore;
using Pw.Hub.Relics.Api.BackgroundJobs;
using Pw.Hub.Relics.Domain.Entities;
using Pw.Hub.Relics.Domain.Enums;
using Pw.Hub.Relics.Infrastructure.Data;
using Pw.Hub.Relics.Shared.Packets;

namespace Pw.Hub.Relics.Api.Features.Relics.ParseRelic;

public class ParseRelicHandler : IRequestHandler<ParseRelicCommand, ParseRelicResult>
{
    private readonly RelicsDbContext _dbContext;
    private readonly ILogger<ParseRelicHandler> _logger;
    private readonly INotificationProcessor _notificationProcessor;

    public ParseRelicHandler(
        RelicsDbContext dbContext, 
        ILogger<ParseRelicHandler> logger,
        INotificationProcessor notificationProcessor)
    {
        _dbContext = dbContext;
        _logger = logger;
        _notificationProcessor = notificationProcessor;
    }

    public async Task<ParseRelicResult> Handle(ParseRelicCommand request, CancellationToken cancellationToken)
    {
        using var packetStream = new PacketStream(request.Data);

        var packet = new GetRelicDetail_Re();
        packet.Read(packetStream);

        if (packet.lots is null || packet.lots.Count == 0)
        {
            _logger.LogWarning("[ParseRelic] Lots can't be null or empty.");
            return new ParseRelicResult(0, 0, "Lots can't be null or empty.");
        }

        _logger.LogInformation("[ParseRelic] Received {Count} lots.", packet.lots.Count);

        var server = await _dbContext.ServerDefinitions
            .FirstOrDefaultAsync(s => s.Key.ToLower() == request.Server.ToLower(), cancellationToken);

        if (server == null)
        {
            _logger.LogWarning("[ParseRelic] Server '{Server}' not found.", request.Server);
            return new ParseRelicResult(0, 0, $"Server '{request.Server}' not found.");
        }

        var createdCount = 0;
        var updatedCount = 0;

        try
        {
            foreach (var lot in packet.lots)
            {
                // Найти RelicDefinition по relic_item.id
                var relicDefinition = await _dbContext.RelicDefinitions
                    .FirstOrDefaultAsync(rd => rd.Id == lot.relic_item.id, cancellationToken);

                if (relicDefinition == null)
                {
                    _logger.LogWarning("[ParseRelic] RelicDefinition not found for id={RelicId}.", lot.relic_item.id);
                    continue;
                }

                // Найти существующий лот по уникальному ключу, включая атрибуты и заточку
                var candidateListings = await _dbContext.RelicListings
                    .Include(rl => rl.Attributes)
                    .Where(rl =>
                        rl.SellerCharacterId == lot.sell_id.player_id &&
                        rl.ShopPosition == lot.sell_id.pos_in_shop &&
                        rl.ServerId == server.Id &&
                        rl.RelicDefinitionId == relicDefinition.Id &&
                        rl.EnhancementLevel == lot.relic_item.reserve)
                    .ToListAsync(cancellationToken);

                // Фильтрация по атрибутам (основной + дополнительные)
                var existingListing = candidateListings.FirstOrDefault(rl =>
                {
                    // Проверка основного атрибута
                    var mainAttr = rl.Attributes.FirstOrDefault(a => a.Category == AttributeCategory.Main);
                    if (lot.relic_item.main_addon >= 0)
                    {
                        if (mainAttr == null || mainAttr.AttributeDefinitionId != lot.relic_item.main_addon)
                            return false;
                    }
                    else if (mainAttr != null)
                    {
                        return false;
                    }

                    // Проверка дополнительных атрибутов
                    var additionalAttrs = rl.Attributes
                        .Where(a => a.Category == AttributeCategory.Additional)
                        .Select(a => (a.AttributeDefinitionId, a.Value))
                        .OrderBy(a => a.AttributeDefinitionId)
                        .ThenBy(a => a.Value)
                        .ToList();

                    var incomingAddons = lot.relic_item.addons
                        .Select(a => (a.id, a.value))
                        .OrderBy(a => a.id)
                        .ThenBy(a => a.value)
                        .ToList();

                    return additionalAttrs.SequenceEqual(incomingAddons);
                });

                if (existingListing != null)
                {
                    // Обновить существующий лот
                    existingListing.LastSeenAt = DateTime.UtcNow;
                    existingListing.IsActive = true;
                    existingListing.AbsorbExperience = lot.relic_item.exp;
                    existingListing.EnhancementLevel = lot.relic_item.reserve;
                    existingListing.Price = lot.price;

                    // Обновить атрибуты
                    _dbContext.RelicAttributes.RemoveRange(existingListing.Attributes);
                    existingListing.Attributes = CreateAttributes(lot.relic_item, existingListing.Id);

                    updatedCount++;
                }
                else
                {
                    // Создать новый лот
                    var newListing = new RelicListing
                    {
                        Id = Guid.NewGuid(),
                        RelicDefinitionId = relicDefinition.Id,
                        AbsorbExperience = lot.relic_item.exp,
                        EnhancementLevel = lot.relic_item.reserve,
                        SellerCharacterId = lot.sell_id.player_id,
                        ShopPosition = lot.sell_id.pos_in_shop,
                        Price = lot.price,
                        ServerId = server.Id,
                        CreatedAt = DateTime.UtcNow,
                        LastSeenAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    newListing.Attributes = CreateAttributes(lot.relic_item, newListing.Id);

                    _dbContext.RelicListings.Add(newListing);

                    // Загрузить RelicDefinition для уведомлений
                    newListing.RelicDefinition = relicDefinition;

                    // Обработать уведомления для нового лота (fire and forget)
                    _ = _notificationProcessor.ProcessNewListingAsync(newListing, CancellationToken.None);

                    createdCount++;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return new ParseRelicResult(createdCount, updatedCount, $"Successfully processed {packet.lots.Count} lots. Created: {createdCount}, Updated: {updatedCount}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ParseRelic] Error processing lots.");
            throw;
        }
    }

    private static List<RelicAttribute> CreateAttributes(Relic relic, Guid listingId)
    {
        var attributes = new List<RelicAttribute>();

        // Основной атрибут
        if (relic.main_addon >= 0)
        {
            attributes.Add(new RelicAttribute
            {
                Id = Guid.NewGuid(),
                RelicListingId = listingId,
                AttributeDefinitionId = relic.main_addon,
                Value = 0, // Значение основного атрибута не передаётся в пакете
                Category = AttributeCategory.Main
            });
        }

        // Дополнительные атрибуты
        foreach (var addon in relic.addons)
        {
            attributes.Add(new RelicAttribute
            {
                Id = Guid.NewGuid(),
                RelicListingId = listingId,
                AttributeDefinitionId = addon.id,
                Value = addon.value,
                Category = AttributeCategory.Additional
            });
        }

        return attributes;
    }
}
