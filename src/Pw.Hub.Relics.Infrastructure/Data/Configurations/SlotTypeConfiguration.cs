using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pw.Hub.Relics.Domain.Entities;

namespace Pw.Hub.Relics.Infrastructure.Data.Configurations;

public class SlotTypeConfiguration : IEntityTypeConfiguration<SlotType>
{
    public void Configure(EntityTypeBuilder<SlotType> builder)
    {
        builder.ToTable("slot_types");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();
        
        // Seed data
        builder.HasData(
            new SlotType { Id = 1, Name = "Цветок сострадания" },
            new SlotType { Id = 2, Name = "Самоцвет скромности" },
            new SlotType { Id = 3, Name = "Зеркало честности" },
            new SlotType { Id = 4, Name = "Замок дисциплины" },
            new SlotType { Id = 5, Name = "Колокол праведности" }
        );
    }
}
