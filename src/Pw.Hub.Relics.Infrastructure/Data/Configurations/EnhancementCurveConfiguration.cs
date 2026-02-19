using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pw.Hub.Relics.Domain.Entities;

namespace Pw.Hub.Relics.Infrastructure.Data.Configurations;

public class EnhancementCurveConfiguration : IEntityTypeConfiguration<EnhancementCurve>
{
    public void Configure(EntityTypeBuilder<EnhancementCurve> builder)
    {
        builder.ToTable("enhancement_curve");

        builder.HasKey(x => x.Level);

        builder.Property(x => x.Level)
            .HasColumnName("level")
            .ValueGeneratedNever();

        builder.Property(x => x.RequiredExperience)
            .HasColumnName("required_experience")
            .IsRequired();

        // Seed data
        builder.HasData(
            new EnhancementCurve { Level = 1, RequiredExperience =  200},
            new EnhancementCurve { Level = 2, RequiredExperience =  275},
            new EnhancementCurve { Level = 3, RequiredExperience = 400 },
            new EnhancementCurve { Level = 4, RequiredExperience =  625},
            new EnhancementCurve { Level = 5, RequiredExperience =  900},
            new EnhancementCurve { Level = 6, RequiredExperience =  1200},
            new EnhancementCurve { Level = 7, RequiredExperience =  1775},
            new EnhancementCurve { Level = 8, RequiredExperience =  2625},
            new EnhancementCurve { Level = 9, RequiredExperience =  3675},
            new EnhancementCurve { Level = 10, RequiredExperience =  5725},
            new EnhancementCurve { Level = 11, RequiredExperience =  7450},
            new EnhancementCurve { Level = 12, RequiredExperience =  10150},
            new EnhancementCurve { Level = 13, RequiredExperience =  14125},
            new EnhancementCurve { Level = 14, RequiredExperience = 18075},
            new EnhancementCurve { Level = 15, RequiredExperience =  23530},
            new EnhancementCurve { Level = 16, RequiredExperience =  29270},
            new EnhancementCurve { Level = 17, RequiredExperience =  31050},
            new EnhancementCurve { Level = 18, RequiredExperience =  35100},
            new EnhancementCurve { Level = 19, RequiredExperience =  39025},
            new EnhancementCurve { Level = 20, RequiredExperience =  44825}
        );
    }
}