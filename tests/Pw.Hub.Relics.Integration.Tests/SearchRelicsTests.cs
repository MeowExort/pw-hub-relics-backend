using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Pw.Hub.Relics.Api.Features.Relics.SearchRelics;
using Pw.Hub.Relics.Domain.Entities;
using Pw.Hub.Relics.Domain.Enums;
using Pw.Hub.Relics.Infrastructure.Data;
using Xunit;

namespace Pw.Hub.Relics.Integration.Tests;

public class SearchRelicsTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public SearchRelicsTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SearchRelics_ShouldFilterByAdditionalAttributes()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RelicsDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Clean up previous runs
        dbContext.RelicListings.RemoveRange(dbContext.RelicListings);
        dbContext.ServerDefinitions.RemoveRange(dbContext.ServerDefinitions);
        dbContext.RelicDefinitions.RemoveRange(dbContext.RelicDefinitions);
        dbContext.AttributeDefinitions.RemoveRange(dbContext.AttributeDefinitions);
        dbContext.SlotTypes.RemoveRange(dbContext.SlotTypes);
        await dbContext.SaveChangesAsync();

        // Seed Data
        var server = new ServerDefinition { Id = 1, Name = "Server1", Key = "server1" };
        dbContext.ServerDefinitions.Add(server);

        var slotType = new SlotType { Id = 1, Name = "Slot1" };
        dbContext.SlotTypes.Add(slotType);

        var relicDef = new RelicDefinition 
        { 
            Id = 1, 
            Name = "Relic1", 
            SoulLevel = 1, 
            SoulType = SoulType.Peace, 
            SlotType = slotType,
            Race = Race.Human,
            IconUri = "icon"
        };
        dbContext.RelicDefinitions.Add(relicDef);

        var attrDef1 = new AttributeDefinition { Id = 101, Name = "Attr1" };
        var attrDef2 = new AttributeDefinition { Id = 102, Name = "Attr2" };
        dbContext.AttributeDefinitions.AddRange(attrDef1, attrDef2);

        var listing1 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef,
            Server = server,
            Price = 1000,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            SellerCharacterId = 1001,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main),
                new(102, 20, AttributeCategory.Additional) // Matching attribute (val >= 15)
            }
        };

        var listing2 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef,
            Server = server,
            Price = 2000,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            SellerCharacterId = 1002,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main),
                new(102, 5, AttributeCategory.Additional) // Not matching value (val < 15)
            }
        };
        
        var listing3 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef,
            Server = server,
            Price = 3000,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            SellerCharacterId = 1003,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main)
                // No additional attribute
            }
        };

        dbContext.RelicListings.AddRange(listing1, listing2, listing3);
        await dbContext.SaveChangesAsync();

        var query = new SearchRelicsQuery
        {
            AdditionalAttributes = new List<RelicAttributeFilterDto>
            {
                new(102, 15) // Id=102, MinValue=15
            }
        };

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().Id.Should().Be(listing1.Id);
    }

    [Fact]
    public async Task SearchRelics_ShouldSortByAttributeValue_Descending()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RelicsDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Clean up previous runs
        dbContext.RelicListings.RemoveRange(dbContext.RelicListings);
        dbContext.ServerDefinitions.RemoveRange(dbContext.ServerDefinitions);
        dbContext.RelicDefinitions.RemoveRange(dbContext.RelicDefinitions);
        dbContext.AttributeDefinitions.RemoveRange(dbContext.AttributeDefinitions);
        dbContext.SlotTypes.RemoveRange(dbContext.SlotTypes);
        await dbContext.SaveChangesAsync();

        // Seed Data
        var server = new ServerDefinition { Id = 1, Name = "Server1", Key = "server1" };
        dbContext.ServerDefinitions.Add(server);

        var slotType = new SlotType { Id = 1, Name = "Slot1" };
        dbContext.SlotTypes.Add(slotType);

        var relicDef = new RelicDefinition 
        { 
            Id = 1, 
            Name = "Relic1", 
            SoulLevel = 1, 
            SoulType = SoulType.Peace, 
            SlotType = slotType,
            Race = Race.Human,
            IconUri = "icon"
        };
        dbContext.RelicDefinitions.Add(relicDef);

        var attrDef1 = new AttributeDefinition { Id = 101, Name = "Attr1" };
        var attrDef2 = new AttributeDefinition { Id = 59, Name = "TestAttr" };
        dbContext.AttributeDefinitions.AddRange(attrDef1, attrDef2);

        var listing1 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef,
            Server = server,
            Price = 1000,
            CreatedAt = DateTime.UtcNow.AddMinutes(-3),
            IsActive = true,
            SellerCharacterId = 2001,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main),
                new(59, 5, AttributeCategory.Additional) // Lowest value
            }
        };

        var listing2 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef,
            Server = server,
            Price = 2000,
            CreatedAt = DateTime.UtcNow.AddMinutes(-2),
            IsActive = true,
            SellerCharacterId = 2002,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main),
                new(59, 10, AttributeCategory.Additional) // Highest value
            }
        };
        
        var listing3 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef,
            Server = server,
            Price = 3000,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
            IsActive = true,
            SellerCharacterId = 2003,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main),
                new(59, 7, AttributeCategory.Additional) // Middle value
            }
        };

        dbContext.RelicListings.AddRange(listing1, listing2, listing3);
        await dbContext.SaveChangesAsync();

        var query = new SearchRelicsQuery
        {
            SortBy = "Attribute",
            SortDirection = "desc",
            SortAttributeId = 59
        };

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Id.Should().Be(listing2.Id); // Value 10 (highest)
        result.Items[1].Id.Should().Be(listing3.Id); // Value 7 (middle)
        result.Items[2].Id.Should().Be(listing1.Id); // Value 5 (lowest)
    }

    [Fact]
    public async Task SearchRelics_ShouldSortByAttributeValue_Ascending()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RelicsDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Clean up previous runs
        dbContext.RelicListings.RemoveRange(dbContext.RelicListings);
        dbContext.ServerDefinitions.RemoveRange(dbContext.ServerDefinitions);
        dbContext.RelicDefinitions.RemoveRange(dbContext.RelicDefinitions);
        dbContext.AttributeDefinitions.RemoveRange(dbContext.AttributeDefinitions);
        dbContext.SlotTypes.RemoveRange(dbContext.SlotTypes);
        await dbContext.SaveChangesAsync();

        // Seed Data
        var server = new ServerDefinition { Id = 1, Name = "Server1", Key = "server1" };
        dbContext.ServerDefinitions.Add(server);

        var slotType = new SlotType { Id = 1, Name = "Slot1" };
        dbContext.SlotTypes.Add(slotType);

        var relicDef = new RelicDefinition 
        { 
            Id = 1, 
            Name = "Relic1", 
            SoulLevel = 1, 
            SoulType = SoulType.Peace, 
            SlotType = slotType,
            Race = Race.Human,
            IconUri = "icon"
        };
        dbContext.RelicDefinitions.Add(relicDef);

        var attrDef1 = new AttributeDefinition { Id = 101, Name = "Attr1" };
        var attrDef2 = new AttributeDefinition { Id = 59, Name = "TestAttr" };
        dbContext.AttributeDefinitions.AddRange(attrDef1, attrDef2);

        var listing1 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef,
            Server = server,
            Price = 1000,
            CreatedAt = DateTime.UtcNow.AddMinutes(-3),
            IsActive = true,
            SellerCharacterId = 3001,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main),
                new(59, 5, AttributeCategory.Additional) // Lowest value
            }
        };

        var listing2 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef,
            Server = server,
            Price = 2000,
            CreatedAt = DateTime.UtcNow.AddMinutes(-2),
            IsActive = true,
            SellerCharacterId = 3002,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main),
                new(59, 10, AttributeCategory.Additional) // Highest value
            }
        };
        
        var listing3 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef,
            Server = server,
            Price = 3000,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
            IsActive = true,
            SellerCharacterId = 3003,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main),
                new(59, 7, AttributeCategory.Additional) // Middle value
            }
        };

        dbContext.RelicListings.AddRange(listing1, listing2, listing3);
        await dbContext.SaveChangesAsync();

        var query = new SearchRelicsQuery
        {
            SortBy = "Attribute",
            SortDirection = "asc",
            SortAttributeId = 59
        };

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Id.Should().Be(listing1.Id); // Value 5 (lowest)
        result.Items[1].Id.Should().Be(listing3.Id); // Value 7 (middle)
        result.Items[2].Id.Should().Be(listing2.Id); // Value 10 (highest)
    }

    [Fact]
    public async Task SearchRelics_ShouldFilterByAttributeWhenSortingByAttribute()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RelicsDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Clean up previous runs
        dbContext.RelicListings.RemoveRange(dbContext.RelicListings);
        dbContext.ServerDefinitions.RemoveRange(dbContext.ServerDefinitions);
        dbContext.RelicDefinitions.RemoveRange(dbContext.RelicDefinitions);
        dbContext.AttributeDefinitions.RemoveRange(dbContext.AttributeDefinitions);
        dbContext.SlotTypes.RemoveRange(dbContext.SlotTypes);
        await dbContext.SaveChangesAsync();

        // Seed Data
        var server = new ServerDefinition { Id = 1, Name = "Server1", Key = "server1" };
        dbContext.ServerDefinitions.Add(server);

        var slotType = new SlotType { Id = 1, Name = "Slot1" };
        dbContext.SlotTypes.Add(slotType);

        var relicDef = new RelicDefinition 
        { 
            Id = 1, 
            Name = "Relic1", 
            SoulLevel = 1, 
            SoulType = SoulType.Peace, 
            SlotType = slotType,
            Race = Race.Human,
            IconUri = "icon"
        };
        dbContext.RelicDefinitions.Add(relicDef);

        var attrDef1 = new AttributeDefinition { Id = 101, Name = "Attr1" };
        var attrDef2 = new AttributeDefinition { Id = 59, Name = "TestAttr" };
        var attrDef3 = new AttributeDefinition { Id = 60, Name = "OtherAttr" };
        dbContext.AttributeDefinitions.AddRange(attrDef1, attrDef2, attrDef3);

        // Listing with attribute 59 - should be included
        var listing1 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef,
            Server = server,
            Price = 1000,
            CreatedAt = DateTime.UtcNow.AddMinutes(-3),
            IsActive = true,
            SellerCharacterId = 4001,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main),
                new(59, 8, AttributeCategory.Additional) // Has attribute 59
            }
        };

        // Listing with attribute 59 - should be included
        var listing2 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef,
            Server = server,
            Price = 2000,
            CreatedAt = DateTime.UtcNow.AddMinutes(-2),
            IsActive = true,
            SellerCharacterId = 4002,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main),
                new(59, 12, AttributeCategory.Additional) // Has attribute 59
            }
        };
        
        // Listing WITHOUT attribute 59 - should NOT be included
        var listing3 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef,
            Server = server,
            Price = 500,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
            IsActive = true,
            SellerCharacterId = 4003,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main),
                new(60, 15, AttributeCategory.Additional) // Has different attribute (60, not 59)
            }
        };

        // Listing WITHOUT any additional attributes - should NOT be included
        var listing4 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef,
            Server = server,
            Price = 3000,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            SellerCharacterId = 4004,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main)
                // No additional attributes
            }
        };

        dbContext.RelicListings.AddRange(listing1, listing2, listing3, listing4);
        await dbContext.SaveChangesAsync();

        var query = new SearchRelicsQuery
        {
            SortBy = "Attribute",
            SortDirection = "desc",
            SortAttributeId = 59
        };

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.Items.Should().HaveCount(2, "only relics with attribute 59 should be returned");
        result.Items[0].Id.Should().Be(listing2.Id, "listing2 has highest value (12)");
        result.Items[1].Id.Should().Be(listing1.Id, "listing1 has lower value (8)");
        
        // Verify that listings without attribute 59 are not included
        result.Items.Should().NotContain(item => item.Id == listing3.Id);
        result.Items.Should().NotContain(item => item.Id == listing4.Id);
    }

    [Fact]
    public async Task SearchRelics_ShouldSortByAbsorbExperience_Descending()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RelicsDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Clean up previous runs
        dbContext.RelicListings.RemoveRange(dbContext.RelicListings);
        dbContext.ServerDefinitions.RemoveRange(dbContext.ServerDefinitions);
        dbContext.RelicDefinitions.RemoveRange(dbContext.RelicDefinitions);
        dbContext.AttributeDefinitions.RemoveRange(dbContext.AttributeDefinitions);
        dbContext.SlotTypes.RemoveRange(dbContext.SlotTypes);
        await dbContext.SaveChangesAsync();

        // Seed Data
        var server = new ServerDefinition { Id = 1, Name = "Server1", Key = "server1" };
        dbContext.ServerDefinitions.Add(server);

        var slotType = new SlotType { Id = 1, Name = "Slot1" };
        dbContext.SlotTypes.Add(slotType);

        var relicDef = new RelicDefinition 
        { 
            Id = 1, 
            Name = "Relic1", 
            SoulLevel = 1, 
            SoulType = SoulType.Peace, 
            SlotType = slotType,
            Race = Race.Human,
            IconUri = "icon"
        };
        dbContext.RelicDefinitions.Add(relicDef);

        var attrDef1 = new AttributeDefinition { Id = 101, Name = "Attr1" };
        dbContext.AttributeDefinitions.Add(attrDef1);

        var listing1 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef,
            Server = server,
            Price = 1000,
            AbsorbExperience = 500, // Lowest
            CreatedAt = DateTime.UtcNow.AddMinutes(-3),
            IsActive = true,
            SellerCharacterId = 5001,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main)
            }
        };

        var listing2 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef,
            Server = server,
            Price = 2000,
            AbsorbExperience = 1500, // Highest
            CreatedAt = DateTime.UtcNow.AddMinutes(-2),
            IsActive = true,
            SellerCharacterId = 5002,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main)
            }
        };
        
        var listing3 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef,
            Server = server,
            Price = 3000,
            AbsorbExperience = 1000, // Middle
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
            IsActive = true,
            SellerCharacterId = 5003,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main)
            }
        };

        dbContext.RelicListings.AddRange(listing1, listing2, listing3);
        await dbContext.SaveChangesAsync();

        var query = new SearchRelicsQuery
        {
            SortBy = "AbsorbExperience",
            SortDirection = "desc"
        };

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Id.Should().Be(listing2.Id); // AbsorbExperience 1500 (highest)
        result.Items[1].Id.Should().Be(listing3.Id); // AbsorbExperience 1000 (middle)
        result.Items[2].Id.Should().Be(listing1.Id); // AbsorbExperience 500 (lowest)
    }

    [Fact]
    public async Task SearchRelics_ShouldSortByAbsorbExperience_Ascending()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RelicsDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Clean up previous runs
        dbContext.RelicListings.RemoveRange(dbContext.RelicListings);
        dbContext.ServerDefinitions.RemoveRange(dbContext.ServerDefinitions);
        dbContext.RelicDefinitions.RemoveRange(dbContext.RelicDefinitions);
        dbContext.AttributeDefinitions.RemoveRange(dbContext.AttributeDefinitions);
        dbContext.SlotTypes.RemoveRange(dbContext.SlotTypes);
        await dbContext.SaveChangesAsync();

        // Seed Data
        var server = new ServerDefinition { Id = 1, Name = "Server1", Key = "server1" };
        dbContext.ServerDefinitions.Add(server);

        var slotType = new SlotType { Id = 1, Name = "Slot1" };
        dbContext.SlotTypes.Add(slotType);

        var relicDef = new RelicDefinition 
        { 
            Id = 1, 
            Name = "Relic1", 
            SoulLevel = 1, 
            SoulType = SoulType.Peace, 
            SlotType = slotType,
            Race = Race.Human,
            IconUri = "icon"
        };
        dbContext.RelicDefinitions.Add(relicDef);

        var attrDef1 = new AttributeDefinition { Id = 101, Name = "Attr1" };
        dbContext.AttributeDefinitions.Add(attrDef1);

        var listing1 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef,
            Server = server,
            Price = 1000,
            AbsorbExperience = 500, // Lowest
            CreatedAt = DateTime.UtcNow.AddMinutes(-3),
            IsActive = true,
            SellerCharacterId = 6001,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main)
            }
        };

        var listing2 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef,
            Server = server,
            Price = 2000,
            AbsorbExperience = 1500, // Highest
            CreatedAt = DateTime.UtcNow.AddMinutes(-2),
            IsActive = true,
            SellerCharacterId = 6002,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main)
            }
        };
        
        var listing3 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef,
            Server = server,
            Price = 3000,
            AbsorbExperience = 1000, // Middle
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
            IsActive = true,
            SellerCharacterId = 6003,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main)
            }
        };

        dbContext.RelicListings.AddRange(listing1, listing2, listing3);
        await dbContext.SaveChangesAsync();

        var query = new SearchRelicsQuery
        {
            SortBy = "AbsorbExperience",
            SortDirection = "asc"
        };

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Id.Should().Be(listing1.Id); // AbsorbExperience 500 (lowest)
        result.Items[1].Id.Should().Be(listing3.Id); // AbsorbExperience 1000 (middle)
        result.Items[2].Id.Should().Be(listing2.Id); // AbsorbExperience 1500 (highest)
    }

    [Fact]
    public async Task SearchRelics_ShouldSortByCreatedAt_Descending()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RelicsDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Clean up previous runs
        dbContext.RelicListings.RemoveRange(dbContext.RelicListings);
        dbContext.ServerDefinitions.RemoveRange(dbContext.ServerDefinitions);
        dbContext.RelicDefinitions.RemoveRange(dbContext.RelicDefinitions);
        dbContext.AttributeDefinitions.RemoveRange(dbContext.AttributeDefinitions);
        dbContext.SlotTypes.RemoveRange(dbContext.SlotTypes);
        await dbContext.SaveChangesAsync();

        // Seed Data
        var server = new ServerDefinition { Id = 1, Name = "Server1", Key = "server1" };
        dbContext.ServerDefinitions.Add(server);

        var slotType = new SlotType { Id = 1, Name = "Slot1" };
        dbContext.SlotTypes.Add(slotType);

        var relicDef = new RelicDefinition 
        { 
            Id = 1, 
            Name = "Relic1", 
            SoulLevel = 1, 
            SoulType = SoulType.Peace, 
            SlotType = slotType,
            Race = Race.Human,
            IconUri = "icon"
        };
        dbContext.RelicDefinitions.Add(relicDef);

        var attrDef1 = new AttributeDefinition { Id = 101, Name = "Attr1" };
        dbContext.AttributeDefinitions.Add(attrDef1);

        var listing1 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef,
            Server = server,
            Price = 1000,
            CreatedAt = DateTime.UtcNow.AddHours(-3), // Oldest
            IsActive = true,
            SellerCharacterId = 7001,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main)
            }
        };

        var listing2 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef,
            Server = server,
            Price = 2000,
            CreatedAt = DateTime.UtcNow.AddHours(-1), // Newest
            IsActive = true,
            SellerCharacterId = 7002,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main)
            }
        };
        
        var listing3 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef,
            Server = server,
            Price = 3000,
            CreatedAt = DateTime.UtcNow.AddHours(-2), // Middle
            IsActive = true,
            SellerCharacterId = 7003,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main)
            }
        };

        dbContext.RelicListings.AddRange(listing1, listing2, listing3);
        await dbContext.SaveChangesAsync();

        var query = new SearchRelicsQuery
        {
            SortBy = "CreatedAt",
            SortDirection = "desc"
        };

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Id.Should().Be(listing2.Id); // Newest
        result.Items[1].Id.Should().Be(listing3.Id); // Middle
        result.Items[2].Id.Should().Be(listing1.Id); // Oldest
    }

    [Fact]
    public async Task SearchRelics_ShouldSortByCreatedAt_Ascending()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RelicsDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Clean up previous runs
        dbContext.RelicListings.RemoveRange(dbContext.RelicListings);
        dbContext.ServerDefinitions.RemoveRange(dbContext.ServerDefinitions);
        dbContext.RelicDefinitions.RemoveRange(dbContext.RelicDefinitions);
        dbContext.AttributeDefinitions.RemoveRange(dbContext.AttributeDefinitions);
        dbContext.SlotTypes.RemoveRange(dbContext.SlotTypes);
        await dbContext.SaveChangesAsync();

        // Seed Data
        var server = new ServerDefinition { Id = 1, Name = "Server1", Key = "server1" };
        dbContext.ServerDefinitions.Add(server);

        var slotType = new SlotType { Id = 1, Name = "Slot1" };
        dbContext.SlotTypes.Add(slotType);

        var relicDef = new RelicDefinition 
        { 
            Id = 1, 
            Name = "Relic1", 
            SoulLevel = 1, 
            SoulType = SoulType.Peace, 
            SlotType = slotType,
            Race = Race.Human,
            IconUri = "icon"
        };
        dbContext.RelicDefinitions.Add(relicDef);

        var attrDef1 = new AttributeDefinition { Id = 101, Name = "Attr1" };
        dbContext.AttributeDefinitions.Add(attrDef1);

        var listing1 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef,
            Server = server,
            Price = 1000,
            CreatedAt = DateTime.UtcNow.AddHours(-3), // Oldest
            IsActive = true,
            SellerCharacterId = 8001,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main)
            }
        };

        var listing2 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef,
            Server = server,
            Price = 2000,
            CreatedAt = DateTime.UtcNow.AddHours(-1), // Newest
            IsActive = true,
            SellerCharacterId = 8002,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main)
            }
        };
        
        var listing3 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef,
            Server = server,
            Price = 3000,
            CreatedAt = DateTime.UtcNow.AddHours(-2), // Middle
            IsActive = true,
            SellerCharacterId = 8003,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main)
            }
        };

        dbContext.RelicListings.AddRange(listing1, listing2, listing3);
        await dbContext.SaveChangesAsync();

        var query = new SearchRelicsQuery
        {
            SortBy = "CreatedAt",
            SortDirection = "asc"
        };

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Id.Should().Be(listing1.Id); // Oldest
        result.Items[1].Id.Should().Be(listing3.Id); // Middle
        result.Items[2].Id.Should().Be(listing2.Id); // Newest
    }

    [Fact]
    public async Task SearchRelics_ShouldSortBySoulLevel_Descending()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RelicsDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Clean up previous runs
        dbContext.RelicListings.RemoveRange(dbContext.RelicListings);
        dbContext.ServerDefinitions.RemoveRange(dbContext.ServerDefinitions);
        dbContext.RelicDefinitions.RemoveRange(dbContext.RelicDefinitions);
        dbContext.AttributeDefinitions.RemoveRange(dbContext.AttributeDefinitions);
        dbContext.SlotTypes.RemoveRange(dbContext.SlotTypes);
        await dbContext.SaveChangesAsync();

        // Seed Data
        var server = new ServerDefinition { Id = 1, Name = "Server1", Key = "server1" };
        dbContext.ServerDefinitions.Add(server);

        var slotType = new SlotType { Id = 1, Name = "Slot1" };
        dbContext.SlotTypes.Add(slotType);

        var relicDef1 = new RelicDefinition 
        { 
            Id = 1, 
            Name = "Relic1", 
            SoulLevel = 1, // Lowest
            SoulType = SoulType.Peace, 
            SlotType = slotType,
            Race = Race.Human,
            IconUri = "icon"
        };
        var relicDef2 = new RelicDefinition 
        { 
            Id = 2, 
            Name = "Relic2", 
            SoulLevel = 5, // Highest
            SoulType = SoulType.Peace, 
            SlotType = slotType,
            Race = Race.Human,
            IconUri = "icon"
        };
        var relicDef3 = new RelicDefinition 
        { 
            Id = 3, 
            Name = "Relic3", 
            SoulLevel = 3, // Middle
            SoulType = SoulType.Peace, 
            SlotType = slotType,
            Race = Race.Human,
            IconUri = "icon"
        };
        dbContext.RelicDefinitions.AddRange(relicDef1, relicDef2, relicDef3);

        var attrDef1 = new AttributeDefinition { Id = 101, Name = "Attr1" };
        dbContext.AttributeDefinitions.Add(attrDef1);

        var listing1 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef1,
            Server = server,
            Price = 1000,
            CreatedAt = DateTime.UtcNow.AddMinutes(-3),
            IsActive = true,
            SellerCharacterId = 9001,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main)
            }
        };

        var listing2 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef2,
            Server = server,
            Price = 2000,
            CreatedAt = DateTime.UtcNow.AddMinutes(-2),
            IsActive = true,
            SellerCharacterId = 9002,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main)
            }
        };
        
        var listing3 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef3,
            Server = server,
            Price = 3000,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
            IsActive = true,
            SellerCharacterId = 9003,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main)
            }
        };

        dbContext.RelicListings.AddRange(listing1, listing2, listing3);
        await dbContext.SaveChangesAsync();

        var query = new SearchRelicsQuery
        {
            SortBy = "SoulLevel",
            SortDirection = "desc"
        };

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Id.Should().Be(listing2.Id); // SoulLevel 5 (highest)
        result.Items[1].Id.Should().Be(listing3.Id); // SoulLevel 3 (middle)
        result.Items[2].Id.Should().Be(listing1.Id); // SoulLevel 1 (lowest)
    }

    [Fact]
    public async Task SearchRelics_ShouldSortBySoulLevel_Ascending()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RelicsDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Clean up previous runs
        dbContext.RelicListings.RemoveRange(dbContext.RelicListings);
        dbContext.ServerDefinitions.RemoveRange(dbContext.ServerDefinitions);
        dbContext.RelicDefinitions.RemoveRange(dbContext.RelicDefinitions);
        dbContext.AttributeDefinitions.RemoveRange(dbContext.AttributeDefinitions);
        dbContext.SlotTypes.RemoveRange(dbContext.SlotTypes);
        await dbContext.SaveChangesAsync();

        // Seed Data
        var server = new ServerDefinition { Id = 1, Name = "Server1", Key = "server1" };
        dbContext.ServerDefinitions.Add(server);

        var slotType = new SlotType { Id = 1, Name = "Slot1" };
        dbContext.SlotTypes.Add(slotType);

        var relicDef1 = new RelicDefinition 
        { 
            Id = 1, 
            Name = "Relic1", 
            SoulLevel = 1, // Lowest
            SoulType = SoulType.Peace, 
            SlotType = slotType,
            Race = Race.Human,
            IconUri = "icon"
        };
        var relicDef2 = new RelicDefinition 
        { 
            Id = 2, 
            Name = "Relic2", 
            SoulLevel = 5, // Highest
            SoulType = SoulType.Peace, 
            SlotType = slotType,
            Race = Race.Human,
            IconUri = "icon"
        };
        var relicDef3 = new RelicDefinition 
        { 
            Id = 3, 
            Name = "Relic3", 
            SoulLevel = 3, // Middle
            SoulType = SoulType.Peace, 
            SlotType = slotType,
            Race = Race.Human,
            IconUri = "icon"
        };
        dbContext.RelicDefinitions.AddRange(relicDef1, relicDef2, relicDef3);

        var attrDef1 = new AttributeDefinition { Id = 101, Name = "Attr1" };
        dbContext.AttributeDefinitions.Add(attrDef1);

        var listing1 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef1,
            Server = server,
            Price = 1000,
            CreatedAt = DateTime.UtcNow.AddMinutes(-3),
            IsActive = true,
            SellerCharacterId = 10001,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main)
            }
        };

        var listing2 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef2,
            Server = server,
            Price = 2000,
            CreatedAt = DateTime.UtcNow.AddMinutes(-2),
            IsActive = true,
            SellerCharacterId = 10002,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main)
            }
        };
        
        var listing3 = new RelicListing
        {
            Id = Guid.NewGuid(),
            RelicDefinition = relicDef3,
            Server = server,
            Price = 3000,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
            IsActive = true,
            SellerCharacterId = 10003,
            ShopPosition = 1,
            JsonAttributes = new List<Pw.Hub.Relics.Domain.Entities.RelicAttributeDto>
            {
                new(101, 10, AttributeCategory.Main)
            }
        };

        dbContext.RelicListings.AddRange(listing1, listing2, listing3);
        await dbContext.SaveChangesAsync();

        var query = new SearchRelicsQuery
        {
            SortBy = "SoulLevel",
            SortDirection = "asc"
        };

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Id.Should().Be(listing1.Id); // SoulLevel 1 (lowest)
        result.Items[1].Id.Should().Be(listing3.Id); // SoulLevel 3 (middle)
        result.Items[2].Id.Should().Be(listing2.Id); // SoulLevel 5 (highest)
    }
}
