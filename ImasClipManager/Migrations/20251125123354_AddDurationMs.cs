using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImasClipManager.Migrations
{
    /// <inheritdoc />
    public partial class AddDurationMs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CharacterName",
                table: "Performers");

            migrationBuilder.DropColumn(
                name: "CharacterYomi",
                table: "Performers");

            migrationBuilder.RenameColumn(
                name: "NameYomi",
                table: "Performers",
                newName: "Yomi");

            migrationBuilder.AlterColumn<int>(
                name: "Brand",
                table: "Performers",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<int>(
                name: "LiveType",
                table: "Performers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<long>(
                name: "StartTimeMs",
                table: "Clips",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ConcertDate",
                table: "Clips",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AddColumn<long>(
                name: "DurationMs",
                table: "Clips",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "IsAutoThumbnail",
                table: "Clips",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LiveType",
                table: "Performers");

            migrationBuilder.DropColumn(
                name: "DurationMs",
                table: "Clips");

            migrationBuilder.DropColumn(
                name: "IsAutoThumbnail",
                table: "Clips");

            migrationBuilder.RenameColumn(
                name: "Yomi",
                table: "Performers",
                newName: "NameYomi");

            migrationBuilder.AlterColumn<string>(
                name: "Brand",
                table: "Performers",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "CharacterName",
                table: "Performers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CharacterYomi",
                table: "Performers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<long>(
                name: "StartTimeMs",
                table: "Clips",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ConcertDate",
                table: "Clips",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
