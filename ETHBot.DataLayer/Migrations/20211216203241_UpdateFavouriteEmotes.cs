using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETHBot.DataLayer.Migrations
{
    public partial class UpdateFavouriteEmotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_FavouriteDiscordEmotes_DiscordEmoteId",
                table: "FavouriteDiscordEmotes",
                column: "DiscordEmoteId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FavouriteDiscordEmotes_DiscordEmoteId",
                table: "FavouriteDiscordEmotes");
        }
    }
}
