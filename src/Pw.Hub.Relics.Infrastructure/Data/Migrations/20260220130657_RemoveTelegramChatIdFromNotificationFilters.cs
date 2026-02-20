using Microsoft.EntityFrameworkCore.Migrations;

namespace Pw.Hub.Relics.Infrastructure.Data.Migrations;

public partial class RemoveTelegramChatIdFromNotificationFilters : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "telegram_chat_id",
            table: "notification_filters");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "telegram_chat_id",
            table: "notification_filters",
            type: "bigint",
            nullable: false,
            defaultValue: 0L);
    }
}
