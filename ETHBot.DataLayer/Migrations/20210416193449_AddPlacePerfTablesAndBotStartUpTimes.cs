using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ETHBot.DataLayer.Migrations
{
    public partial class AddPlacePerfTablesAndBotStartUpTimes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BotStartUpTimes",
                columns: table => new
                {
                    BotStartUpTimeId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StartUpTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotStartUpTimes", x => x.BotStartUpTimeId);
                });

            migrationBuilder.CreateTable(
                name: "PlacePerformanceInfos",
                columns: table => new
                {
                    PlacePerformanceHistoryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    AvgTimeInMs = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlacePerformanceInfos", x => x.PlacePerformanceHistoryId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BotStartUpTimes");

            migrationBuilder.DropTable(
                name: "PlacePerformanceInfos");
        }
    }
}
