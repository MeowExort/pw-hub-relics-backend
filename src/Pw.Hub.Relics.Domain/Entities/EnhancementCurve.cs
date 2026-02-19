namespace Pw.Hub.Relics.Domain.Entities;

/// <summary>
/// Кривая опыта для заточки реликвий
/// </summary>
public class EnhancementCurve
{
    /// <summary>
    /// Уровень заточки
    /// </summary>
    public int Level { get; set; }
    
    /// <summary>
    /// Требуемый опыт для достижения этого уровня
    /// </summary>
    public int RequiredExperience { get; set; }
}
