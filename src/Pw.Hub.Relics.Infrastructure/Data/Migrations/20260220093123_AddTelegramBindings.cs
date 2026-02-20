using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pw.Hub.Relics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramBindings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "telegram_bindings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    telegram_chat_id = table.Column<long>(type: "bigint", nullable: true),
                    telegram_username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    link_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_confirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telegram_bindings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_telegram_bindings_link_token",
                table: "telegram_bindings",
                column: "link_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_telegram_bindings_telegram_chat_id",
                table: "telegram_bindings",
                column: "telegram_chat_id",
                unique: true,
                filter: "telegram_chat_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_telegram_bindings_user_id",
                table: "telegram_bindings",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "telegram_bindings");
        }
    }
}
