using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pw.Hub.Relics.Domain.Entities;

namespace Pw.Hub.Relics.Infrastructure.Data.Configurations;

public class NotificationFilterConfiguration : IEntityTypeConfiguration<NotificationFilter>
{
    public void Configure(EntityTypeBuilder<NotificationFilter> builder)
    {
        builder.ToTable("notification_filters");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.IsEnabled)
            .HasColumnName("is_enabled")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.TelegramChatId)
            .HasColumnName("telegram_chat_id")
            .IsRequired();

        builder.Property(x => x.SoulType)
            .HasColumnName("soul_type");

        builder.Property(x => x.SlotTypeId)
            .HasColumnName("slot_type_id");

        builder.Property(x => x.Race)
            .HasColumnName("race");

        builder.Property(x => x.SoulLevel)
            .HasColumnName("soul_level");

        builder.Property(x => x.MainAttributeId)
            .HasColumnName("main_attribute_id");

        builder.Property(x => x.RequiredAdditionalAttributeIds)
            .HasColumnName("required_additional_attribute_ids")
            .HasColumnType("integer[]");

        builder.Property(x => x.MinPrice)
            .HasColumnName("min_price");

        builder.Property(x => x.MaxPrice)
            .HasColumnName("max_price");

        builder.Property(x => x.ServerId)
            .HasColumnName("server_id");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Indexes
        builder.HasIndex(x => new { x.UserId, x.IsEnabled });
    }
}
