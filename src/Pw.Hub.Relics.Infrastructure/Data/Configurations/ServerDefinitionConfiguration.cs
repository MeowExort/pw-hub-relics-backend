using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pw.Hub.Relics.Domain.Entities;

namespace Pw.Hub.Relics.Infrastructure.Data.Configurations;

public class ServerDefinitionConfiguration : IEntityTypeConfiguration<ServerDefinition>
{
    public void Configure(EntityTypeBuilder<ServerDefinition> builder)
    {
        builder.ToTable("server_definitions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Key)
            .HasColumnName("key")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.Key).IsUnique();

        // Seed data
        builder.HasData(
            new ServerDefinition { Id = 2, Name = "Центавр", Key = "centaur" },
            new ServerDefinition { Id = 3, Name = "Алькор", Key = "alkor" },
            new ServerDefinition { Id = 5, Name = "Мицар", Key = "mizar" },
            new ServerDefinition { Id = 29, Name = "Капелла", Key = "capella" }
        );
    }
}
