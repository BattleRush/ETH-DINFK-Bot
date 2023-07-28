using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETHBot.DataLayer.Migrations
{
    public partial class InitialFood2050Addition : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*migrationBuilder.RenameTable("DiscordUserFavouriteRestaturants", null, "DiscordUserFavouriteRestaurants");

            migrationBuilder.AddColumn<bool>(
                name: "IsFood2050Supported",
                table: "Restaurants",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DirectMenuImageUrl",
                table: "Menus",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Food2050CO2Entries",
                columns: table => new
                {
                    Food2050CO2EntryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    CO2Delta = table.Column<double>(type: "double", nullable: false),
                    CO2Total = table.Column<double>(type: "double", nullable: false),
                    TemperatureChange = table.Column<double>(type: "double", nullable: false),
                    TemperatureChangeDelta = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Food2050CO2Entries", x => x.Food2050CO2EntryId);
                    table.ForeignKey(
                        name: "FK_Food2050CO2Entries_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "RestaurantId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");*/

            // deleting the wrong intex with typo
            /*migrationBuilder.DropIndex(
                name: "IX_DiscordUserFavouriteRestaturants_DiscordUserId",
                table: "DiscordUserFavouriteRestaturants");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordUserFavouriteRestaurants_DiscordUserId",
                table: "DiscordUserFavouriteRestaurants",
                column: "DiscordUserId");*/

            /*migrationBuilder.CreateIndex(
                name: "IX_Food2050CO2Entries_RestaurantId",
                table: "Food2050CO2Entries",
                column: "RestaurantId");*/
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscordUserFavouriteRestaurants");

            migrationBuilder.DropTable(
                name: "Food2050CO2Entries");

            migrationBuilder.DropColumn(
                name: "IsFood2050Supported",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "DirectMenuImageUrl",
                table: "Menus");

            migrationBuilder.CreateTable(
                name: "DiscordUserFavouriteRestaturants",
                columns: table => new
                {
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    DiscordUserId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordUserFavouriteRestaturants", x => new { x.RestaurantId, x.DiscordUserId });
                    table.ForeignKey(
                        name: "FK_DiscordUserFavouriteRestaturants_DiscordUsers_DiscordUserId",
                        column: x => x.DiscordUserId,
                        principalTable: "DiscordUsers",
                        principalColumn: "DiscordUserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscordUserFavouriteRestaturants_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "RestaurantId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordUserFavouriteRestaturants_DiscordUserId",
                table: "DiscordUserFavouriteRestaturants",
                column: "DiscordUserId");
        }
    }
}
