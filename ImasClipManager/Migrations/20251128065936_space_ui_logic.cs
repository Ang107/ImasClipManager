using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImasClipManager.Migrations
{
    /// <inheritdoc />
    public partial class space_ui_logic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEditing",
                table: "Spaces",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEditing",
                table: "Spaces");
        }
    }
}
