using Microsoft.EntityFrameworkCore.Migrations;

namespace ETHBot.DataLayer.Migrations
{
    public partial class AddedPlaceSetting : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PlaceLocked",
                table: "BotSetting",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlaceLocked",
                table: "BotSetting");
        }
    }
}
