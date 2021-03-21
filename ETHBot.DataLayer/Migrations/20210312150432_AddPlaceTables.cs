using Microsoft.EntityFrameworkCore.Migrations;

namespace ETHBot.DataLayer.Migrations
{
    public partial class AddPlaceTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlaceBoardPixels",
                columns: table => new
                {
                    XPos = table.Column<int>(type: "INTEGER", nullable: false),
                    YPos = table.Column<int>(type: "INTEGER", nullable: false),
                    R = table.Column<byte>(type: "INTEGER", nullable: false),
                    G = table.Column<byte>(type: "INTEGER", nullable: false),
                    B = table.Column<byte>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceBoardPixels", x => new { x.XPos, x.YPos });
                });

            migrationBuilder.CreateTable(
                name: "PlaceBoardHistory",
                columns: table => new
                {
                    PlaceBoardHistoryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    XPos = table.Column<int>(type: "INTEGER", nullable: false),
                    YPos = table.Column<int>(type: "INTEGER", nullable: false),
                    R = table.Column<byte>(type: "INTEGER", nullable: false),
                    G = table.Column<byte>(type: "INTEGER", nullable: false),
                    B = table.Column<byte>(type: "INTEGER", nullable: false),
                    DiscordUserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    SnowflakeTimePlaced = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceBoardHistory", x => x.PlaceBoardHistoryId);
                    table.ForeignKey(
                        name: "FK_PlaceBoardHistory_DiscordUsers_DiscordUserId",
                        column: x => x.DiscordUserId,
                        principalTable: "DiscordUsers",
                        principalColumn: "DiscordUserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlaceBoardHistory_PlaceBoardPixels_XPos_YPos",
                        columns: x => new { x.XPos, x.YPos },
                        principalTable: "PlaceBoardPixels",
                        principalColumns: new[] { "XPos", "YPos" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlaceBoardHistory_DiscordUserId",
                table: "PlaceBoardHistory",
                column: "DiscordUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaceBoardHistory_XPos_YPos",
                table: "PlaceBoardHistory",
                columns: new[] { "XPos", "YPos" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlaceBoardHistory");

            migrationBuilder.DropTable(
                name: "PlaceBoardPixels");
        }
    }
}
