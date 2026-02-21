using System.Security.Cryptography;
using System.Text;
using Pw.Hub.Relics.Domain.Entities;
using Pw.Hub.Relics.Domain.Enums;
using Pw.Hub.Relics.Shared.Helpers;
using Pw.Hub.Relics.Shared.Packets;

namespace Pw.Hub.Relics.Api.Helpers;

/// <summary>
/// Вспомогательный класс для вычисления хеша атрибутов реликвии
/// </summary>
public static class AttributeHashHelper
{
    /// <summary>
    /// Вычисляет SHA256 хеш на основе основного аддона и списка дополнительных аддонов
    /// </summary>
    public static string ComputeHash(int mainAddon, IEnumerable<(int Id, int Value)> addons)
    {
        var sb = new StringBuilder();
        sb.Append(mainAddon);
        
        foreach (var addon in addons.OrderBy(a => a.Id).ThenBy(a => a.Value))
        {
            sb.Append($"|{addon.Id}:{addon.Value}");
        }
        
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes);
    }

    /// <summary>
    /// Вычисляет хеш из объекта Relic (из пакета)
    /// </summary>
    public static string ComputeHashFromRelic(Relic relic)
    {
        var mainAddonMapped = AddonMapping.GetRelicAttributeType(relic.main_addon) ?? -1;
        var addons = relic.addons.Select(a => (AddonMapping.GetRelicAttributeType(a.id) ?? 0, (int)a.value));
        return ComputeHash(mainAddonMapped, addons);
    }

    /// <summary>
    /// Вычисляет хеш из списка атрибутов (из БД)
    /// </summary>
    public static string ComputeHashFromAttributes(IEnumerable<RelicAttributeDto> attributes)
    {
        var mainAttr = attributes.FirstOrDefault(a => a.Category == AttributeCategory.Main);
        var mainAddon = mainAttr?.AttributeDefinitionId ?? -1;
        
        var addons = attributes
            .Where(a => a.Category == AttributeCategory.Additional)
            .Select(a => (a.AttributeDefinitionId, a.Value));
        
        return ComputeHash(mainAddon, addons);
    }
}
