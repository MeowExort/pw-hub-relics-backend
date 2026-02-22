namespace Pw.Hub.Relics.Domain.Enums;

/// <summary>
/// Частота отправки уведомлений в Telegram
/// </summary>
public enum NotificationFrequency
{
    /// <summary>
    /// Мгновенные уведомления (сразу при срабатывании)
    /// </summary>
    Instant = 0,
    
    /// <summary>
    /// Раз в час (дайджест за час)
    /// </summary>
    Hourly = 1,
    
    /// <summary>
    /// Раз в день (ежедневный дайджест)
    /// </summary>
    Daily = 2
}
