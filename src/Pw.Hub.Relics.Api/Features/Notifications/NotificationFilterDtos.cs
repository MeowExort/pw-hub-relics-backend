namespace Pw.Hub.Relics.Api.Features.Notifications;

/// <summary>
/// DTO фильтра уведомлений
/// </summary>
public record NotificationFilterDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public bool IsEnabled { get; init; }
    public long TelegramChatId { get; init; }
    public required FilterCriteriaDto Criteria { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Критерии фильтрации
/// </summary>
public record FilterCriteriaDto
{
    public int? SoulType { get; init; }
    public int? SlotTypeId { get; init; }
    public int? Race { get; init; }
    public int? SoulLevel { get; init; }
    public int? MainAttributeId { get; init; }
    public List<int>? RequiredAdditionalAttributeIds { get; init; }
    public long? MinPrice { get; init; }
    public long? MaxPrice { get; init; }
    public int? ServerId { get; init; }
}

/// <summary>
/// Запрос на создание фильтра
/// </summary>
public record CreateFilterRequest
{
    public required string Name { get; init; }
    public long TelegramChatId { get; init; }
    public required FilterCriteriaDto Criteria { get; init; }
}

/// <summary>
/// Запрос на обновление фильтра
/// </summary>
public record UpdateFilterRequest
{
    public required string Name { get; init; }
    public long TelegramChatId { get; init; }
    public required FilterCriteriaDto Criteria { get; init; }
}

/// <summary>
/// Запрос на переключение фильтра
/// </summary>
public record ToggleFilterRequest
{
    public bool IsEnabled { get; init; }
}
