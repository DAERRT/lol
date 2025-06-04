using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lol.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectExchanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectExchangeProjects_ProjectExchange_ProjectExchangesId",
                table: "ProjectExchangeProjects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectExchange",
                table: "ProjectExchange");

            migrationBuilder.RenameTable(
                name: "ProjectExchange",
                newName: "ProjectExchanges");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectExchanges",
                table: "ProjectExchanges",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectExchangeProjects_ProjectExchanges_ProjectExchangesId",
                table: "ProjectExchangeProjects",
                column: "ProjectExchangesId",
                principalTable: "ProjectExchanges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectExchangeProjects_ProjectExchanges_ProjectExchangesId",
                table: "ProjectExchangeProjects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectExchanges",
                table: "ProjectExchanges");

            migrationBuilder.RenameTable(
                name: "ProjectExchanges",
                newName: "ProjectExchange");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectExchange",
                table: "ProjectExchange",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectExchangeProjects_ProjectExchange_ProjectExchangesId",
                table: "ProjectExchangeProjects",
                column: "ProjectExchangesId",
                principalTable: "ProjectExchange",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
