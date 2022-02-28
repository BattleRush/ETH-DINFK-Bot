using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETHBot.DataLayer.Migrations
{
    public partial class UpdatePingHistoryAndSavedMessages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SendInDM",
                table: "SavedMessages");

            migrationBuilder.AddColumn<bool>(
                name: "TriggeredByCommand",
                table: "SavedMessages",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<ulong>(
                name: "DMDiscordMessageId",
                table: "SavedMessages",
                type: "bigint unsigned",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReplyCount",
                table: "PingStatistics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReplyCountBot",
                table: "PingStatistics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsReply",
                table: "PingHistory",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DMDiscordMessageId",
                table: "SavedMessages");

            migrationBuilder.DropColumn(
                name: "ReplyCount",
                table: "PingStatistics");

            migrationBuilder.DropColumn(
                name: "ReplyCountBot",
                table: "PingStatistics");

            migrationBuilder.DropColumn(
                name: "IsReply",
                table: "PingHistory");

            migrationBuilder.DropColumn(
                name: "TriggeredByCommand",
                table: "SavedMessages");

            migrationBuilder.AddColumn<bool>(
                name: "SendInDM",
                table: "SavedMessages",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
