using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETHBot.DataLayer.Migrations
{
    public partial class ExtendDiscordUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Banned",
                table: "DiscordUsers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NoTrack",
                table: "DiscordUsers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Banned",
                table: "DiscordUsers");

            migrationBuilder.DropColumn(
                name: "NoTrack",
                table: "DiscordUsers");
        }
    }
}
