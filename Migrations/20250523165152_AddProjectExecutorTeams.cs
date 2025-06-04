using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lol.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectExecutorTeams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectExecutors",
                columns: table => new
                {
                    ExecutorTeamsId = table.Column<int>(type: "int", nullable: false),
                    Project1Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectExecutors", x => new { x.ExecutorTeamsId, x.Project1Id });
                    table.ForeignKey(
                        name: "FK_ProjectExecutors_Projects_Project1Id",
                        column: x => x.Project1Id,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectExecutors_Teams_ExecutorTeamsId",
                        column: x => x.ExecutorTeamsId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectExecutors_Project1Id",
                table: "ProjectExecutors",
                column: "Project1Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectExecutors");
        }
    }
}
