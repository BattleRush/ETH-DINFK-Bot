using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETHBot.DataLayer.Migrations
{
    public partial class AddFavouriteEmotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FavouriteDiscordEmotes",
                columns: table => new
                {
                    DiscordEmoteId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    DiscordUserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavouriteDiscordEmotes", x => new { x.DiscordEmoteId, x.DiscordUserId });
                    table.ForeignKey(
                        name: "FK_FavouriteDiscordEmotes_DiscordEmotes_DiscordEmoteId",
                        column: x => x.DiscordEmoteId,
                        principalTable: "DiscordEmotes",
                        principalColumn: "DiscordEmoteId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FavouriteDiscordEmotes_DiscordUsers_DiscordUserId",
                        column: x => x.DiscordUserId,
                        principalTable: "DiscordUsers",
                        principalColumn: "DiscordUserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_FavouriteDiscordEmotes_DiscordUserId",
                table: "FavouriteDiscordEmotes",
                column: "DiscordUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FavouriteDiscordEmotes");
        }
    }
}
