using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Pw.Hub.Relics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attribute_definitions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attribute_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "enhancement_curve",
                columns: table => new
                {
                    level = table.Column<int>(type: "integer", nullable: false),
                    required_experience = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enhancement_curve", x => x.level);
                });

            migrationBuilder.CreateTable(
                name: "notification_filters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    telegram_chat_id = table.Column<long>(type: "bigint", nullable: false),
                    soul_type = table.Column<int>(type: "integer", nullable: true),
                    slot_type_id = table.Column<int>(type: "integer", nullable: true),
                    race = table.Column<int>(type: "integer", nullable: true),
                    soul_level = table.Column<int>(type: "integer", nullable: true),
                    main_attribute_id = table.Column<int>(type: "integer", nullable: true),
                    required_additional_attribute_ids = table.Column<List<int>>(type: "integer[]", nullable: false),
                    min_price = table.Column<long>(type: "bigint", nullable: true),
                    max_price = table.Column<long>(type: "bigint", nullable: true),
                    server_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_filters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "price_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    relic_definition_id = table.Column<int>(type: "integer", nullable: false),
                    main_attribute_id = table.Column<int>(type: "integer", nullable: false),
                    additional_attribute_ids = table.Column<List<int>>(type: "integer[]", nullable: false),
                    price = table.Column<long>(type: "bigint", nullable: false),
                    server_id = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_history", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "server_definitions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "slot_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_slot_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "relic_definitions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    soul_level = table.Column<int>(type: "integer", nullable: false),
                    soul_type = table.Column<int>(type: "integer", nullable: false),
                    slot_type_id = table.Column<int>(type: "integer", nullable: false),
                    race = table.Column<int>(type: "integer", nullable: false),
                    icon_uri = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_relic_definitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_relic_definitions_slot_types_slot_type_id",
                        column: x => x.slot_type_id,
                        principalTable: "slot_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "relic_listings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    relic_definition_id = table.Column<int>(type: "integer", nullable: false),
                    absorb_experience = table.Column<int>(type: "integer", nullable: false),
                    enhancement_level = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    seller_character_id = table.Column<long>(type: "bigint", nullable: false),
                    shop_position = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<long>(type: "bigint", nullable: false),
                    server_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sold_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_relic_listings", x => x.id);
                    table.ForeignKey(
                        name: "FK_relic_listings_relic_definitions_relic_definition_id",
                        column: x => x.relic_definition_id,
                        principalTable: "relic_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_relic_listings_server_definitions_server_id",
                        column: x => x.server_id,
                        principalTable: "server_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "relic_attributes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    relic_listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attribute_definition_id = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<int>(type: "integer", nullable: false),
                    category = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.InsertData(
                table: "attribute_definitions",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 0, "Физическая атака" },
                    { 3, "Магическая атака" },
                    { 12, "Защита" },
                    { 14, "Магическая защита" },
                    { 35, "Здоровье" },
                    { 36, "Мана" },
                    { 46, "Меткость" },
                    { 50, "Уклонение" },
                    { 59, "Показатель атаки" },
                    { 60, "Показатель защиты" },
                    { 160, "Боевой дух" }
                });

            migrationBuilder.InsertData(
                table: "server_definitions",
                columns: new[] { "id", "key", "name" },
                values: new object[,]
                {
                    { 2, "centaur", "Центавр" },
                    { 3, "alkor", "Алькор" },
                    { 5, "mizar", "Мицар" },
                    { 29, "capella", "Капелла" }
                });

            migrationBuilder.InsertData(
                table: "slot_types",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "Цветок сострадания" },
                    { 2, "Самоцвет скромности" },
                    { 3, "Зеркало честности" },
                    { 4, "Замок дисциплины" },
                    { 5, "Колокол праведности" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_filters_user_id_is_enabled",
                table: "notification_filters",
                columns: new[] { "user_id", "is_enabled" });

            migrationBuilder.CreateIndex(
                name: "IX_price_history_additional_attribute_ids",
                table: "price_history",
                column: "additional_attribute_ids")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_price_history_main_attribute_id",
                table: "price_history",
                column: "main_attribute_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_history_relic_definition_id",
                table: "price_history",
                column: "relic_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_history_timestamp",
                table: "price_history",
                column: "timestamp",
                descending: new bool[0]);

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

            migrationBuilder.CreateIndex(
                name: "IX_relic_definitions_slot_type_id",
                table: "relic_definitions",
                column: "slot_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_relic_definitions_soul_level_soul_type_slot_type_id_race",
                table: "relic_definitions",
                columns: new[] { "soul_level", "soul_type", "slot_type_id", "race" });

            migrationBuilder.CreateIndex(
                name: "IX_relic_listings_created_at",
                table: "relic_listings",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_relic_listings_is_active_created_at",
                table: "relic_listings",
                columns: new[] { "is_active", "created_at" },
                filter: "is_active = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_relic_listings_price",
                table: "relic_listings",
                column: "price");

            migrationBuilder.CreateIndex(
                name: "IX_relic_listings_relic_definition_id",
                table: "relic_listings",
                column: "relic_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_relic_listings_seller_character_id_shop_position_server_id",
                table: "relic_listings",
                columns: new[] { "seller_character_id", "shop_position", "server_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_relic_listings_server_id",
                table: "relic_listings",
                column: "server_id");

            migrationBuilder.CreateIndex(
                name: "IX_server_definitions_key",
                table: "server_definitions",
                column: "key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "enhancement_curve");

            migrationBuilder.DropTable(
                name: "notification_filters");

            migrationBuilder.DropTable(
                name: "price_history");

            migrationBuilder.DropTable(
                name: "relic_attributes");

            migrationBuilder.DropTable(
                name: "attribute_definitions");

            migrationBuilder.DropTable(
                name: "relic_listings");

            migrationBuilder.DropTable(
                name: "relic_definitions");

            migrationBuilder.DropTable(
                name: "server_definitions");

            migrationBuilder.DropTable(
                name: "slot_types");
        }
    }
}
