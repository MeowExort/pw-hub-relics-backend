using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pw.Hub.Relics.Domain.Entities;
using Pw.Hub.Relics.Domain.Enums;
using Pw.Hub.Relics.Infrastructure.Data;

namespace Pw.Hub.Relics.Api.Features.Notifications;

[ApiController]
[Route("api/notifications/filters")]
[Authorize(Policy = "UserPolicy")]
public class NotificationsController : ControllerBase
{
    private readonly RelicsDbContext _dbContext;

    public NotificationsController(RelicsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Получить userId из JWT токена
    /// </summary>
    private string GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                     ?? User.FindFirstValue("sub");
        
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        
        return userId;
    }

    /// <summary>
    /// Получить список фильтров пользователя
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetFilters(CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var filters = await _dbContext.NotificationFilters
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);

        var result = filters.Select(MapToDto).ToList();

        return Ok(new { filters = result });
    }

    /// <summary>
    /// Создать фильтр уведомлений
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateFilter(
        [FromBody] CreateFilterRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var filter = new NotificationFilter
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            IsEnabled = true,
            SoulType = request.Criteria.SoulType.HasValue ? (SoulType)request.Criteria.SoulType.Value : null,
            SlotTypeId = request.Criteria.SlotTypeId,
            Race = request.Criteria.Race.HasValue ? (Race)request.Criteria.Race.Value : null,
            SoulLevel = request.Criteria.SoulLevel,
            MainAttributeId = request.Criteria.MainAttributeId,
            RequiredAdditionalAttributeIds = request.Criteria.RequiredAdditionalAttributeIds ?? new List<int>(),
            MinPrice = request.Criteria.MinPrice,
            MaxPrice = request.Criteria.MaxPrice,
            ServerId = request.Criteria.ServerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.NotificationFilters.Add(filter);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetFilterById), new { id = filter.Id }, MapToDto(filter));
    }

    /// <summary>
    /// Получить фильтр по ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetFilterById(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var filter = await _dbContext.NotificationFilters
            .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId, cancellationToken);

        if (filter == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(filter));
    }

    /// <summary>
    /// Обновить фильтр
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateFilter(
        Guid id,
        [FromBody] UpdateFilterRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var filter = await _dbContext.NotificationFilters
            .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId, cancellationToken);

        if (filter == null)
        {
            return NotFound();
        }

        filter.Name = request.Name;
        filter.SoulType = request.Criteria.SoulType.HasValue ? (SoulType)request.Criteria.SoulType.Value : null;
        filter.SlotTypeId = request.Criteria.SlotTypeId;
        filter.Race = request.Criteria.Race.HasValue ? (Race)request.Criteria.Race.Value : null;
        filter.SoulLevel = request.Criteria.SoulLevel;
        filter.MainAttributeId = request.Criteria.MainAttributeId;
        filter.RequiredAdditionalAttributeIds = request.Criteria.RequiredAdditionalAttributeIds ?? new List<int>();
        filter.MinPrice = request.Criteria.MinPrice;
        filter.MaxPrice = request.Criteria.MaxPrice;
        filter.ServerId = request.Criteria.ServerId;
        filter.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(filter));
    }

    /// <summary>
    /// Удалить фильтр
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteFilter(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var filter = await _dbContext.NotificationFilters
            .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId, cancellationToken);

        if (filter == null)
        {
            return NotFound();
        }

        _dbContext.NotificationFilters.Remove(filter);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Включить/выключить фильтр
    /// </summary>
    [HttpPatch("{id:guid}/toggle")]
    public async Task<IActionResult> ToggleFilter(
        Guid id,
        [FromBody] ToggleFilterRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var filter = await _dbContext.NotificationFilters
            .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId, cancellationToken);

        if (filter == null)
        {
            return NotFound();
        }

        filter.IsEnabled = request.IsEnabled;
        filter.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(filter));
    }

    private static NotificationFilterDto MapToDto(NotificationFilter filter)
    {
        return new NotificationFilterDto
        {
            Id = filter.Id,
            Name = filter.Name,
            IsEnabled = filter.IsEnabled,
            Criteria = new FilterCriteriaDto
            {
                SoulType = filter.SoulType.HasValue ? (int)filter.SoulType.Value : null,
                SlotTypeId = filter.SlotTypeId,
                Race = filter.Race.HasValue ? (int)filter.Race.Value : null,
                SoulLevel = filter.SoulLevel,
                MainAttributeId = filter.MainAttributeId,
                RequiredAdditionalAttributeIds = filter.RequiredAdditionalAttributeIds,
                MinPrice = filter.MinPrice,
                MaxPrice = filter.MaxPrice,
                ServerId = filter.ServerId
            },
            CreatedAt = filter.CreatedAt,
            UpdatedAt = filter.UpdatedAt
        };
    }
}
