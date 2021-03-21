using Microsoft.EntityFrameworkCore.Migrations;

namespace ETHBot.DataLayer.Migrations
{
    public partial class AddedPlaceFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Removed",
                table: "PlaceBoardHistory",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Removed",
                table: "PlaceBoardHistory");
        }
    }
}
