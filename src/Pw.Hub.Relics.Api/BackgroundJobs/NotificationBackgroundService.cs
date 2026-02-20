namespace Pw.Hub.Relics.Api.BackgroundJobs;

/// <summary>
/// Фоновый сервис для обработки уведомлений с контролируемым параллелизмом
/// </summary>
public class NotificationBackgroundService : BackgroundService
{
    private readonly INotificationQueue _queue;
    private readonly INotificationProcessor _processor;
    private readonly ILogger<NotificationBackgroundService> _logger;
    private readonly SemaphoreSlim _semaphore = new(5); // Максимум 5 параллельных задач

    public NotificationBackgroundService(
        INotificationQueue queue,
        INotificationProcessor processor,
        ILogger<NotificationBackgroundService> logger)
    {
        _queue = queue;
        _processor = processor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification background service started");

        var tasks = new List<Task>();

        try
        {
            await foreach (var listing in _queue.DequeueAllAsync(stoppingToken))
            {
                await _semaphore.WaitAsync(stoppingToken);

                var task = ProcessWithSemaphoreAsync(listing, stoppingToken);
                tasks.Add(task);

                // Очистка завершённых задач
                tasks.RemoveAll(t => t.IsCompleted);
            }

            // Дождаться завершения всех оставшихся задач
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Notification background service is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in notification background service");
        }

        _logger.LogInformation("Notification background service stopped");
    }

    private async Task ProcessWithSemaphoreAsync(Domain.Entities.RelicListing listing, CancellationToken ct)
    {
        try
        {
            await _processor.ProcessNewListingAsync(listing, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notification for listing {Id}", listing.Id);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
