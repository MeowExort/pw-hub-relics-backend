namespace Pw.Hub.Relics.Domain.Entities;

/// <summary>
/// Справочник серверов
/// </summary>
public class ServerDefinition
{
    public int Id { get; set; }
    
    /// <summary>
    /// Название на русском (Центавр, Алькор, Мицар, Капелла)
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Ключ (centaur, alkor, mizar, capella)
    /// </summary>
    public string Key { get; set; } = string.Empty;
}
