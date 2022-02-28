using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETHBot.DataLayer.Migrations
{
    public partial class AddDiscordThreads : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "DiscordThreadId",
                table: "DiscordMessages",
                type: "bigint unsigned",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DiscordThreads",
                columns: table => new
                {
                    DiscordThreadId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ThreadName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsArchived = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsLocked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsNsfw = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsPrivateThread = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ThreadType = table.Column<int>(type: "int", nullable: false),
                    MemberCount = table.Column<int>(type: "int", nullable: false),
                    DiscordChannelId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordThreads", x => x.DiscordThreadId);
                    table.ForeignKey(
                        name: "FK_DiscordThreads_DiscordChannels_DiscordChannelId",
                        column: x => x.DiscordChannelId,
                        principalTable: "DiscordChannels",
                        principalColumn: "DiscordChannelId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordMessages_DiscordThreadId",
                table: "DiscordMessages",
                column: "DiscordThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordThreads_DiscordChannelId",
                table: "DiscordThreads",
                column: "DiscordChannelId");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscordMessages_DiscordThreads_DiscordThreadId",
                table: "DiscordMessages",
                column: "DiscordThreadId",
                principalTable: "DiscordThreads",
                principalColumn: "DiscordThreadId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscordMessages_DiscordThreads_DiscordThreadId",
                table: "DiscordMessages");

            migrationBuilder.DropTable(
                name: "DiscordThreads");

            migrationBuilder.DropIndex(
                name: "IX_DiscordMessages_DiscordThreadId",
                table: "DiscordMessages");

            migrationBuilder.DropColumn(
                name: "DiscordThreadId",
                table: "DiscordMessages");
        }
    }
}
