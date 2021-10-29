using Microsoft.EntityFrameworkCore.Migrations;

namespace ETHBot.DataLayer.Migrations
{
    public partial class AddPlaceChunkInfoFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "PlaceLastChunkId",
                table: "BotSetting",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<int>(
                name: "PlacePixelIdLastChunked",
                table: "BotSetting",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlaceLastChunkId",
                table: "BotSetting");

            migrationBuilder.DropColumn(
                name: "PlacePixelIdLastChunked",
                table: "BotSetting");
        }
    }
}
