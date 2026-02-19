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

        // Проверяем, есть ли уже данные в таблице
        var existingCount = await _context.RelicDefinitions.CountAsync(cancellationToken);
        if (existingCount > 0)
        {
            _logger.LogInformation("RelicDefinitions table already contains {Count} records. Skipping seed.", existingCount);
            return;
        }

        // Десериализуем JSON
        var relicsDto = JsonSerializer.Deserialize<List<RelicJsonDto>>(jsonContent);

        if (relicsDto == null || relicsDto.Count == 0)
        {
            _logger.LogWarning("No relics found in JSON file");
            return;
        }

        _logger.LogInformation("Found {Count} relics in JSON file", relicsDto.Count);

        // Преобразуем DTO в сущности
        var relicDefinitions = relicsDto.Select(dto => new RelicDefinition
        {
            Id = dto.Id,
            Name = dto.Name,
            SoulLevel = dto.Rarity,
            SoulType = (SoulType)dto.Type,
            SlotTypeId = dto.Category,
            Race = (Race)dto.Race,
            IconUri = BuildIconUri(iconBaseUri, dto.FileIcon)
        }).ToList();

        // Добавляем в базу данных
        await _context.RelicDefinitions.AddRangeAsync(relicDefinitions, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully seeded {Count} relic definitions", relicDefinitions.Count);
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
