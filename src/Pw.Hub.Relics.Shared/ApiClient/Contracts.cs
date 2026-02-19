namespace Pw.Hub.Relics.Shared.ApiClient;

/// <summary>
/// Запрос на парсинг бинарных данных реликвий
/// </summary>
/// <param name="Server">Ключ сервера (например, "alcor", "capella")</param>
/// <param name="Data">Бинарные данные пакета GetRelicDetail_Re</param>
public record ParseRelicRequest(string Server, byte[] Data);

/// <summary>
/// Результат парсинга реликвий
/// </summary>
/// <param name="CreatedCount">Количество созданных новых лотов</param>
/// <param name="UpdatedCount">Количество обновлённых существующих лотов</param>
/// <param name="Message">Сообщение о результате операции</param>
public record ParseRelicResult(int CreatedCount, int UpdatedCount, string Message);

/// <summary>
/// Настройки для Client Credentials Flow
/// </summary>
public class ClientCredentialsOptions
{
    /// <summary>
    /// Адрес сервера авторизации (Identity Provider)
    /// </summary>
    public string Authority { get; set; } = string.Empty;
    
    /// <summary>
    /// Идентификатор клиента
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
    
    /// <summary>
    /// Секретный ключ клиента
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// Запрашиваемые области доступа (scopes), разделённые пробелами
    /// </summary>
    public string Scope { get; set; } = string.Empty;
}