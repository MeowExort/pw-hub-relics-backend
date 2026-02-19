using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Pw.Hub.Relics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEnhCurve : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "enhancement_curve",
                columns: new[] { "level", "required_experience" },
                values: new object[,]
                {
                    { 1, 200 },
                    { 2, 275 },
                    { 3, 400 },
                    { 4, 625 },
                    { 5, 900 },
                    { 6, 1200 },
                    { 7, 1775 },
                    { 8, 2625 },
                    { 9, 3675 },
                    { 10, 5725 },
                    { 11, 7450 },
                    { 12, 10150 },
                    { 13, 14125 },
                    { 14, 18075 },
                    { 15, 23530 },
                    { 16, 29270 },
                    { 17, 31050 },
                    { 18, 35100 },
                    { 19, 39025 },
                    { 20, 44825 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "enhancement_curve",
                keyColumn: "level",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "enhancement_curve",
                keyColumn: "level",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "enhancement_curve",
                keyColumn: "level",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "enhancement_curve",
                keyColumn: "level",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "enhancement_curve",
                keyColumn: "level",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "enhancement_curve",
                keyColumn: "level",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "enhancement_curve",
                keyColumn: "level",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "enhancement_curve",
                keyColumn: "level",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "enhancement_curve",
                keyColumn: "level",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "enhancement_curve",
                keyColumn: "level",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "enhancement_curve",
                keyColumn: "level",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "enhancement_curve",
                keyColumn: "level",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "enhancement_curve",
                keyColumn: "level",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "enhancement_curve",
                keyColumn: "level",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "enhancement_curve",
                keyColumn: "level",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "enhancement_curve",
                keyColumn: "level",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "enhancement_curve",
                keyColumn: "level",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "enhancement_curve",
                keyColumn: "level",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "enhancement_curve",
                keyColumn: "level",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "enhancement_curve",
                keyColumn: "level",
                keyValue: 20);
        }
    }
}
