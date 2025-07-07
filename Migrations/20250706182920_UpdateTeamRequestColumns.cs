using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lol.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTeamRequestColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserCompetencies_AspNetUsers_UsersId",
                table: "UserCompetencies");

            migrationBuilder.DropForeignKey(
                name: "FK_UserCompetencies_Competencies_CompetenciesId",
                table: "UserCompetencies");

            migrationBuilder.RenameColumn(
                name: "UsersId",
                table: "UserCompetencies",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "CompetenciesId",
                table: "UserCompetencies",
                newName: "CompetencyId");

            migrationBuilder.RenameIndex(
                name: "IX_UserCompetencies_UsersId",
                table: "UserCompetencies",
                newName: "IX_UserCompetencies_UserId");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalDate",
                table: "TeamRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificatesAtRequestJson",
                table: "TeamRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompetenciesAtRequestJson",
                table: "TeamRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "TeamRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestDate",
                table: "TeamRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddForeignKey(
                name: "FK_UserCompetencies_AspNetUsers_UserId",
                table: "UserCompetencies",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserCompetencies_Competencies_CompetencyId",
                table: "UserCompetencies",
                column: "CompetencyId",
                principalTable: "Competencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserCompetencies_AspNetUsers_UserId",
                table: "UserCompetencies");

            migrationBuilder.DropForeignKey(
                name: "FK_UserCompetencies_Competencies_CompetencyId",
                table: "UserCompetencies");

            migrationBuilder.DropColumn(
                name: "ApprovalDate",
                table: "TeamRequests");

            migrationBuilder.DropColumn(
                name: "CertificatesAtRequestJson",
                table: "TeamRequests");

            migrationBuilder.DropColumn(
                name: "CompetenciesAtRequestJson",
                table: "TeamRequests");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "TeamRequests");

            migrationBuilder.DropColumn(
                name: "RequestDate",
                table: "TeamRequests");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "UserCompetencies",
                newName: "UsersId");

            migrationBuilder.RenameColumn(
                name: "CompetencyId",
                table: "UserCompetencies",
                newName: "CompetenciesId");

            migrationBuilder.RenameIndex(
                name: "IX_UserCompetencies_UserId",
                table: "UserCompetencies",
                newName: "IX_UserCompetencies_UsersId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserCompetencies_AspNetUsers_UsersId",
                table: "UserCompetencies",
                column: "UsersId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserCompetencies_Competencies_CompetenciesId",
                table: "UserCompetencies",
                column: "CompetenciesId",
                principalTable: "Competencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
