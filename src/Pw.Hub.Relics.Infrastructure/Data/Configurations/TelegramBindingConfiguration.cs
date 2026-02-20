using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pw.Hub.Relics.Domain.Entities;

namespace Pw.Hub.Relics.Infrastructure.Data.Configurations;

public class TelegramBindingConfiguration : IEntityTypeConfiguration<TelegramBinding>
{
    public void Configure(EntityTypeBuilder<TelegramBinding> builder)
    {
        builder.ToTable("telegram_bindings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.TelegramChatId)
            .HasColumnName("telegram_chat_id");

        builder.Property(x => x.TelegramUsername)
            .HasColumnName("telegram_username")
            .HasMaxLength(100);

        builder.Property(x => x.LinkToken)
            .HasColumnName("link_token")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.TokenExpiresAt)
            .HasColumnName("token_expires_at")
            .IsRequired();

        builder.Property(x => x.IsConfirmed)
            .HasColumnName("is_confirmed")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Indexes
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasIndex(x => x.LinkToken).IsUnique();
        builder.HasIndex(x => x.TelegramChatId)
            .HasFilter("telegram_chat_id IS NOT NULL")
            .IsUnique();
    }
}
