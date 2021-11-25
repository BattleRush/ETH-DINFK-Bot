using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETHBot.DataLayer.Migrations
{
    public partial class UpdateDiscordChannelsIncludeCategories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscordChannels_DiscordServers_ParentDiscordChannelId",
                table: "DiscordChannels");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscordChannels_DiscordChannels_ParentDiscordChannelId",
                table: "DiscordChannels",
                column: "ParentDiscordChannelId",
                principalTable: "DiscordChannels",
                principalColumn: "DiscordChannelId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscordChannels_DiscordChannels_ParentDiscordChannelId",
                table: "DiscordChannels");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscordChannels_DiscordServers_ParentDiscordChannelId",
                table: "DiscordChannels",
                column: "ParentDiscordChannelId",
                principalTable: "DiscordServers",
                principalColumn: "DiscordServerId");
        }
    }
}
