using Microsoft.EntityFrameworkCore;
using Pw.Hub.Relics.Domain.Entities;
using Pw.Hub.Relics.Domain.Enums;
using Pw.Hub.Relics.Infrastructure.Data;

namespace Pw.Hub.Relics.Api.BackgroundJobs;

/// <summary>
/// Background job для обновления истории цен каждые 15 минут
/// </summary>
public class RefreshPriceHistoryJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RefreshPriceHistoryJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(15);

    public RefreshPriceHistoryJob(
        IServiceProvider serviceProvider,
        ILogger<RefreshPriceHistoryJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RefreshPriceHistoryJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshPriceHistoryAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing price history");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("RefreshPriceHistoryJob stopped");
    }

    private async Task RefreshPriceHistoryAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RelicsDbContext>();

        var threeMonthAgo = DateTime.UtcNow.AddMonths(-3);

        _logger.LogInformation("Refreshing price history data...");

        // Получить все активные лоты за последние 3 месяца с их атрибутами
        var listings = await dbContext.RelicListings
            .Include(r => r.Attributes)
            .Where(r => r.CreatedAt >= threeMonthAgo)
            .ToListAsync(cancellationToken);

        // Удалить старые записи истории цен
        var oldHistoryCount = await dbContext.PriceHistories
            .Where(p => p.Timestamp < threeMonthAgo)
            .ExecuteDeleteAsync(cancellationToken);

        if (oldHistoryCount > 0)
        {
            _logger.LogInformation("Deleted {Count} old price history records", oldHistoryCount);
        }

        // Получить существующие записи истории для избежания дубликатов
        var existingIds = await dbContext.PriceHistories
            .Select(p => p.Id)
            .ToHashSetAsync(cancellationToken);

        var newRecords = new List<PriceHistory>();

        foreach (var listing in listings)
        {
            var mainAttr = listing.Attributes.FirstOrDefault(a => a.Category == AttributeCategory.Main);
            if (mainAttr == null) continue;

            var additionalAttrIds = listing.Attributes
                .Where(a => a.Category == AttributeCategory.Additional)
                .Select(a => a.AttributeDefinitionId)
                .OrderBy(id => id)
                .ToList();

            // Создаем детерминированный ID на основе данных лота
            var historyId = GenerateHistoryId(listing.Id, listing.CreatedAt);

            if (!existingIds.Contains(historyId))
            {
                newRecords.Add(new PriceHistory
                {
                    Id = historyId,
                    RelicDefinitionId = listing.RelicDefinitionId,
                    MainAttributeId = mainAttr.AttributeDefinitionId,
                    AdditionalAttributeIds = additionalAttrIds,
                    Price = listing.Price,
                    ServerId = listing.ServerId,
                    Timestamp = listing.CreatedAt
                });
            }
        }

        if (newRecords.Count > 0)
        {
            await dbContext.PriceHistories.AddRangeAsync(newRecords, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Added {Count} new price history records", newRecords.Count);
        }
        else
        {
            _logger.LogDebug("No new price history records to add");
        }
    }

    /// <summary>
    /// Генерирует детерминированный GUID на основе ID лота и времени создания
    /// </summary>
    private static Guid GenerateHistoryId(Guid listingId, DateTime createdAt)
    {
        var bytes = new byte[16];
        var listingBytes = listingId.ToByteArray();
        var timeBytes = BitConverter.GetBytes(createdAt.Ticks);
        
        Array.Copy(listingBytes, 0, bytes, 0, 8);
        Array.Copy(timeBytes, 0, bytes, 8, 8);
        
        return new Guid(bytes);
    }
}
