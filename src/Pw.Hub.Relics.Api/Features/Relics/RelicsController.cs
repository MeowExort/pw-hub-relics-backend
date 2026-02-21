using MediatR;
using Microsoft.AspNetCore.Mvc;
using Pw.Hub.Relics.Api.BackgroundJobs;
using Pw.Hub.Relics.Api.Features.Relics.CreateRelic;
using Pw.Hub.Relics.Api.Features.Relics.GetRelicById;
using Pw.Hub.Relics.Api.Features.Relics.ParseRelic;
using Pw.Hub.Relics.Api.Features.Relics.SearchRelics;
using Pw.Hub.Relics.Api.Helpers;

namespace Pw.Hub.Relics.Api.Features.Relics;

[ApiController]
[Route("api/relics")]
[ApiKeyAuth]
public class RelicsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IParseRelicQueue _parseRelicQueue;

    public RelicsController(IMediator mediator, IParseRelicQueue parseRelicQueue)
    {
        _mediator = mediator;
        _parseRelicQueue = parseRelicQueue;
    }

    /// <summary>
    /// Создание/обновление лота реликвии (Bot API)
    /// </summary>
    /// <remarks>
    /// Бот НЕ знает relicDefinitionId - поиск по soulLevel+soulType+slotTypeId+race.
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> CreateRelic(
        [FromBody] CreateRelicCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        
        if (result.IsUpdated)
        {
            return Ok(result);
        }
        
        return CreatedAtAction(nameof(GetRelicById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Поиск реликвий с фильтрацией
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchRelics(
        [FromQuery] SearchRelicsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Получить реликвию по ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetRelicById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRelicByIdQuery(id), cancellationToken);
        
        if (result == null)
        {
            return NotFound();
        }
        
        return Ok(result);
    }

    /// <summary>
    /// Парсинг бинарных данных лотов реликвий (Bot API)
    /// </summary>
    /// <remarks>
    /// Принимает бинарные данные пакета GetRelicDetail_Re и сохраняет лоты в БД.
    /// </remarks>
    [HttpPost("parse")]
    [SkipApiKeyAuth]
    public async Task<IActionResult> ParseRelic(
        [FromBody] ParseRelicCommand command,
        CancellationToken cancellationToken)
    {
        await _parseRelicQueue.EnqueueAsync(command, cancellationToken);
        return Accepted(new { message = "Data enqueued for processing" });
    }
}
