﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETHBot.DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class MakeDiscordFileFullPathUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DiscordFiles",
                keyColumn: "FullPath",
                keyValue: null,
                column: "FullPath",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "FullPath",
                table: "DiscordFiles",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FullPath",
                table: "DiscordFiles",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
