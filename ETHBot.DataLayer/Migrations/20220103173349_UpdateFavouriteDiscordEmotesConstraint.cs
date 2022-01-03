using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETHBot.DataLayer.Migrations
{
    public partial class UpdateFavouriteDiscordEmotesConstraint : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FavouriteDiscordEmotes_DiscordEmoteId",
                table: "FavouriteDiscordEmotes");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_FavouriteDiscordEmotes_DiscordEmoteId",
                table: "FavouriteDiscordEmotes",
                column: "DiscordEmoteId",
                unique: true);
        }
    }
}
