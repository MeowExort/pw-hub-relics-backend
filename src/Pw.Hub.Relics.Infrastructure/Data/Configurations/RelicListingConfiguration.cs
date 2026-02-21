using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pw.Hub.Relics.Domain.Entities;

namespace Pw.Hub.Relics.Infrastructure.Data.Configurations;

public class RelicListingConfiguration : IEntityTypeConfiguration<RelicListing>
{
    public void Configure(EntityTypeBuilder<RelicListing> builder)
    {
        builder.ToTable("relic_listings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.RelicDefinitionId)
            .HasColumnName("relic_definition_id")
            .IsRequired();

        builder.Property(x => x.AbsorbExperience)
            .HasColumnName("absorb_experience")
            .IsRequired();

        builder.Property(x => x.EnhancementLevel)
            .HasColumnName("enhancement_level")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.SellerCharacterId)
            .HasColumnName("seller_character_id")
            .IsRequired();

        builder.Property(x => x.ShopPosition)
            .HasColumnName("shop_position")
            .IsRequired();

        builder.Property(x => x.Price)
            .HasColumnName("price")
            .IsRequired();

        builder.Property(x => x.ServerId)
            .HasColumnName("server_id")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(x => x.LastSeenAt)
            .HasColumnName("last_seen_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.SoldAt)
            .HasColumnName("sold_at");

        builder.Property(x => x.AttributesHash)
            .HasColumnName("attributes_hash")
            .HasMaxLength(64);

        builder.Property(x => x.JsonAttributes)
            .HasColumnName("json_attributes")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb")
            .IsRequired();

        // Concurrency token using PostgreSQL xmin system column
        builder.Property(x => x.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // Relationships
        builder.HasOne(x => x.RelicDefinition)
            .WithMany()
            .HasForeignKey(x => x.RelicDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Server)
            .WithMany()
            .HasForeignKey(x => x.ServerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint
        builder.HasIndex(x => new { x.SellerCharacterId, x.ShopPosition, x.ServerId, x.RelicDefinitionId })
            .IsUnique();

        // Covering index for optimized lookup
        builder.HasIndex(x => new { x.ServerId, x.SellerCharacterId, x.ShopPosition })
            .IncludeProperties(x => new { x.AttributesHash, x.RowVersion })
            .HasDatabaseName("IX_RelicListings_Lookup_Covering");

        // Performance indexes
        builder.HasIndex(x => new { x.IsActive, x.CreatedAt })
            .HasFilter("is_active = TRUE");

        builder.HasIndex(x => x.RelicDefinitionId);
        builder.HasIndex(x => x.ServerId);
        builder.HasIndex(x => x.Price);
        builder.HasIndex(x => x.CreatedAt).IsDescending();
        
        // Index for optimized lookup by attributes hash
        builder.HasIndex(x => new { x.ServerId, x.SellerCharacterId, x.ShopPosition, x.AttributesHash })
            .HasDatabaseName("IX_RelicListings_Lookup");
    }
}
