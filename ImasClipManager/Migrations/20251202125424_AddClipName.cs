using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImasClipManager.Migrations
{
    /// <inheritdoc />
    public partial class AddClipName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClipName",
                table: "Clips",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClipName",
                table: "Clips");
        }
    }
}
