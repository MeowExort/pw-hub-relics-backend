using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pw.Hub.Relics.Domain.Entities;

namespace Pw.Hub.Relics.Infrastructure.Data.Configurations;

public class PriceHistoryConfiguration : IEntityTypeConfiguration<PriceHistory>
{
    public void Configure(EntityTypeBuilder<PriceHistory> builder)
    {
        builder.ToTable("price_history");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.RelicDefinitionId)
            .HasColumnName("relic_definition_id")
            .IsRequired();

        builder.Property(x => x.MainAttributeId)
            .HasColumnName("main_attribute_id")
            .IsRequired();

        builder.Property(x => x.AdditionalAttributeIds)
            .HasColumnName("additional_attribute_ids")
            .HasColumnType("integer[]");

        builder.Property(x => x.Price)
            .HasColumnName("price")
            .IsRequired();

        builder.Property(x => x.ServerId)
            .HasColumnName("server_id")
            .IsRequired();

        builder.Property(x => x.Timestamp)
            .HasColumnName("timestamp")
            .IsRequired();

        // Indexes
        builder.HasIndex(x => x.RelicDefinitionId);
        builder.HasIndex(x => x.MainAttributeId);
        builder.HasIndex(x => x.Timestamp).IsDescending();
        builder.HasIndex(x => x.AdditionalAttributeIds).HasMethod("gin");
    }
}
