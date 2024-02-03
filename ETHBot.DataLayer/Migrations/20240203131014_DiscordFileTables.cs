using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETHBot.DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class DiscordFileTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscordFiles",
                columns: table => new
                {
                    DiscordFileId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DiscordMessageId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    FileName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FullPath = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Downloaded = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsImage = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsVideo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsAudio = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsText = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    FPS = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    Bitrate = table.Column<int>(type: "int", nullable: true),
                    FileSize = table.Column<int>(type: "int", nullable: false),
                    MimeType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Extension = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Url = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UrlWithoutParams = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OcrText = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OcrDone = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordFiles", x => x.DiscordFileId);
                    table.ForeignKey(
                        name: "FK_DiscordFiles_DiscordMessages_DiscordMessageId",
                        column: x => x.DiscordMessageId,
                        principalTable: "DiscordMessages",
                        principalColumn: "DiscordMessageId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PytorchModels",
                columns: table => new
                {
                    PytorchModelId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ModelName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Main = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ForImage = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ForVideo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ForAudio = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PytorchModels", x => x.PytorchModelId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OcrBoxes",
                columns: table => new
                {
                    OcrBoxId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DiscordFileId = table.Column<int>(type: "int", nullable: false),
                    TopLeftX = table.Column<int>(type: "int", nullable: false),
                    TopLeftY = table.Column<int>(type: "int", nullable: false),
                    TopRightX = table.Column<int>(type: "int", nullable: false),
                    TopRightY = table.Column<int>(type: "int", nullable: false),
                    BottomRightX = table.Column<int>(type: "int", nullable: false),
                    BottomRightY = table.Column<int>(type: "int", nullable: false),
                    BottomLeftX = table.Column<int>(type: "int", nullable: false),
                    BottomLeftY = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Probability = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OcrBoxes", x => x.OcrBoxId);
                    table.ForeignKey(
                        name: "FK_OcrBoxes_DiscordFiles_DiscordFileId",
                        column: x => x.DiscordFileId,
                        principalTable: "DiscordFiles",
                        principalColumn: "DiscordFileId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DiscordFileEmbeds",
                columns: table => new
                {
                    DiscordFileEmbedId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DiscordFileId = table.Column<int>(type: "int", nullable: false),
                    PytorchModelId = table.Column<int>(type: "int", nullable: false),
                    Embed = table.Column<byte[]>(type: "longblob", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordFileEmbeds", x => x.DiscordFileEmbedId);
                    table.ForeignKey(
                        name: "FK_DiscordFileEmbeds_DiscordFiles_DiscordFileId",
                        column: x => x.DiscordFileId,
                        principalTable: "DiscordFiles",
                        principalColumn: "DiscordFileId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscordFileEmbeds_PytorchModels_PytorchModelId",
                        column: x => x.PytorchModelId,
                        principalTable: "PytorchModels",
                        principalColumn: "PytorchModelId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordFileEmbeds_DiscordFileId",
                table: "DiscordFileEmbeds",
                column: "DiscordFileId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordFileEmbeds_PytorchModelId",
                table: "DiscordFileEmbeds",
                column: "PytorchModelId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordFiles_DiscordMessageId",
                table: "DiscordFiles",
                column: "DiscordMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_OcrBoxes_DiscordFileId",
                table: "OcrBoxes",
                column: "DiscordFileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscordFileEmbeds");

            migrationBuilder.DropTable(
                name: "OcrBoxes");

            migrationBuilder.DropTable(
                name: "PytorchModels");

            migrationBuilder.DropTable(
                name: "DiscordFiles");
        }
    }
}
