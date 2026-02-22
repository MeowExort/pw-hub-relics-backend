using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pw.Hub.Relics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramBindingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "notification_frequency",
                table: "telegram_bindings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "quiet_hours_enabled",
                table: "telegram_bindings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "quiet_hours_end",
                table: "telegram_bindings",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "quiet_hours_start",
                table: "telegram_bindings",
                type: "time without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "notification_frequency",
                table: "telegram_bindings");

            migrationBuilder.DropColumn(
                name: "quiet_hours_enabled",
                table: "telegram_bindings");

            migrationBuilder.DropColumn(
                name: "quiet_hours_end",
                table: "telegram_bindings");

            migrationBuilder.DropColumn(
                name: "quiet_hours_start",
                table: "telegram_bindings");
        }
    }
}
