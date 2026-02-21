using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pw.Hub.Relics.Api.Helpers;
using Pw.Hub.Relics.Infrastructure.Data;

namespace Pw.Hub.Relics.Api.Features.Dictionaries;

[ApiController]
[Route("api/dictionaries")]
[ApiKeyAuth]
public class DictionariesController : ControllerBase
{
    private readonly RelicsDbContext _dbContext;

    public DictionariesController(RelicsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Получить список серверов
    /// </summary>
    [HttpGet("servers")]
    public async Task<IActionResult> GetServers(CancellationToken cancellationToken)
    {
        var servers = await _dbContext.ServerDefinitions
            .Select(s => new ServerDto(s.Id, s.Name, s.Key))
            .ToListAsync(cancellationToken);

        return Ok(new { servers });
    }

    /// <summary>
    /// Получить список типов слотов
    /// </summary>
    [HttpGet("slot-types")]
    public async Task<IActionResult> GetSlotTypes(CancellationToken cancellationToken)
    {
        var slotTypes = await _dbContext.SlotTypes
            .Select(s => new SlotTypeDto(s.Id, s.Name))
            .ToListAsync(cancellationToken);

        return Ok(new { slotTypes });
    }

    /// <summary>
    /// Получить список характеристик
    /// </summary>
    [HttpGet("attributes")]
    public async Task<IActionResult> GetAttributes(CancellationToken cancellationToken)
    {
        var attributes = await _dbContext.AttributeDefinitions
            .Select(a => new AttributeDto(a.Id, a.Name))
            .ToListAsync(cancellationToken);

        return Ok(new { attributes });
    }

    /// <summary>
    /// Получить список реликвий
    /// </summary>
    [HttpGet("relic-definitions")]
    public async Task<IActionResult> GetRelicDefinitions(CancellationToken cancellationToken)
    {
        var relics = await _dbContext.RelicDefinitions
            .Include(r => r.SlotType)
            .Select(r => new RelicDefinitionDto(
                r.Id,
                r.Name,
                r.SoulLevel,
                (int)r.SoulType,
                new SlotTypeDto(r.SlotType.Id, r.SlotType.Name),
                (int)r.Race,
                r.IconUri))
            .ToListAsync(cancellationToken);

        return Ok(new { relicDefinitions = relics });
    }

    /// <summary>
    /// Получить кривую опыта для заточки
    /// </summary>
    [HttpGet("enhancement-curve")]
    public async Task<IActionResult> GetEnhancementCurve(CancellationToken cancellationToken)
    {
        var curve = await _dbContext.EnhancementCurves
            .OrderBy(e => e.Level)
            .Select(e => new EnhancementCurveDto(e.Level, e.RequiredExperience))
            .ToListAsync(cancellationToken);

        return Ok(new { enhancementCurve = curve });
    }
}

// DTOs
public record ServerDto(int Id, string Name, string Key);
public record SlotTypeDto(int Id, string Name);
public record AttributeDto(int Id, string Name);
public record RelicDefinitionDto(int Id, string Name, int SoulLevel, int SoulType, SlotTypeDto SlotType, int Race, string? IconUri);
public record EnhancementCurveDto(int Level, int RequiredExperience);
