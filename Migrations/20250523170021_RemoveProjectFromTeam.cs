using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lol.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProjectFromTeam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectExecutors_Projects_Project1Id",
                table: "ProjectExecutors");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Projects_ProjectId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_ProjectId",
                table: "Teams");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectExecutors",
                table: "ProjectExecutors");

            migrationBuilder.DropIndex(
                name: "IX_ProjectExecutors_Project1Id",
                table: "ProjectExecutors");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Teams");

            migrationBuilder.RenameColumn(
                name: "Project1Id",
                table: "ProjectExecutors",
                newName: "ExecutorProjectsId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectExecutors",
                table: "ProjectExecutors",
                columns: new[] { "ExecutorProjectsId", "ExecutorTeamsId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectExecutors_ExecutorTeamsId",
                table: "ProjectExecutors",
                column: "ExecutorTeamsId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectExecutors_Projects_ExecutorProjectsId",
                table: "ProjectExecutors",
                column: "ExecutorProjectsId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectExecutors_Projects_ExecutorProjectsId",
                table: "ProjectExecutors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectExecutors",
                table: "ProjectExecutors");

            migrationBuilder.DropIndex(
                name: "IX_ProjectExecutors_ExecutorTeamsId",
                table: "ProjectExecutors");

            migrationBuilder.RenameColumn(
                name: "ExecutorProjectsId",
                table: "ProjectExecutors",
                newName: "Project1Id");

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "Teams",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectExecutors",
                table: "ProjectExecutors",
                columns: new[] { "ExecutorTeamsId", "Project1Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_ProjectId",
                table: "Teams",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectExecutors_Project1Id",
                table: "ProjectExecutors",
                column: "Project1Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectExecutors_Projects_Project1Id",
                table: "ProjectExecutors",
                column: "Project1Id",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Projects_ProjectId",
                table: "Teams",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
