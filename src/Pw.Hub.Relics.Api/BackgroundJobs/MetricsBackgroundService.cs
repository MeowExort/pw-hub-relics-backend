using Microsoft.EntityFrameworkCore;
using Pw.Hub.Relics.Infrastructure.Data;
using Prometheus;

namespace Pw.Hub.Relics.Api.BackgroundJobs;

/// <summary>
/// Background job для сбора статистики и обновления метрик Prometheus
/// </summary>
public class MetricsBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MetricsBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    private static readonly Gauge TotalListings = Metrics
        .CreateGauge("relic_listings_total", "Total number of relic listings in the database");

    private static readonly Gauge ActiveListings = Metrics
        .CreateGauge("relic_listings_active_total", "Number of active relic listings in the database");

    public MetricsBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<MetricsBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MetricsBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateMetricsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating metrics");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("MetricsBackgroundService stopped");
    }

    private async Task UpdateMetricsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RelicsDbContext>();

        var total = await dbContext.RelicListings.CountAsync(cancellationToken);
        var active = await dbContext.RelicListings.CountAsync(r => r.IsActive, cancellationToken);

        TotalListings.Set(total);
        ActiveListings.Set(active);

        _logger.LogDebug("Metrics updated: Total={Total}, Active={Active}", total, active);
    }
}
