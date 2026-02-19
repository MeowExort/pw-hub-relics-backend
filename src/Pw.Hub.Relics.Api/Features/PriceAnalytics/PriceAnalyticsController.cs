using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pw.Hub.Relics.Api.Features.PriceAnalytics.GetPriceTrends;

namespace Pw.Hub.Relics.Api.Features.PriceAnalytics;

[ApiController]
[Route("api/analytics")]
[Authorize(Policy = "UserPolicy")]
public class PriceAnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PriceAnalyticsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Получить тенденции цен
    /// </summary>
    /// <remarks>
    /// mainAttributeId - необязательный параметр.
    /// Период ограничен максимум 1 месяцем.
    /// Требуется scope: relics:read
    /// </remarks>
    [HttpGet("price-trends")]
    public async Task<IActionResult> GetPriceTrends(
        [FromQuery] GetPriceTrendsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
