using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pw.Hub.Relics.Domain.Entities;

namespace Pw.Hub.Relics.Infrastructure.Data.Configurations;

public class RelicAttributeConfiguration : IEntityTypeConfiguration<RelicAttribute>
{
    public void Configure(EntityTypeBuilder<RelicAttribute> builder)
    {
        builder.ToTable("relic_attributes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.RelicListingId)
            .HasColumnName("relic_listing_id")
            .IsRequired();

        builder.Property(x => x.AttributeDefinitionId)
            .HasColumnName("attribute_definition_id")
            .IsRequired();

        builder.Property(x => x.Value)
            .HasColumnName("value")
            .IsRequired();

        builder.Property(x => x.Category)
            .HasColumnName("category")
            .IsRequired();

        builder.HasOne(x => x.AttributeDefinition)
            .WithMany()
            .HasForeignKey(x => x.AttributeDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.RelicListingId);
        builder.HasIndex(x => x.AttributeDefinitionId);
        builder.HasIndex(x => x.Category);
    }
}
