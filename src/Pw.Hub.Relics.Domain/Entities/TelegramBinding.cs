using Pw.Hub.Relics.Domain.Enums;

namespace Pw.Hub.Relics.Domain.Entities;

/// <summary>
/// Привязка Telegram аккаунта к пользователю системы
/// </summary>
public class TelegramBinding
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// ID пользователя в системе (из JWT токена)
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// ID чата в Telegram (заполняется после подтверждения привязки)
    /// </summary>
    public long? TelegramChatId { get; set; }
    
    /// <summary>
    /// Username пользователя в Telegram (опционально)
    /// </summary>
    public string? TelegramUsername { get; set; }
    
    /// <summary>
    /// Токен для deeplink привязки
    /// </summary>
    public string LinkToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Время истечения токена привязки
    /// </summary>
    public DateTime TokenExpiresAt { get; set; }
    
    /// <summary>
    /// Подтверждена ли привязка (пользователь перешел по ссылке и нажал Start)
    /// </summary>
    public bool IsConfirmed { get; set; }
    
    /// <summary>
    /// Дата создания записи
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Дата последнего обновления
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    // ===== Настройки уведомлений =====
    
    /// <summary>
    /// Частота отправки уведомлений
    /// </summary>
    public NotificationFrequency NotificationFrequency { get; set; } = NotificationFrequency.Instant;
    
    /// <summary>
    /// Включены ли "тихие часы"
    /// </summary>
    public bool QuietHoursEnabled { get; set; } = false;
    
    /// <summary>
    /// Начало "тихих часов" (время в формате HH:mm, UTC)
    /// </summary>
    public TimeOnly? QuietHoursStart { get; set; }
    
    /// <summary>
    /// Конец "тихих часов" (время в формате HH:mm, UTC)
    /// </summary>
    public TimeOnly? QuietHoursEnd { get; set; }
}
