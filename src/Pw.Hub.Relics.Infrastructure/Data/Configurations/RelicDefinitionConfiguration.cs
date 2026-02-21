using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pw.Hub.Relics.Domain.Entities;

namespace Pw.Hub.Relics.Infrastructure.Data.Configurations;

public class RelicDefinitionConfiguration : IEntityTypeConfiguration<RelicDefinition>
{
    public void Configure(EntityTypeBuilder<RelicDefinition> builder)
    {
        builder.ToTable("relic_definitions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.SoulLevel)
            .HasColumnName("soul_level")
            .IsRequired();

        builder.Property(x => x.SoulType)
            .HasColumnName("soul_type")
            .IsRequired();

        builder.Property(x => x.SlotTypeId)
            .HasColumnName("slot_type_id")
            .IsRequired();

        builder.Property(x => x.Race)
            .HasColumnName("race")
            .IsRequired();

        builder.Property(x => x.IconUri)
            .HasColumnName("icon_uri")
            .HasMaxLength(500);

        builder.Property(x => x.MainAttributeScaling)
            .HasColumnName("main_attribute_scaling")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<int, int>>(v, (JsonSerializerOptions?)null),
                new ValueComparer<Dictionary<int, int>?>(
                    (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.Count == c2.Count && !c1.Except(c2).Any()),
                    c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c == null ? null : new Dictionary<int, int>(c)));

        builder.HasOne(x => x.SlotType)
            .WithMany()
            .HasForeignKey(x => x.SlotTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.SoulLevel, x.SoulType, x.SlotTypeId, x.Race });
    }
}
