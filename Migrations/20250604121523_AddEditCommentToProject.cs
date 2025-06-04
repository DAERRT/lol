using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lol.Migrations
{
    /// <inheritdoc />
    public partial class AddEditCommentToProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EditComment",
                table: "Projects",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EditComment",
                table: "Projects");
        }
    }
}
