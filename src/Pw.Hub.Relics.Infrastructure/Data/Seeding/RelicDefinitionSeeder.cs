using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pw.Hub.Relics.Domain.Entities;
using Pw.Hub.Relics.Domain.Enums;

namespace Pw.Hub.Relics.Infrastructure.Data.Seeding;

/// <summary>
/// Сервис для сидинга справочника реликвий из JSON файла
/// </summary>
public class RelicDefinitionSeeder
{
    private readonly RelicsDbContext _context;
    private readonly ILogger<RelicDefinitionSeeder> _logger;
    private static readonly HttpClient HttpClient = new();

    public RelicDefinitionSeeder(RelicsDbContext context, ILogger<RelicDefinitionSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Загружает данные реликвий из JSON файла или по URI в базу данных
    /// </summary>
    /// <param name="jsonUri">Путь к JSON файлу или URL</param>
    /// <param name="iconBaseUri">Базовый URI для иконок (например, CDN URL)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task SeedAsync(string jsonUri, string iconBaseUri, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting RelicDefinition seeding from {Uri}", jsonUri);

        string jsonContent;

        if (jsonUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
            jsonUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                _logger.LogInformation("Downloading JSON from {Uri}", jsonUri);
                jsonContent = await HttpClient.GetStringAsync(jsonUri, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download JSON from {Uri}", jsonUri);
                return;
            }
        }
        else
        {
            if (!File.Exists(jsonUri))
            {
                _logger.LogWarning("JSON file not found: {FilePath}", jsonUri);
                return;
            }
            jsonContent = await File.ReadAllTextAsync(jsonUri, cancellationToken);
        }

        // Получаем существующие записи для проверки
        var existingRelics = await _context.RelicDefinitions
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        // Десериализуем JSON
        var relicsDto = JsonSerializer.Deserialize<List<RelicJsonDto>>(jsonContent);

        if (relicsDto == null || relicsDto.Count == 0)
        {
            _logger.LogWarning("No relics found in JSON file");
            return;
        }

        _logger.LogInformation("Found {Count} relics in JSON file", relicsDto.Count);

        var newDefinitions = new List<RelicDefinition>();
        var updatedCount = 0;

        foreach (var dto in relicsDto)
        {
            var scaling = new Dictionary<int, int>();
            if (dto.ProfessionGroups != null)
            {
                foreach (var group in dto.ProfessionGroups)
                {
                    if (group.GroupId == 32804 && group.Extensions != null)
                    {
                        foreach (var ext in group.Extensions)
                        {
                            scaling[ext.ExtId] = ext.PropValue;
                        }
                    }
                }
            }

            var finalScaling = scaling.Count > 0 ? scaling : null;

            if (existingRelics.TryGetValue(dto.Id, out var existing))
            {
                // Если запись есть, проверяем нужно ли обогатить MainAttributeScaling
                if (existing.MainAttributeScaling == null && finalScaling != null)
                {
                    existing.MainAttributeScaling = finalScaling;
                    updatedCount++;
                }
            }
            else
            {
                // Если записи нет, создаем новую
                newDefinitions.Add(new RelicDefinition
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    SoulLevel = dto.Rarity,
                    SoulType = (SoulType)dto.Type,
                    SlotTypeId = dto.Category,
                    Race = (Race)dto.Race,
                    IconUri = BuildIconUri(iconBaseUri, dto.FileIcon),
                    MainAttributeScaling = finalScaling
                });
            }
        }

        if (newDefinitions.Count > 0)
        {
            await _context.RelicDefinitions.AddRangeAsync(newDefinitions, cancellationToken);
            _logger.LogInformation("Adding {Count} new relic definitions", newDefinitions.Count);
        }

        if (updatedCount > 0 || newDefinitions.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully updated {Updated} and added {Added} relic definitions", updatedCount, newDefinitions.Count);
        }
        else
        {
            _logger.LogInformation("No changes needed for relic definitions");
        }
    }

    /// <summary>
    /// Формирует полный URI иконки
    /// </summary>
    private static string? BuildIconUri(string baseUri, string fileIcon)
    {
        if (string.IsNullOrWhiteSpace(fileIcon))
            return null;

        // Убираем trailing slash из baseUri если есть
        baseUri = baseUri.TrimEnd('/');
        
        return $"{baseUri}/{fileIcon}";
    }
}
