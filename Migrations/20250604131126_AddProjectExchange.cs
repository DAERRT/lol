using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lol.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectExchange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectExchange",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectExchange", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectExchangeProjects",
                columns: table => new
                {
                    ProjectExchangesId = table.Column<int>(type: "int", nullable: false),
                    ProjectsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectExchangeProjects", x => new { x.ProjectExchangesId, x.ProjectsId });
                    table.ForeignKey(
                        name: "FK_ProjectExchangeProjects_ProjectExchange_ProjectExchangesId",
                        column: x => x.ProjectExchangesId,
                        principalTable: "ProjectExchange",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectExchangeProjects_Projects_ProjectsId",
                        column: x => x.ProjectsId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectExchangeProjects_ProjectsId",
                table: "ProjectExchangeProjects",
                column: "ProjectsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectExchangeProjects");

            migrationBuilder.DropTable(
                name: "ProjectExchange");
        }
    }
}
