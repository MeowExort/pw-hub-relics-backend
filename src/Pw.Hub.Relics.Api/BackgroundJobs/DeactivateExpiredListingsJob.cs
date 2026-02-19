using Microsoft.EntityFrameworkCore;
using Pw.Hub.Relics.Infrastructure.Data;

namespace Pw.Hub.Relics.Api.BackgroundJobs;

/// <summary>
/// Фоновая задача для деактивации устаревших лотов.
/// Каждые 5 минут деактивирует лоты, не обновлявшиеся более 10 минут.
/// </summary>
public class DeactivateExpiredListingsJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DeactivateExpiredListingsJob> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _expirationThreshold = TimeSpan.FromMinutes(10);

    public DeactivateExpiredListingsJob(
        IServiceProvider serviceProvider,
        ILogger<DeactivateExpiredListingsJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DeactivateExpiredListingsJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DeactivateExpiredListings(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deactivating expired listings");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("DeactivateExpiredListingsJob stopped");
    }

    private async Task DeactivateExpiredListings(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RelicsDbContext>();

        var threshold = DateTime.UtcNow - _expirationThreshold;

        var deactivatedCount = await dbContext.RelicListings
            .Where(r => r.IsActive && r.LastSeenAt < threshold)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.IsActive, false)
                .SetProperty(r => r.SoldAt, DateTime.UtcNow),
                cancellationToken);

        if (deactivatedCount > 0)
        {
            _logger.LogInformation("Deactivated {Count} expired listings", deactivatedCount);
        }
    }
}
