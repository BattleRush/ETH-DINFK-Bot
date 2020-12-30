using Microsoft.EntityFrameworkCore.Migrations;

namespace ETHBot.DataLayer.Migrations
{
    public partial class AddRantTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RantTypes",
                columns: table => new
                {
                    RantTypeId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RantTypes", x => x.RantTypeId);
                });

            migrationBuilder.CreateTable(
                name: "RantMessages",
                columns: table => new
                {
                    RantMessageId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RantTypeId = table.Column<int>(nullable: false),
                    Content = table.Column<string>(nullable: true),
                    DiscordChannelId = table.Column<ulong>(nullable: false),
                    DiscordUserId = table.Column<ulong>(nullable: false),
                    DiscordMessageId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RantMessages", x => x.RantMessageId);
                    table.ForeignKey(
                        name: "FK_RantMessages_DiscordChannels_DiscordChannelId",
                        column: x => x.DiscordChannelId,
                        principalTable: "DiscordChannels",
                        principalColumn: "DiscordChannelId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RantMessages_DiscordMessages_DiscordMessageId",
                        column: x => x.DiscordMessageId,
                        principalTable: "DiscordMessages",
                        principalColumn: "MessageId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RantMessages_DiscordUsers_DiscordUserId",
                        column: x => x.DiscordUserId,
                        principalTable: "DiscordUsers",
                        principalColumn: "DiscordUserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RantMessages_RantTypes_RantTypeId",
                        column: x => x.RantTypeId,
                        principalTable: "RantTypes",
                        principalColumn: "RantTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RantMessages_DiscordChannelId",
                table: "RantMessages",
                column: "DiscordChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_RantMessages_DiscordMessageId",
                table: "RantMessages",
                column: "DiscordMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_RantMessages_DiscordUserId",
                table: "RantMessages",
                column: "DiscordUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RantMessages_RantTypeId",
                table: "RantMessages",
                column: "RantTypeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RantMessages");

            migrationBuilder.DropTable(
                name: "RantTypes");
        }
    }
}
