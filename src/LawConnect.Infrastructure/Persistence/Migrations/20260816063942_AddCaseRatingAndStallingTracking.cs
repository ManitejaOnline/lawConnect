using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LawConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseRatingAndStallingTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CaseManagementNote",
                table: "LawyerTrustScoreHistories",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "AppointmentId",
                table: "LawyerRatings",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "CaseId",
                table: "LawyerRatings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LawyerRatings_CaseId",
                table: "LawyerRatings",
                column: "CaseId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LawyerRatings_Cases_CaseId",
                table: "LawyerRatings",
                column: "CaseId",
                principalTable: "Cases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LawyerRatings_Cases_CaseId",
                table: "LawyerRatings");

            migrationBuilder.DropIndex(
                name: "IX_LawyerRatings_CaseId",
                table: "LawyerRatings");

            migrationBuilder.DropColumn(
                name: "CaseManagementNote",
                table: "LawyerTrustScoreHistories");

            migrationBuilder.DropColumn(
                name: "CaseId",
                table: "LawyerRatings");

            migrationBuilder.AlterColumn<Guid>(
                name: "AppointmentId",
                table: "LawyerRatings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
