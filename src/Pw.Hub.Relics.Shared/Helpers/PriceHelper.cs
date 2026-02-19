namespace Pw.Hub.Relics.Shared.Helpers;

/// <summary>
/// Вспомогательные методы для работы с ценами
/// </summary>
public static class PriceHelper
{
    private const int SilverPerGold = 100;

    /// <summary>
    /// Конвертирует цену в серебре в форматированную строку
    /// </summary>
    /// <param name="priceInSilver">Цена в серебре</param>
    /// <returns>Форматированная строка (например, "1500 зол. 50 сер.")</returns>
    public static string FormatPrice(long priceInSilver)
    {
        var gold = priceInSilver / SilverPerGold;
        var silver = priceInSilver % SilverPerGold;

        if (silver == 0)
            return $"{gold} зол.";
        if (gold == 0)
            return $"{silver} сер.";
        return $"{gold} зол. {silver} сер.";
    }

    /// <summary>
    /// Конвертирует золото и серебро в общую сумму в серебре
    /// </summary>
    /// <param name="gold">Количество золота</param>
    /// <param name="silver">Количество серебра (по умолчанию 0)</param>
    /// <returns>Общая сумма в серебре</returns>
    public static long ToSilver(long gold, int silver = 0)
    {
        return gold * SilverPerGold + silver;
    }

    /// <summary>
    /// Извлекает золото из цены в серебре
    /// </summary>
    public static long GetGold(long priceInSilver)
    {
        return priceInSilver / SilverPerGold;
    }

    /// <summary>
    /// Извлекает остаток серебра из цены
    /// </summary>
    public static int GetSilver(long priceInSilver)
    {
        return (int)(priceInSilver % SilverPerGold);
    }
}
