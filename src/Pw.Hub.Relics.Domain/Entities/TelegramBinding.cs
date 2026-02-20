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
}
