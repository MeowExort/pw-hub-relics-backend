using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pw.Hub.Relics.Api.Helpers;
using Pw.Hub.Relics.Domain.Entities;
using Pw.Hub.Relics.Domain.Enums;
using Pw.Hub.Relics.Infrastructure.Data;

namespace Pw.Hub.Relics.Api.Features.Relics;

[ApiController]
[Route("api/relics/seed")]
[ApiKeyAuth]
public class SeedingController : ControllerBase
{
    private readonly RelicsDbContext _context;
    private readonly Random _random = new();

    public SeedingController(RelicsDbContext context)
    {
        _context = context;
    }

    [HttpPost("market")]
    public async Task<IActionResult> SeedMarket(CancellationToken cancellationToken)
    {
        var servers = await _context.ServerDefinitions.ToListAsync(cancellationToken);
        var relicDefinitions = await _context.RelicDefinitions.ToListAsync(cancellationToken);
        var attributeDefinitions = await _context.AttributeDefinitions.ToListAsync(cancellationToken);
        var curves = await _context.EnhancementCurves.OrderBy(c => c.Level).ToListAsync(cancellationToken);

        if (!servers.Any() || !relicDefinitions.Any() || !attributeDefinitions.Any())
        {
            return BadRequest("Необходимые справочники (серверы, реликвии, атрибуты) пусты.");
        }

        // ПА и ПЗ id из конфигурации (59 и 60)
        const int paId = 59;
        const int pzId = 60;

        var mainAttrPool = attributeDefinitions.Where(a => a.Id != paId && a.Id != pzId).ToList();
        
        var totalGenerated = 0;

        foreach (var server in servers)
        {
            var relicCount = _random.Next(5000, 10001);
            var batchSize = 1000;
            var listings = new List<RelicListing>();

            for (int i = 0; i < relicCount; i++)
            {
                var definition = relicDefinitions[_random.Next(relicDefinitions.Count)];
                
                // 1. Основная характеристика
                var mainAttrDef = mainAttrPool[_random.Next(mainAttrPool.Count)];
                
                // 2. Дополнительные характеристики (0-4)
                // Шанс на 4 выше при высоком уровне души (SoulLevel 1-5)
                var additionalCount = GetRandomAdditionalCount(definition.SoulLevel);
                var additionalAttrs = new List<AttributeDefinition>();
                var availableForAdditional = attributeDefinitions.ToList();
                
                for (int j = 0; j < additionalCount; j++)
                {
                    var attr = availableForAdditional[_random.Next(availableForAdditional.Count)];
                    additionalAttrs.Add(attr);
                    availableForAdditional.Remove(attr); // Чтобы не повторялись
                }

                // 6. Уровень заточки (допустим 0-12)
                var enhancementLevel = _random.Next(0, 13);

                // 7. Опыт при поглощении
                var absorbExp = CalculateAbsorbExperience(enhancementLevel, curves);

                // 3. Базовая цена
                long basePrice = GetBasePrice(definition.SoulLevel);

                // 4. Случайный процент от допов (1% - 100%)
                // "чем больше характеристик - тем больше %"
                double bonusPercent = GetBonusPercent(additionalCount);
                basePrice = (long)(basePrice * (1 + bonusPercent));

                // 5. ПА или ПЗ в допах -> x2
                if (additionalAttrs.Any(a => a.Id == paId || a.Id == pzId))
                {
                    basePrice *= 2;
                }

                // Коэффициенты серверов
                // Реликвии для капеллы = -10% к цене, для центавра = +5%, для мицара = +15%, для алькора = +30%
                basePrice = ApplyServerMultiplier(basePrice, server.Key);

                var listing = new RelicListing
                {
                    Id = Guid.NewGuid(),
                    RelicDefinitionId = definition.Id,
                    ServerId = server.Id,
                    EnhancementLevel = enhancementLevel,
                    AbsorbExperience = absorbExp,
                    Price = basePrice,
                    SellerCharacterId = _random.Next(1000, 1000000),
                    ShopPosition = _random.Next(1, 100),
                    CreatedAt = DateTime.UtcNow.AddMinutes(-_random.Next(1, 10000)),
                    LastSeenAt = DateTime.UtcNow,
                    IsActive = true,
                    JsonAttributes = new List<RelicAttributeDto>()
                };

                // Добавляем атрибуты
                listing.JsonAttributes.Add(new RelicAttributeDto(
                    mainAttrDef.Id,
                    _random.Next(10, 100), // Рандомное значение
                    AttributeCategory.Main
                ));

                foreach (var attr in additionalAttrs)
                {
                    listing.JsonAttributes.Add(new RelicAttributeDto(
                        attr.Id,
                        _random.Next(5, 50),
                        AttributeCategory.Additional
                    ));
                }

                listings.Add(listing);

                if (listings.Count >= batchSize)
                {
                    await _context.RelicListings.AddRangeAsync(listings, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                    totalGenerated += listings.Count;
                    listings.Clear();
                }
            }

            if (listings.Any())
            {
                await _context.RelicListings.AddRangeAsync(listings, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                totalGenerated += listings.Count;
            }
        }

        return Ok(new { Message = $"Сгенерировано {totalGenerated} реликвий.", TotalServers = servers.Count });
    }

    private int GetRandomAdditionalCount(int soulLevel)
    {
        // Чем выше soulLevel, тем выше шанс на 4.
        // SoulLevel: 1..5
        var rand = _random.Next(100);
        
        // Примерные веса в зависимости от уровня
        // Уровень 1: 0(40%), 1(30%), 2(20%), 3(8%), 4(2%)
        // Уровень 5: 0(5%), 1(10%), 2(20%), 3(30%), 4(35%)
        
        int[][] chances = new int[][]
        {
            new int[] { 40, 70, 90, 98 }, // Level 1
            new int[] { 30, 60, 85, 95 }, // Level 2
            new int[] { 20, 45, 75, 90 }, // Level 3
            new int[] { 10, 30, 60, 85 }, // Level 4
            new int[] { 5, 15, 35, 65 }   // Level 5
        };

        var levelChances = chances[Math.Clamp(soulLevel - 1, 0, 4)];
        
        if (rand < levelChances[0]) return 0;
        if (rand < levelChances[1]) return 1;
        if (rand < levelChances[2]) return 2;
        if (rand < levelChances[3]) return 3;
        return 4;
    }

    private long GetBasePrice(int soulLevel)
    {
        // 1 - от 50 серебра до 250, 
        // 2 - от 100 до 400, 
        // 3 - от 200 до 600, 
        // 4 - от 500 до 2500, 
        // 5 - от 1000 до 1500.
        return soulLevel switch
        {
            1 => _random.Next(50, 251),
            2 => _random.Next(100, 401),
            3 => _random.Next(200, 601),
            4 => _random.Next(500, 2501),
            5 => _random.Next(1000, 1501),
            _ => 50
        };
    }

    private double GetBonusPercent(int additionalCount)
    {
        // "чем больше характеристик - тем больше %". Минимально - +1%, максимально +100%
        if (additionalCount == 0) return _random.Next(1, 11) / 100.0; // 1-10%
        
        var min = additionalCount * 20 - 10; // 1: 10, 2: 30, 3: 50, 4: 70
        var max = additionalCount * 25;      // 1: 25, 2: 50, 3: 75, 4: 100
        
        return _random.Next(Math.Max(1, min), max + 1) / 100.0;
    }

    private long ApplyServerMultiplier(long price, string serverKey)
    {
        // Реликвии для капеллы = -10% к цене, для центавра = +5%, для мицара = +15%, для алькора = +30%
        return serverKey.ToLower() switch
        {
            "capella" => (long)(price * 0.9),
            "centaur" => (long)(price * 1.05),
            "mizar" => (long)(price * 1.15),
            "alkor" => (long)(price * 1.3),
            _ => price
        };
    }

    private int CalculateAbsorbExperience(int enhancementLevel, List<EnhancementCurve> curves)
    {
        if (enhancementLevel == 0) return 0;
        
        // Опыт при поглащении - это половина опыта прокачки до текущей заточки.
        var totalExpToCurrentLevel = curves
            .Where(c => c.Level <= enhancementLevel)
            .Sum(c => c.RequiredExperience);
            
        return totalExpToCurrentLevel / 2;
    }
}
