using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pw.Hub.Relics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAttributesHashToRelicListing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "attributes_hash",
                table: "relic_listings",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RelicListings_Lookup",
                table: "relic_listings",
                columns: new[] { "server_id", "seller_character_id", "shop_position", "attributes_hash" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RelicListings_Lookup",
                table: "relic_listings");

            migrationBuilder.DropColumn(
                name: "attributes_hash",
                table: "relic_listings");
        }
    }
}
