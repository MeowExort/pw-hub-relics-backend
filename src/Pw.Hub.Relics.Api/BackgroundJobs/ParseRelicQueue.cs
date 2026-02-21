using System.Threading.Channels;
using Pw.Hub.Relics.Api.Features.Relics.ParseRelic;
using Pw.Hub.Relics.Api.Helpers;

namespace Pw.Hub.Relics.Api.BackgroundJobs;

/// <summary>
/// Интерфейс очереди парсинга реликвий для пакетной обработки
/// </summary>
public interface IParseRelicQueue
{
    ValueTask EnqueueAsync(ParseRelicCommand command, CancellationToken ct = default);
    IAsyncEnumerable<ParseRelicCommand> DequeueAllAsync(CancellationToken ct);
}

/// <summary>
/// Реализация очереди парсинга на базе Channel
/// </summary>
public class ParseRelicQueue : IParseRelicQueue
{
    private readonly Channel<ParseRelicCommand> _channel;

    public ParseRelicQueue()
    {
        var options = new BoundedChannelOptions(2000)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<ParseRelicCommand>(options);
    }

    public async ValueTask EnqueueAsync(ParseRelicCommand command, CancellationToken ct = default)
    {
        await _channel.Writer.WriteAsync(command, ct);
        RelicMetrics.ParseQueueLength.Inc();
    }

    public async IAsyncEnumerable<ParseRelicCommand> DequeueAllAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var command in _channel.Reader.ReadAllAsync(ct))
        {
            RelicMetrics.ParseQueueLength.Dec();
            yield return command;
        }
    }
}
