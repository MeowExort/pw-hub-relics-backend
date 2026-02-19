using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pw.Hub.Relics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRelicListingIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_relic_listings_seller_character_id_shop_position_server_id",
                table: "relic_listings");

            migrationBuilder.CreateIndex(
                name: "IX_relic_listings_seller_character_id_shop_position_server_id_~",
                table: "relic_listings",
                columns: new[] { "seller_character_id", "shop_position", "server_id", "relic_definition_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_relic_listings_seller_character_id_shop_position_server_id_~",
                table: "relic_listings");

            migrationBuilder.CreateIndex(
                name: "IX_relic_listings_seller_character_id_shop_position_server_id",
                table: "relic_listings",
                columns: new[] { "seller_character_id", "shop_position", "server_id" },
                unique: true);
        }
    }
}
