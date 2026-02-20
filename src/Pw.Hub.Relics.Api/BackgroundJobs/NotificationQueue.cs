using System.Threading.Channels;
using Pw.Hub.Relics.Domain.Entities;

namespace Pw.Hub.Relics.Api.BackgroundJobs;

/// <summary>
/// Интерфейс очереди уведомлений для пакетной обработки
/// </summary>
public interface INotificationQueue
{
    /// <summary>
    /// Добавить листинги в очередь на обработку уведомлений
    /// </summary>
    ValueTask EnqueueAsync(IEnumerable<RelicListing> listings, CancellationToken ct = default);
    
    /// <summary>
    /// Получить все листинги из очереди (для фонового сервиса)
    /// </summary>
    IAsyncEnumerable<RelicListing> DequeueAllAsync(CancellationToken ct);
}

/// <summary>
/// Реализация очереди уведомлений на базе Channel
/// </summary>
public class NotificationQueue : INotificationQueue
{
    private readonly Channel<RelicListing> _channel;

    public NotificationQueue()
    {
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<RelicListing>(options);
    }

    public async ValueTask EnqueueAsync(IEnumerable<RelicListing> listings, CancellationToken ct = default)
    {
        foreach (var listing in listings)
        {
            await _channel.Writer.WriteAsync(listing, ct);
        }
    }

    public async IAsyncEnumerable<RelicListing> DequeueAllAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var listing in _channel.Reader.ReadAllAsync(ct))
        {
            yield return listing;
        }
    }
}
