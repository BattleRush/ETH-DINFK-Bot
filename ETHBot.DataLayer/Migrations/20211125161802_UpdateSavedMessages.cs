using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETHBot.DataLayer.Migrations
{
    public partial class UpdateSavedMessages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SavedMessages_DiscordMessages_DiscordMessageId",
                table: "SavedMessages");

            migrationBuilder.AlterColumn<ulong>(
                name: "DiscordMessageId",
                table: "SavedMessages",
                type: "bigint unsigned",
                nullable: true,
                oldClrType: typeof(ulong),
                oldType: "bigint unsigned");

            migrationBuilder.AddForeignKey(
                name: "FK_SavedMessages_DiscordMessages_DiscordMessageId",
                table: "SavedMessages",
                column: "DiscordMessageId",
                principalTable: "DiscordMessages",
                principalColumn: "DiscordMessageId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SavedMessages_DiscordMessages_DiscordMessageId",
                table: "SavedMessages");

            migrationBuilder.AlterColumn<ulong>(
                name: "DiscordMessageId",
                table: "SavedMessages",
                type: "bigint unsigned",
                nullable: false,
                defaultValue: 0ul,
                oldClrType: typeof(ulong),
                oldType: "bigint unsigned",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SavedMessages_DiscordMessages_DiscordMessageId",
                table: "SavedMessages",
                column: "DiscordMessageId",
                principalTable: "DiscordMessages",
                principalColumn: "DiscordMessageId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
