using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETHBot.DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class EmoteServerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "DiscordServerId",
                table: "DiscordEmotes",
                type: "bigint unsigned",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscordEmotes_DiscordServerId",
                table: "DiscordEmotes",
                column: "DiscordServerId");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscordEmotes_DiscordServers_DiscordServerId",
                table: "DiscordEmotes",
                column: "DiscordServerId",
                principalTable: "DiscordServers",
                principalColumn: "DiscordServerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscordEmotes_DiscordServers_DiscordServerId",
                table: "DiscordEmotes");

            migrationBuilder.DropIndex(
                name: "IX_DiscordEmotes_DiscordServerId",
                table: "DiscordEmotes");

            migrationBuilder.DropColumn(
                name: "DiscordServerId",
                table: "DiscordEmotes");
        }
    }
}
