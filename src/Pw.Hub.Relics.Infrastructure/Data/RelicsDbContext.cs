using Microsoft.EntityFrameworkCore;
using Pw.Hub.Relics.Domain.Entities;

namespace Pw.Hub.Relics.Infrastructure.Data;

public class RelicsDbContext : DbContext
{
    public RelicsDbContext(DbContextOptions<RelicsDbContext> options) : base(options)
    {
    }

    public DbSet<SlotType> SlotTypes => Set<SlotType>();
    public DbSet<AttributeDefinition> AttributeDefinitions => Set<AttributeDefinition>();
    public DbSet<ServerDefinition> ServerDefinitions => Set<ServerDefinition>();
    public DbSet<EnhancementCurve> EnhancementCurves => Set<EnhancementCurve>();
    public DbSet<RelicDefinition> RelicDefinitions => Set<RelicDefinition>();
    public DbSet<RelicListing> RelicListings => Set<RelicListing>();
    public DbSet<NotificationFilter> NotificationFilters => Set<NotificationFilter>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();
    public DbSet<TelegramBinding> TelegramBindings => Set<TelegramBinding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RelicsDbContext).Assembly);
    }
}
