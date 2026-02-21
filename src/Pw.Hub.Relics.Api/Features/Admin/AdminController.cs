using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Pw.Hub.Relics.Api.Helpers;

namespace Pw.Hub.Relics.Api.Features.Admin;

/// <summary>
/// Контроллер для административных операций
/// </summary>
[ApiController]
[Route("api/admin")]
[ApiKeyAuth]
public class AdminController : ControllerBase
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IMemoryCache cache, ILogger<AdminController> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Инвалидация кэша
    /// </summary>
    /// <remarks>
    /// Очищает весь кэш приложения (ServerDefinitions, RelicDefinitions).
    /// Требуется scope: relics:write
    /// </remarks>
    [HttpPost("cache/invalidate")]
    public IActionResult InvalidateCache()
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0); // Очистить весь кэш
            _logger.LogInformation("[Admin] Cache invalidated successfully");
            return Ok(new { Message = "Cache invalidated successfully" });
        }

        _logger.LogWarning("[Admin] Cache invalidation failed - cache is not MemoryCache");
        return BadRequest(new { Message = "Cache invalidation not supported" });
    }
}
