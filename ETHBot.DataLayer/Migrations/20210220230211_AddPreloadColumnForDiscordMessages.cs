using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ETHBot.DataLayer.Migrations
{
    public partial class AddPreloadColumnForDiscordMessages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*migrationBuilder.DropTable(
                name: "EmojiHistory");

            migrationBuilder.DropTable(
                name: "EmojiStatistics");*/

            migrationBuilder.AddColumn<bool>(
                name: "Preloaded",
                table: "DiscordMessages",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Preloaded",
                table: "DiscordMessages");

            migrationBuilder.CreateTable(
                name: "EmojiStatistics",
                columns: table => new
                {
                    EmojiInfoId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Animated = table.Column<bool>(type: "INTEGER", nullable: false),
                    Blocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EmojiId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    EmojiName = table.Column<string>(type: "TEXT", nullable: true),
                    FallbackEmojiId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ImageData = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Url = table.Column<string>(type: "TEXT", nullable: true),
                    UsedAsReaction = table.Column<int>(type: "INTEGER", nullable: false),
                    UsedByBots = table.Column<int>(type: "INTEGER", nullable: false),
                    UsedInText = table.Column<int>(type: "INTEGER", nullable: false),
                    UsedInTextOnce = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmojiStatistics", x => x.EmojiInfoId);
                });

            migrationBuilder.CreateTable(
                name: "EmojiHistory",
                columns: table => new
                {
                    EmojiHistoryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    DateTimePosted = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EmojiStatisticId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsBot = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsReaction = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmojiHistory", x => x.EmojiHistoryId);
                    table.ForeignKey(
                        name: "FK_EmojiHistory_EmojiStatistics_EmojiStatisticId",
                        column: x => x.EmojiStatisticId,
                        principalTable: "EmojiStatistics",
                        principalColumn: "EmojiInfoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmojiHistory_EmojiStatisticId",
                table: "EmojiHistory",
                column: "EmojiStatisticId");
        }
    }
}
