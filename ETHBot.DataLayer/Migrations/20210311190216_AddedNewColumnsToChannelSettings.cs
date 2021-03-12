using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ETHBot.DataLayer.Migrations
{
    public partial class AddedNewColumnsToChannelSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*migrationBuilder.DropForeignKey(
                name: "FK_PingHistory_DiscordMessages_MessageId",
                table: "PingHistory");*/

            /*migrationBuilder.AlterColumn<ulong>(
                name: "MessageId",
                table: "PingHistory",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(ulong),
                oldType: "INTEGER");*/

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NewestPostTimePreloaded",
                table: "BotChannelSettings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OldestPostTimePreloaded",
                table: "BotChannelSettings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReachedOldestPreload",
                table: "BotChannelSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "BotSetting",
                columns: table => new
                {
                    BotSettingId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SpaceXSubredditCheckCronJob = table.Column<string>(type: "TEXT", nullable: true),
                    LastSpaceXRedditPost = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSetting", x => x.BotSettingId);
                });

            /*migrationBuilder.AddForeignKey(
                name: "FK_PingHistory_DiscordMessages_MessageId",
                table: "PingHistory",
                column: "MessageId",
                principalTable: "DiscordMessages",
                principalColumn: "MessageId",
                onDelete: ReferentialAction.Restrict);*/
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            /*migrationBuilder.DropForeignKey(
                name: "FK_PingHistory_DiscordMessages_MessageId",
                table: "PingHistory");*/

            migrationBuilder.DropTable(
                name: "BotSetting");

            migrationBuilder.DropColumn(
                name: "NewestPostTimePreloaded",
                table: "BotChannelSettings");

            migrationBuilder.DropColumn(
                name: "OldestPostTimePreloaded",
                table: "BotChannelSettings");

            migrationBuilder.DropColumn(
                name: "ReachedOldestPreload",
                table: "BotChannelSettings");

            /*migrationBuilder.AlterColumn<ulong>(
                name: "MessageId",
                table: "PingHistory",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0ul,
                oldClrType: typeof(ulong),
                oldType: "INTEGER",
                oldNullable: true);*/
            /*
            migrationBuilder.AddForeignKey(
                name: "FK_PingHistory_DiscordMessages_MessageId",
                table: "PingHistory",
                column: "MessageId",
                principalTable: "DiscordMessages",
                principalColumn: "MessageId",
                onDelete: ReferentialAction.Cascade);*/
        }
    }
}
