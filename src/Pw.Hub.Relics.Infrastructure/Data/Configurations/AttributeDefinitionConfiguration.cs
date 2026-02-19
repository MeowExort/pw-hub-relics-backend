using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pw.Hub.Relics.Domain.Entities;

namespace Pw.Hub.Relics.Infrastructure.Data.Configurations;

public class AttributeDefinitionConfiguration : IEntityTypeConfiguration<AttributeDefinition>
{
    public void Configure(EntityTypeBuilder<AttributeDefinition> builder)
    {
        builder.ToTable("attribute_definitions");

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
            new AttributeDefinition { Id = 0, Name = "Физическая атака" },
            new AttributeDefinition { Id = 3, Name = "Магическая атака" },
            new AttributeDefinition { Id = 12, Name = "Защита" },
            new AttributeDefinition { Id = 14, Name = "Магическая защита" },
            new AttributeDefinition { Id = 35, Name = "Здоровье" },
            new AttributeDefinition { Id = 36, Name = "Мана" },
            new AttributeDefinition { Id = 46, Name = "Меткость" },
            new AttributeDefinition { Id = 50, Name = "Уклонение" },
            new AttributeDefinition { Id = 59, Name = "Показатель атаки" },
            new AttributeDefinition { Id = 60, Name = "Показатель защиты" },
            new AttributeDefinition { Id = 160, Name = "Боевой дух" }
        );
    }
}
