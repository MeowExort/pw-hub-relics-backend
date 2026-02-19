using Microsoft.EntityFrameworkCore;
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

        builder.HasOne(x => x.SlotType)
            .WithMany()
            .HasForeignKey(x => x.SlotTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.SoulLevel, x.SoulType, x.SlotTypeId, x.Race });
    }
}
