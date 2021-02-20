using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ETHBot.DataLayer.Migrations
{
    public partial class StartMigrationToNewEmoteTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "ReplyMessageId",
                table: "DiscordMessages",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DiscordEmotes",
                columns: table => new
                {
                    DiscordEmoteId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    EmoteName = table.Column<string>(type: "TEXT", nullable: true),
                    Animated = table.Column<bool>(type: "INTEGER", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: true),
                    LocalPath = table.Column<string>(type: "TEXT", nullable: true),
                    Blocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordEmotes", x => x.DiscordEmoteId);
                });

            migrationBuilder.CreateTable(
                name: "DiscordEmoteHistory",
                columns: table => new
                {
                    EmoteHistoryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsReaction = table.Column<bool>(type: "INTEGER", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    DateTimePosted = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DiscordEmoteId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    DiscordUserId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    DiscordMessageId = table.Column<ulong>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordEmoteHistory", x => x.EmoteHistoryId);
                    table.ForeignKey(
                        name: "FK_DiscordEmoteHistory_DiscordEmotes_DiscordEmoteId",
                        column: x => x.DiscordEmoteId,
                        principalTable: "DiscordEmotes",
                        principalColumn: "DiscordEmoteId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscordEmoteHistory_DiscordMessages_DiscordMessageId",
                        column: x => x.DiscordMessageId,
                        principalTable: "DiscordMessages",
                        principalColumn: "MessageId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiscordEmoteHistory_DiscordUsers_DiscordUserId",
                        column: x => x.DiscordUserId,
                        principalTable: "DiscordUsers",
                        principalColumn: "DiscordUserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DiscordEmoteStatistics",
                columns: table => new
                {
                    DiscordEmoteId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UsedAsReaction = table.Column<int>(type: "INTEGER", nullable: false),
                    UsedInText = table.Column<int>(type: "INTEGER", nullable: false),
                    UsedInTextOnce = table.Column<int>(type: "INTEGER", nullable: false),
                    UsedByBots = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordEmoteStatistics", x => x.DiscordEmoteId);
                    table.ForeignKey(
                        name: "FK_DiscordEmoteStatistics_DiscordEmotes_DiscordEmoteId",
                        column: x => x.DiscordEmoteId,
                        principalTable: "DiscordEmotes",
                        principalColumn: "DiscordEmoteId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscordMessages_ReplyMessageId",
                table: "DiscordMessages",
                column: "ReplyMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordEmoteHistory_DiscordEmoteId",
                table: "DiscordEmoteHistory",
                column: "DiscordEmoteId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordEmoteHistory_DiscordMessageId",
                table: "DiscordEmoteHistory",
                column: "DiscordMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordEmoteHistory_DiscordUserId",
                table: "DiscordEmoteHistory",
                column: "DiscordUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscordMessages_DiscordMessages_ReplyMessageId",
                table: "DiscordMessages",
                column: "ReplyMessageId",
                principalTable: "DiscordMessages",
                principalColumn: "MessageId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscordMessages_DiscordMessages_ReplyMessageId",
                table: "DiscordMessages");

            migrationBuilder.DropTable(
                name: "DiscordEmoteHistory");

            migrationBuilder.DropTable(
                name: "DiscordEmoteStatistics");

            migrationBuilder.DropTable(
                name: "DiscordEmotes");

            migrationBuilder.DropIndex(
                name: "IX_DiscordMessages_ReplyMessageId",
                table: "DiscordMessages");

            migrationBuilder.DropColumn(
                name: "ReplyMessageId",
                table: "DiscordMessages");
        }
    }
}
