using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Pw.Hub.Relics.Api.BackgroundJobs;
using Pw.Hub.Relics.Api.Helpers;
using Pw.Hub.Relics.Domain.Entities;
using Pw.Hub.Relics.Domain.Enums;
using Pw.Hub.Relics.Infrastructure.Data;
using Pw.Hub.Relics.Shared.Helpers;
using Pw.Hub.Relics.Shared.Packets;
using Pw.Hub.Relics.Shared.Packets.IO;

namespace Pw.Hub.Relics.Api.Features.Relics.ParseRelic;

public class ParseRelicHandler : IRequestHandler<ParseRelicCommand, ParseRelicResult>
{
    private readonly RelicsDbContext _dbContext;
    private readonly ILogger<ParseRelicHandler> _logger;
    private readonly INotificationQueue _notificationQueue;
    private readonly IMemoryCache _cache;

    public ParseRelicHandler(
        RelicsDbContext dbContext,
        ILogger<ParseRelicHandler> logger,
        INotificationQueue notificationQueue,
        IMemoryCache cache)
    {
        _dbContext = dbContext;
        _logger = logger;
        _notificationQueue = notificationQueue;
        _cache = cache;
    }

    public async Task<ParseRelicResult> Handle(ParseRelicCommand request, CancellationToken cancellationToken)
    {
        using var packetStream = new PacketStream(request.Data);

        var packet = new GetRelicDetail_Re();
        packet.Read(packetStream);

        if (packet.lots is null || packet.lots.Count == 0)
        {
            return new ParseRelicResult(0, 0, "Lots can't be null or empty.");
        }

        // Задача 2: Кэширование сервера
        var server = await GetServerAsync(request.Server, cancellationToken);
        if (server == null)
        {
            return new ParseRelicResult(0, 0, $"Server '{request.Server}' not found.");
        }

        // Задача 1: Batch-загрузка RelicDefinitions
        var relicIds = packet.lots.Select(l => l.relic_item.id).Distinct().ToList();
        var definitions = await GetRelicDefinitionsAsync(relicIds, cancellationToken);

        // Задача 1: Batch-загрузка существующих листингов
        var sellerIds = packet.lots.Select(l => (long)l.sell_id.player_id).Distinct().ToList();
        var existingListings = await _dbContext.RelicListings
            .Include(rl => rl.Attributes)
            .Where(rl => rl.ServerId == server.Id && sellerIds.Contains(rl.SellerCharacterId))
            .ToListAsync(cancellationToken);

        var listingsLookup = existingListings
            .GroupBy(rl => (rl.SellerCharacterId, rl.ShopPosition))
            .ToDictionary(g => g.Key, g => g.ToList());

        // Задача 7: Счётчики для итогового логирования
        var createdCount = 0;
        var updatedCount = 0;
        var skippedCount = 0;
        var newListings = new List<RelicListing>();
        var skippedRelicIds = new List<int>();

        foreach (var lot in packet.lots)
        {
            if (!definitions.TryGetValue(lot.relic_item.id, out var relicDefinition))
            {
                skippedCount++;
                skippedRelicIds.Add(lot.relic_item.id);
                continue;
            }

            var key = (lot.sell_id.player_id, lot.sell_id.pos_in_shop);

            // Задача 3: Вычисление хеша атрибутов
            var incomingHash = AttributeHashHelper.ComputeHashFromRelic(lot.relic_item);

            // Поиск существующего листинга по уникальному индексу (seller_character_id, shop_position, server_id, relic_definition_id)
            RelicListing? existingListing = null;
            if (listingsLookup.TryGetValue(key, out var candidates))
            {
                existingListing = candidates.FirstOrDefault(rl =>
                    rl.RelicDefinitionId == relicDefinition.Id);
            }

            if (existingListing != null)
            {
                // Задача 4: Умное обновление - только изменившиеся поля
                existingListing.LastSeenAt = DateTime.UtcNow;
                existingListing.IsActive = true;
                existingListing.AbsorbExperience = GetAbsorbExperience(lot.relic_item, relicDefinition);
                existingListing.Price = lot.price;
                existingListing.EnhancementLevel = lot.relic_item.reserve;
                
                // Обновляем атрибуты только если хеш изменился
                if (existingListing.AttributesHash != incomingHash)
                {
                    existingListing.AttributesHash = incomingHash;
                    // Удаляем старые атрибуты напрямую в базе (без concurrency проверки)
                    // и отсоединяем их от контекста
                    await _dbContext.RelicAttributes
                        .Where(a => a.RelicListingId == existingListing.Id)
                        .ExecuteDeleteAsync(cancellationToken);
                    
                    // Очищаем коллекцию в памяти и создаём новые атрибуты
                    existingListing.Attributes.Clear();
                    existingListing.Attributes = CreateAttributes(lot.relic_item, existingListing.Id);
                }
                updatedCount++;
            }
            else
            {
                // Создать новый лот
                var newListing = new RelicListing
                {
                    Id = Guid.NewGuid(),
                    RelicDefinitionId = relicDefinition.Id,
                    AbsorbExperience = GetAbsorbExperience(lot.relic_item, relicDefinition),
                    EnhancementLevel = lot.relic_item.reserve,
                    SellerCharacterId = lot.sell_id.player_id,
                    ShopPosition = lot.sell_id.pos_in_shop,
                    Price = lot.price,
                    ServerId = server.Id,
                    CreatedAt = DateTime.UtcNow,
                    LastSeenAt = DateTime.UtcNow,
                    IsActive = true,
                    AttributesHash = incomingHash
                };

                newListing.Attributes = CreateAttributes(lot.relic_item, newListing.Id);

                _dbContext.RelicListings.Add(newListing);
                newListings.Add(newListing);
                createdCount++;
            }
        }

        // Retry-логика для обработки DbUpdateConcurrencyException
        const int maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                break;
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(
                    "[ParseRelic] Concurrency conflict on attempt {Attempt}/{MaxRetries}. Retrying...",
                    attempt, maxRetries);

                foreach (var entry in ex.Entries)
                {
                    var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                    
                    if (databaseValues == null)
                    {
                        // Запись была удалена другим процессом - убираем из контекста
                        entry.State = EntityState.Detached;
                    }
                    else if (entry.Entity is RelicListing listing)
                    {
                        // Для RelicListing: обновляем RowVersion и оригинальные значения
                        entry.OriginalValues.SetValues(databaseValues);
                        // Обновляем RowVersion из базы для следующей попытки
                        listing.RowVersion = databaseValues.GetValue<uint>(nameof(RelicListing.RowVersion));
                    }
                    else
                    {
                        // Для других сущностей принимаем значения из базы
                        entry.OriginalValues.SetValues(databaseValues);
                    }
                }
                
                // Небольшая задержка перед повторной попыткой
                await Task.Delay(50 * attempt, cancellationToken);
            }
        }

        // Задача 6: Пакетная отправка уведомлений в очередь
        if (newListings.Count > 0)
        {
            await _notificationQueue.EnqueueAsync(newListings, cancellationToken);
        }

        // Задача 7: Итоговое логирование
        _logger.LogInformation(
            "[ParseRelic] Processed {Total} lots. Created: {Created}, Updated: {Updated}, Skipped: {Skipped}",
            packet.lots.Count, createdCount, updatedCount, skippedCount);

        if (skippedRelicIds.Count > 0)
        {
            _logger.LogWarning(
                "[ParseRelic] Skipped lots due to missing definitions: {Ids}",
                string.Join(", ", skippedRelicIds.Distinct().Take(10)));
        }

        return new ParseRelicResult(createdCount, updatedCount,
            $"Successfully processed {packet.lots.Count} lots.");
    }

    private int GetAbsorbExperience(Relic lotRelicItem, RelicDefinition relicDefinition)
    {
        var baseExp = 0;
        switch (relicDefinition.SoulLevel)
        {
            case 1: baseExp = 500; break;
            case 2: baseExp = 2000; break;
            case 3: baseExp = 5000; break;
            case 4: baseExp = 15000; break;
            case 5: baseExp = 30000; break;
        }

        if (lotRelicItem.exp == 0)
            return baseExp;

        return (int)(lotRelicItem.exp * 0.7) + baseExp;
    }

    /// <summary>
    /// Задача 2: Кэширование ServerDefinitions (TTL 5 мин)
    /// </summary>
    private async Task<ServerDefinition?> GetServerAsync(string serverKey, CancellationToken ct)
    {
        var cacheKey = $"server:{serverKey.ToLower()}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
            // Задача 5: AsNoTracking для read-only запросов
            return await _dbContext.ServerDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Key.ToLower() == serverKey.ToLower(), ct);
        });
    }

    /// <summary>
    /// Задача 2: Кэширование RelicDefinitions (TTL 10 мин)
    /// </summary>
    private async Task<Dictionary<int, RelicDefinition>> GetRelicDefinitionsAsync(
        List<int> relicIds, CancellationToken ct)
    {
        var result = new Dictionary<int, RelicDefinition>();
        var missingIds = new List<int>();

        foreach (var id in relicIds.Distinct())
        {
            var cacheKey = $"relic_def:{id}";
            if (_cache.TryGetValue(cacheKey, out RelicDefinition? cached) && cached != null)
            {
                result[id] = cached;
            }
            else
            {
                missingIds.Add(id);
            }
        }

        if (missingIds.Count > 0)
        {
            // Задача 5: AsNoTracking для read-only запросов
            var fromDb = await _dbContext.RelicDefinitions
                .Where(rd => missingIds.Contains(rd.Id))
                .AsNoTracking()
                .ToListAsync(ct);

            foreach (var def in fromDb)
            {
                _cache.Set($"relic_def:{def.Id}", def, TimeSpan.FromMinutes(120));
                result[def.Id] = def;
            }
        }

        return result;
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
                AttributeDefinitionId = AddonMapping.GetRelicAttributeType(relic.main_addon) ?? 0,
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
                AttributeDefinitionId = AddonMapping.GetRelicAttributeType(addon.id) ?? 0,
                Value = addon.value,
                Category = AttributeCategory.Additional
            });
        }

        return attributes;
    }
}