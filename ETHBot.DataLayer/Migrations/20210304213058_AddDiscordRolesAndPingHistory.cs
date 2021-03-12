using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ETHBot.DataLayer.Migrations
{
    public partial class AddDiscordRolesAndPingHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscordRoles",
                columns: table => new
                {
                    DiscordRoleId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ColorHex = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsHoisted = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsManaged = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsMentionable = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    DiscordServerId = table.Column<ulong>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordRoles", x => x.DiscordRoleId);
                    table.ForeignKey(
                        name: "FK_DiscordRoles_DiscordServers_DiscordServerId",
                        column: x => x.DiscordServerId,
                        principalTable: "DiscordServers",
                        principalColumn: "DiscordServerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PingHistory",
                columns: table => new
                {
                    PingHistoryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DiscordRoleId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    DiscordUserId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    MessageId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    FromDiscordUserId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PingHistory", x => x.PingHistoryId);
                    table.ForeignKey(
                        name: "FK_PingHistory_DiscordMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "DiscordMessages",
                        principalColumn: "MessageId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PingHistory_DiscordRoles_DiscordRoleId",
                        column: x => x.DiscordRoleId,
                        principalTable: "DiscordRoles",
                        principalColumn: "DiscordRoleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PingHistory_DiscordUsers_DiscordUserId",
                        column: x => x.DiscordUserId,
                        principalTable: "DiscordUsers",
                        principalColumn: "DiscordUserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PingHistory_DiscordUsers_FromDiscordUserId",
                        column: x => x.FromDiscordUserId,
                        principalTable: "DiscordUsers",
                        principalColumn: "DiscordUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscordRoles_DiscordServerId",
                table: "DiscordRoles",
                column: "DiscordServerId");

            migrationBuilder.CreateIndex(
                name: "IX_PingHistory_DiscordRoleId",
                table: "PingHistory",
                column: "DiscordRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_PingHistory_DiscordUserId",
                table: "PingHistory",
                column: "DiscordUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PingHistory_FromDiscordUserId",
                table: "PingHistory",
                column: "FromDiscordUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PingHistory_MessageId",
                table: "PingHistory",
                column: "MessageId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PingHistory");

            migrationBuilder.DropTable(
                name: "DiscordRoles");
        }
    }
}
