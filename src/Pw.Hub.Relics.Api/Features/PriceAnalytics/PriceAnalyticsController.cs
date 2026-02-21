using MediatR;
using Microsoft.AspNetCore.Mvc;
using Pw.Hub.Relics.Api.Features.PriceAnalytics.GetPriceTrends;
using Pw.Hub.Relics.Api.Helpers;

namespace Pw.Hub.Relics.Api.Features.PriceAnalytics;

[ApiController]
[Route("api/analytics")]
[ApiKeyAuth]
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
