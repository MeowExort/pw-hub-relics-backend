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
}
