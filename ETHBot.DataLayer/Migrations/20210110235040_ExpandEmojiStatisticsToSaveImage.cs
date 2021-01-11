using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ETHBot.DataLayer.Migrations
{
    public partial class ExpandEmojiStatisticsToSaveImage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        { 
            migrationBuilder.AddColumn<bool>(
                name: "Blocked",
                table: "EmojiStatistics",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "ImageData",
                table: "EmojiStatistics",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Blocked",
                table: "EmojiStatistics");

            migrationBuilder.DropColumn(
                name: "ImageData",
                table: "EmojiStatistics");
        }
    }
}
