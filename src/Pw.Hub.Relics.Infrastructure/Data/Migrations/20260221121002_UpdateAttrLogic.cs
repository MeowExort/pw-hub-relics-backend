using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Pw.Hub.Relics.Domain.Entities;

#nullable disable

namespace Pw.Hub.Relics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAttrLogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "relic_attributes");

            migrationBuilder.AddColumn<List<RelicAttributeDto>>(
                name: "json_attributes",
                table: "relic_listings",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.CreateIndex(
                name: "IX_RelicListings_Lookup_Covering",
                table: "relic_listings",
                columns: new[] { "server_id", "seller_character_id", "shop_position" })
                .Annotation("Npgsql:IndexInclude", new[] { "attributes_hash", "xmin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RelicListings_Lookup_Covering",
                table: "relic_listings");

            migrationBuilder.DropColumn(
                name: "json_attributes",
                table: "relic_listings");

            migrationBuilder.CreateTable(
                name: "relic_attributes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    attribute_definition_id = table.Column<int>(type: "integer", nullable: false),
                    category = table.Column<int>(type: "integer", nullable: false),
                    relic_listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_relic_attributes", x => x.id);
                    table.ForeignKey(
                        name: "FK_relic_attributes_attribute_definitions_attribute_definition~",
                        column: x => x.attribute_definition_id,
                        principalTable: "attribute_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_relic_attributes_relic_listings_relic_listing_id",
                        column: x => x.relic_listing_id,
                        principalTable: "relic_listings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_relic_attributes_attribute_definition_id",
                table: "relic_attributes",
                column: "attribute_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_relic_attributes_category",
                table: "relic_attributes",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_relic_attributes_relic_listing_id",
                table: "relic_attributes",
                column: "relic_listing_id");
        }
    }
}
