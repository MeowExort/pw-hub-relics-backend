using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Pw.Hub.Relics.Api.Features.Relics.ParseRelic;
using Pw.Hub.Relics.Infrastructure.Data;
using Pw.Hub.Relics.Shared.Helpers;
using Pw.Hub.Relics.Api.Helpers;
using Pw.Hub.Relics.Shared.Packets;
using Pw.Hub.Relics.Shared.Packets.IO;
using Pw.Hub.Relics.Domain.Entities;
using Pw.Hub.Relics.Domain.Enums;

namespace Pw.Hub.Relics.Api.BackgroundJobs;

/// <summary>
/// Фоновый сервис для пакетной обработки парсинга реликвий с использованием PostgreSQL UPSERT
/// </summary>
public class ParseRelicBackgroundService : BackgroundService
{
    private readonly IParseRelicQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ParseRelicBackgroundService> _logger;

    public ParseRelicBackgroundService(
        IParseRelicQueue queue,
        IServiceProvider serviceProvider,
        ILogger<ParseRelicBackgroundService> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ParseRelic background service started");

        try
        {
            // Обработка пачками по 100 элементов или раз в 500мс
            const int batchSize = 100;
            var batch = new List<ParseRelicCommand>();

            while (!stoppingToken.IsCancellationRequested)
            {
                var waitToReadTask = _queue.DequeueAllAsync(stoppingToken).GetAsyncEnumerator();

                try
                {
                    while (batch.Count < batchSize)
                    {
                        if (await waitToReadTask.MoveNextAsync())
                        {
                            batch.Add(waitToReadTask.Current);
                        }
                        else
                        {
                            break;
                        }

                        if (batch.Count >= batchSize) break;
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                if (batch.Count > 0)
                {
                    await ProcessBatchWithUpsertAsync(batch, stoppingToken);
                    batch.Clear();
                }
                else
                {
                    await Task.Delay(100, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("ParseRelic background service is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ParseRelic background service");
        }

        _logger.LogInformation("ParseRelic background service stopped");
    }

    private async Task ProcessBatchWithUpsertAsync(List<ParseRelicCommand> batch, CancellationToken ct)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        RelicMetrics.BatchSize.Observe(batch.Count);
        RelicMetrics.BatchesProcessedTotal.Inc();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RelicsDbContext>();

            _logger.LogInformation("Processing batch of {Count} parse commands using UPSERT", batch.Count);

            var allListings = new List<RelicListing>();
            
            // 1. Предварительная обработка всех команд в объекты RelicListing
            foreach (var command in batch)
            {
                var listings = await ExtractListingsFromCommandAsync(command, dbContext, ct);
                allListings.AddRange(listings);
            }

            if (allListings.Count == 0) return;

            // Группируем по уникальному ключу, чтобы избежать конфликтов в одном запросе
            var uniqueListings = allListings
                .GroupBy(l => new { l.SellerCharacterId, l.ShopPosition, l.ServerId, l.RelicDefinitionId })
                .Select(g => g.First())
                .ToList();

            const int sqlBatchSize = 100;
            for (int i = 0; i < uniqueListings.Count; i += sqlBatchSize)
            {
                var currentBatch = uniqueListings.Skip(i).Take(sqlBatchSize).ToList();
                await ExecuteUpsertSqlAsync(dbContext, currentBatch, ct);
                
                // В данном случае мы не знаем точно сколько было создано, а сколько обновлено через ExecuteSqlRawAsync
                // Поэтому помечаем их как 'processed'
                RelicMetrics.RelicsProcessedTotal.WithLabels("processed").Inc(currentBatch.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error processing batch of parse commands with UPSERT");
            RelicMetrics.RelicsProcessedTotal.WithLabels("failed").Inc(batch.Count);
        }
        finally
        {
            stopwatch.Stop();
            RelicMetrics.BatchProcessingDuration.Observe(stopwatch.Elapsed.TotalSeconds);
        }
    }

    private async Task<List<RelicListing>> ExtractListingsFromCommandAsync(ParseRelicCommand command, RelicsDbContext dbContext, CancellationToken ct)
    {
        var result = new List<RelicListing>();
        
        using var packetStream = new PacketStream(command.Data);
        var packet = new GetRelicDetail_Re();
        packet.Read(packetStream);

        if (packet.lots == null || packet.lots.Count == 0) return result;

        var server = await dbContext.ServerDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key.ToLower() == command.Server.ToLower(), ct);
        
        if (server == null) return result;

        var relicIds = packet.lots.Select(l => l.relic_item.id).Distinct().ToList();
        var definitions = await dbContext.RelicDefinitions
            .Where(rd => relicIds.Contains(rd.Id))
            .AsNoTracking()
            .ToDictionaryAsync(rd => rd.Id, ct);

        foreach (var lot in packet.lots)
        {
            if (!definitions.TryGetValue(lot.relic_item.id, out var relicDefinition)) continue;

            var incomingHash = AttributeHashHelper.ComputeHashFromRelic(lot.relic_item);
            var attributes = CreateAttributesDto(lot.relic_item, relicDefinition);

            result.Add(new RelicListing
            {
                Id = Guid.NewGuid(),
                RelicDefinitionId = relicDefinition.Id,
                AbsorbExperience = GetAbsorbExperience(lot.relic_item, relicDefinition),
                EnhancementLevel = RelicHelper.GetRelicRefineLevel(lot.relic_item.exp),
                SellerCharacterId = lot.sell_id.player_id,
                ShopPosition = lot.sell_id.pos_in_shop,
                Price = (long)(lot.price * 1.08),
                ServerId = server.Id,
                CreatedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
                IsActive = true,
                AttributesHash = incomingHash,
                JsonAttributes = attributes
            });
        }

        return result;
    }

    private async Task ExecuteUpsertSqlAsync(RelicsDbContext dbContext, List<RelicListing> listings, CancellationToken ct)
    {
        var sql = new StringBuilder();
        sql.AppendLine("INSERT INTO relic_listings (id, relic_definition_id, absorb_experience, enhancement_level, seller_character_id, shop_position, price, server_id, created_at, last_seen_at, is_active, attributes_hash, json_attributes)");
        sql.Append("VALUES ");

        var parameters = new List<object>();
        for (int i = 0; i < listings.Count; i++)
        {
            var l = listings[i];
            var pOffset = i * 13;
            sql.Append($"(@p{pOffset}, @p{pOffset+1}, @p{pOffset+2}, @p{pOffset+3}, @p{pOffset+4}, @p{pOffset+5}, @p{pOffset+6}, @p{pOffset+7}, @p{pOffset+8}, @p{pOffset+9}, @p{pOffset+10}, @p{pOffset+11}, @p{pOffset+12}::jsonb)");
            if (i < listings.Count - 1) sql.Append(", ");

            parameters.Add(l.Id);
            parameters.Add(l.RelicDefinitionId);
            parameters.Add(l.AbsorbExperience);
            parameters.Add(l.EnhancementLevel);
            parameters.Add(l.SellerCharacterId);
            parameters.Add(l.ShopPosition);
            parameters.Add(l.Price);
            parameters.Add(l.ServerId);
            parameters.Add(l.CreatedAt);
            parameters.Add(l.LastSeenAt);
            parameters.Add(l.IsActive);
            parameters.Add((object)l.AttributesHash ?? DBNull.Value);
            parameters.Add(JsonSerializer.Serialize(l.JsonAttributes));
        }

        sql.AppendLine();
        sql.AppendLine("ON CONFLICT (seller_character_id, shop_position, server_id, relic_definition_id)");
        sql.AppendLine("DO UPDATE SET ");
        sql.AppendLine("  absorb_experience = EXCLUDED.absorb_experience,");
        sql.AppendLine("  enhancement_level = EXCLUDED.enhancement_level,");
        sql.AppendLine("  price = EXCLUDED.price,");
        sql.AppendLine("  last_seen_at = EXCLUDED.last_seen_at,");
        sql.AppendLine("  is_active = EXCLUDED.is_active,");
        sql.AppendLine("  attributes_hash = EXCLUDED.attributes_hash,");
        sql.AppendLine("  json_attributes = EXCLUDED.json_attributes;");

        await dbContext.Database.ExecuteSqlRawAsync(sql.ToString(), parameters, ct);
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

        if (lotRelicItem.exp == 0) return baseExp;
        return (int)(lotRelicItem.exp * 0.7) + baseExp;
    }

    private List<RelicAttributeDto> CreateAttributesDto(Relic relic, RelicDefinition relicDefinition)
    {
        var attributes = new List<RelicAttributeDto>();
        if (relic.main_addon >= 0)
        {
            var attributeId = AddonMapping.GetRelicAttributeType(relic.main_addon);
            if (attributeId.HasValue)
            {
                attributes.Add(new RelicAttributeDto(
                    attributeId.Value,
                    RelicHelper.GetMainAddonValue(relic.main_addon, relic.exp, relicDefinition.SoulLevel, relicDefinition.MainAttributeScaling),
                    AttributeCategory.Main
                ));
            }
        }

        foreach (var addon in relic.addons)
        {
            if (addon.id == 0) continue;
            var attributeId = AddonMapping.GetRelicAttributeType(addon.id);
            if (attributeId.HasValue)
            {
                attributes.Add(new RelicAttributeDto(
                    attributeId.Value,
                    EquipmentAddonHelper.GetAddonValue(addon.id, addon.value),
                    AttributeCategory.Additional
                ));
            }
        }
        return attributes;
    }
}
