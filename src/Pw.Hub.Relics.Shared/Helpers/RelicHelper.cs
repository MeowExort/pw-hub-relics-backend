namespace Pw.Hub.Relics.Shared.Helpers;

/// <summary>
/// Вспомогательные методы для работы с реликвиями
/// </summary>
public static class RelicHelper
{
    /// <summary>
    /// Таблица опыта для уровней заточки реликвий
    /// </summary>
    private static readonly int[] RelicExpTable =
    [
        200, 275, 400, 625, 900, 1200, 1775, 2625, 3675, 5725,
        7450, 10150, 14125, 18075, 23530, 29270, 31050, 35100, 39025, 44825
    ];

    /// <summary>
    /// Этапы заточки в зависимости от редкости (SoulLevel)
    /// </summary>
    private static readonly int[] RelicRefineStages = [4, 8, 12, 16, 20];

    /// <summary>
    /// Вычисляет уровень заточки реликвии по опыту
    /// </summary>
    /// <param name="relicExp">Опыт реликвии</param>
    /// <returns>Уровень заточки (0-20)</returns>
    public static int GetRelicRefineLevel(int relicExp)
    {
        var refineLevel = 0;

        foreach (var exp in RelicExpTable)
        {
            if ((relicExp -= exp) < 0)
                break;

            refineLevel++;
        }

        return refineLevel;
    }

    /// <summary>
    /// Вычисляет значение основного аддона реликвии
    /// </summary>
    /// <param name="mainAddonId">ID основного аддона</param>
    /// <param name="relicExp">Опыт реликвии</param>
    /// <param name="soulLevel">Уровень души (редкость)</param>
    /// <param name="scaling">Справочник масштабирования из RelicDefinition</param>
    /// <returns>Вычисленное значение</returns>
    public static int GetMainAddonValue(
        int mainAddonId, 
        int relicExp, 
        int soulLevel, 
        Dictionary<int, int>? scaling)
    {
        if (scaling == null || !scaling.TryGetValue(mainAddonId, out var mainAddonMax))
        {
            return 0;
        }

        var refineLevel = GetRelicRefineLevel(relicExp);
        var mainAddonMin = mainAddonMax / 2;

        if (refineLevel == 0)
            return mainAddonMin;

        // soulLevel (rarity) 1-5 соответствует индексам 0-4
        var index = soulLevel - 1;
        if (index < 0) index = 0;
        if (index >= RelicRefineStages.Length) index = RelicRefineStages.Length - 1;

        var refineMaxLevel = RelicRefineStages[index];
        var refineBonus = (mainAddonMax - mainAddonMin) / refineMaxLevel;
        var curMainAddonValue = refineLevel * refineBonus;

        return mainAddonMin + curMainAddonValue;
    }
}
