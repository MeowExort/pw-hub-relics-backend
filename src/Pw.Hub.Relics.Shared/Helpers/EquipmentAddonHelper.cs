using System.Text.Json;

namespace Pw.Hub.Relics.Shared.Helpers;

/// <summary>
/// Вспомогательные методы для работы с аддонами экипировки
/// </summary>
public static class EquipmentAddonHelper
{
    private static Dictionary<int, EquipmentAddon>? _addonsCache;
    private static readonly object _lock = new();

    /// <summary>
    /// Модель аддона экипировки из EQUIPMENT_ADDON.json
    /// </summary>
    public class EquipmentAddon
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int NumParams { get; set; }
        public int Param1 { get; set; }
        public int Param2 { get; set; }
        public int Param3 { get; set; }
    }

    /// <summary>
    /// Структура JSON-файла EQUIPMENT_ADDON.json
    /// </summary>
    private class EquipmentAddonFile
    {
        public List<EquipmentAddonItem> Items { get; set; } = new();
    }

    private class EquipmentAddonItem
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public int num_params { get; set; }
        public int param1 { get; set; }
        public int param2 { get; set; }
        public int param3 { get; set; }
    }

    /// <summary>
    /// Загружает данные аддонов по URI
    /// </summary>
    /// <param name="uri">URI к файлу EQUIPMENT_ADDON.json</param>
    public static async Task LoadAddonsFromUriAsync(string uri)
    {
        using var httpClient = new HttpClient();
        var json = await httpClient.GetStringAsync(uri);
        LoadAddonsFromJson(json);
    }

    /// <summary>
    /// Загружает данные аддонов из JSON-файла
    /// </summary>
    /// <param name="jsonFilePath">Путь к файлу EQUIPMENT_ADDON.json</param>
    public static void LoadAddons(string jsonFilePath)
    {
        lock (_lock)
        {
            if (_addonsCache != null)
                return;

            var json = File.ReadAllText(jsonFilePath);
            var data = JsonSerializer.Deserialize<EquipmentAddonFile>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _addonsCache = new Dictionary<int, EquipmentAddon>();

            if (data?.Items != null)
            {
                foreach (var item in data.Items)
                {
                    _addonsCache[item.id] = new EquipmentAddon
                    {
                        Id = item.id,
                        Name = item.name,
                        NumParams = item.num_params,
                        Param1 = item.param1,
                        Param2 = item.param2,
                        Param3 = item.param3
                    };
                }
            }
        }
    }

    /// <summary>
    /// Загружает данные аддонов из JSON-строки
    /// </summary>
    /// <param name="json">JSON-строка с данными аддонов</param>
    public static void LoadAddonsFromJson(string json)
    {
        lock (_lock)
        {
            var data = JsonSerializer.Deserialize<EquipmentAddonFile>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _addonsCache = new Dictionary<int, EquipmentAddon>();

            if (data?.Items != null)
            {
                foreach (var item in data.Items)
                {
                    _addonsCache[item.id] = new EquipmentAddon
                    {
                        Id = item.id,
                        Name = item.name,
                        NumParams = item.num_params,
                        Param1 = item.param1,
                        Param2 = item.param2,
                        Param3 = item.param3
                    };
                }
            }
        }
    }

    /// <summary>
    /// Вычисляет значение аддона по формуле: param2 * addon_level
    /// </summary>
    /// <param name="addonId">ID типа аддона</param>
    /// <param name="addonLevel">Уровень аддона (value из пакета)</param>
    /// <returns>Вычисленное значение аддона или addonLevel, если шаблон не найден</returns>
    public static int GetAddonValue(int addonId, int addonLevel)
    {
        if (_addonsCache == null)
        {
            // Если данные не загружены, возвращаем исходное значение
            return addonLevel;
        }

        if (_addonsCache.TryGetValue(addonId, out var addonTemplate))
        {
            return addonTemplate.Param2 * addonLevel;
        }

        // Если шаблон не найден, возвращаем исходное значение
        return addonLevel;
    }

    /// <summary>
    /// Проверяет, загружены ли данные аддонов
    /// </summary>
    public static bool IsLoaded => _addonsCache != null;

    /// <summary>
    /// Получает аддон по ID
    /// </summary>
    public static EquipmentAddon? GetAddon(int addonId)
    {
        if (_addonsCache == null)
            return null;

        _addonsCache.TryGetValue(addonId, out var addon);
        return addon;
    }
}
