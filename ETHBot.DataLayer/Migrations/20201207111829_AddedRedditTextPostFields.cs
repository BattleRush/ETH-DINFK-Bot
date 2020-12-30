using Microsoft.EntityFrameworkCore.Migrations;

namespace ETHBot.DataLayer.Migrations
{
    public partial class AddedRedditTextPostFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "RedditPosts",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsText",
                table: "RedditPosts",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Content",
                table: "RedditPosts");

            migrationBuilder.DropColumn(
                name: "IsText",
                table: "RedditPosts");
        }
    }
}
