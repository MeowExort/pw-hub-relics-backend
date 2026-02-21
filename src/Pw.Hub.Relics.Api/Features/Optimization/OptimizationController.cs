using MediatR;
using Microsoft.AspNetCore.Mvc;
using Pw.Hub.Relics.Api.Features.Optimization.CalculateCheapestEnhancement;
using Pw.Hub.Relics.Api.Features.Optimization.CalculateMostProfitableQuest;
using Pw.Hub.Relics.Api.Helpers;

namespace Pw.Hub.Relics.Api.Features.Optimization;

[ApiController]
[Route("api/optimization")]
[ApiKeyAuth]
public class OptimizationController : ControllerBase
{
    private readonly IMediator _mediator;

    public OptimizationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Расчет самого дешевого способа заточки
    /// </summary>
    /// <remarks>
    /// Каждая реликвия - отдельный предмет (без quantity).
    /// Требуется scope: relics:read
    /// </remarks>
    [HttpPost("cheapest-enhancement")]
    public async Task<IActionResult> CalculateCheapestEnhancement(
        [FromBody] CalculateCheapestEnhancementCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Расчет самого выгодного квеста на реликвии
    /// </summary>
    /// <remarks>
    /// Механика: уровень 1 бесплатно, уровни 2-5 требуют реликвию предыдущего уровня того же типа души.
    /// Расчет: AVG(MIN по расам) для затрат и дохода.
    /// Требуется scope: relics:read
    /// </remarks>
    [HttpGet("most-profitable-quest")]
    public async Task<IActionResult> CalculateMostProfitableQuest(
        [FromQuery] int serverId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CalculateMostProfitableQuestQuery(serverId), cancellationToken);
        return Ok(result);
    }
}
