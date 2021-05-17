using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ETHBot.DataLayer.Migrations
{
    public partial class AddTablesToSaveMultipixelJobs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlaceMultipixelJobs",
                columns: table => new
                {
                    PlaceMultipixelJobId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalPixels = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CanceledAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PlaceDiscordUserId = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceMultipixelJobs", x => x.PlaceMultipixelJobId);
                    table.ForeignKey(
                        name: "FK_PlaceMultipixelJobs_PlaceDiscordUsers_PlaceDiscordUserId",
                        column: x => x.PlaceDiscordUserId,
                        principalTable: "PlaceDiscordUsers",
                        principalColumn: "PlaceDiscordUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaceMultipixelPackets",
                columns: table => new
                {
                    PlaceMultipixelPacketId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlaceMultipixelJobId = table.Column<int>(type: "int", nullable: false),
                    InstructionCount = table.Column<int>(type: "int", nullable: false),
                    Instructions = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    Done = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceMultipixelPackets", x => x.PlaceMultipixelPacketId);
                    table.ForeignKey(
                        name: "FK_PlaceMultipixelPackets_PlaceMultipixelJobs_PlaceMultipixelJo~",
                        column: x => x.PlaceMultipixelJobId,
                        principalTable: "PlaceMultipixelJobs",
                        principalColumn: "PlaceMultipixelJobId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlaceMultipixelJobs_PlaceDiscordUserId",
                table: "PlaceMultipixelJobs",
                column: "PlaceDiscordUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaceMultipixelPackets_PlaceMultipixelJobId",
                table: "PlaceMultipixelPackets",
                column: "PlaceMultipixelJobId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlaceMultipixelPackets");

            migrationBuilder.DropTable(
                name: "PlaceMultipixelJobs");
        }
    }
}
