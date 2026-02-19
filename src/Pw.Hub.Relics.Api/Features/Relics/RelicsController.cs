using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pw.Hub.Relics.Api.Features.Relics.CreateRelic;
using Pw.Hub.Relics.Api.Features.Relics.GetRelicById;
using Pw.Hub.Relics.Api.Features.Relics.SearchRelics;

namespace Pw.Hub.Relics.Api.Features.Relics;

[ApiController]
[Route("api/relics")]
public class RelicsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RelicsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Создание/обновление лота реликвии (Bot API)
    /// </summary>
    /// <remarks>
    /// Бот НЕ знает relicDefinitionId - поиск по soulLevel+soulType+slotTypeId+race.
    /// Требуется scope: relics:write
    /// </remarks>
    [HttpPost]
    [Authorize(Policy = "BotPolicy")]
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
    /// <remarks>
    /// Требуется scope: relics:read
    /// </remarks>
    [HttpGet("search")]
    [Authorize(Policy = "UserPolicy")]
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
    /// <remarks>
    /// Требуется scope: relics:read
    /// </remarks>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "UserPolicy")]
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
}
