using Microsoft.EntityFrameworkCore;
using Pw.Hub.Relics.Api.Helpers;
using Pw.Hub.Relics.Infrastructure.Data;

namespace Pw.Hub.Relics.Api.BackgroundJobs;

/// <summary>
/// Одноразовая задача для заполнения AttributesHash у существующих записей relic_listings.
/// Запускается при старте приложения и обрабатывает записи с AttributesHash = NULL.
/// </summary>
public class BackfillAttributesHashJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackfillAttributesHashJob> _logger;
    private const int BatchSize = 1000;

    public BackfillAttributesHashJob(
        IServiceProvider serviceProvider,
        ILogger<BackfillAttributesHashJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Небольшая задержка для завершения инициализации приложения
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        _logger.LogInformation("[BackfillAttributesHash] Starting backfill job...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RelicsDbContext>();

            var totalUpdated = 0;
            var hasMore = true;

            while (hasMore && !stoppingToken.IsCancellationRequested)
            {
                // Загружаем пачку записей без хеша
                var listings = await dbContext.RelicListings
                    .Include(rl => rl.Attributes)
                    .Where(rl => rl.AttributesHash == null)
                    .OrderBy(rl => rl.Id)
                    .Take(BatchSize)
                    .ToListAsync(stoppingToken);

                if (listings.Count == 0)
                {
                    hasMore = false;
                    continue;
                }

                foreach (var listing in listings)
                {
                    listing.AttributesHash = AttributeHashHelper.ComputeHashFromAttributes(listing.Attributes);
                }

                await dbContext.SaveChangesAsync(stoppingToken);
                totalUpdated += listings.Count;

                _logger.LogInformation(
                    "[BackfillAttributesHash] Processed batch of {Count} listings. Total updated: {Total}",
                    listings.Count, totalUpdated);

                // Если загрузили меньше BatchSize, значит это последняя пачка
                if (listings.Count < BatchSize)
                {
                    hasMore = false;
                }
            }

            _logger.LogInformation(
                "[BackfillAttributesHash] Backfill completed. Total records updated: {Total}",
                totalUpdated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BackfillAttributesHash] Error during backfill job");
        }
    }
}
