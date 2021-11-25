using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETHBot.DataLayer.Migrations
{
    public partial class FixBrokenTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<bool>(
            //    name: "IsCategory",
            //    table: "DiscordChannels",
            //    type: "tinyint(1)",
            //    nullable: false,
            //    defaultValue: false);

            //migrationBuilder.AddColumn<ulong>(
            //    name: "ParentDiscordChannelId",
            //    table: "DiscordChannels",
            //    type: "bigint unsigned",
            //    nullable: true);

            //migrationBuilder.AddColumn<int>(
            //    name: "Position",
            //    table: "DiscordChannels",
            //    type: "int",
            //    nullable: false,
            //    defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DiscordChannels_ParentDiscordChannelId",
                table: "DiscordChannels",
                column: "ParentDiscordChannelId");

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

            migrationBuilder.DropIndex(
                name: "IX_DiscordChannels_ParentDiscordChannelId",
                table: "DiscordChannels");

            migrationBuilder.DropColumn(
                name: "IsCategory",
                table: "DiscordChannels");

            migrationBuilder.DropColumn(
                name: "ParentDiscordChannelId",
                table: "DiscordChannels");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "DiscordChannels");
        }
    }
}
