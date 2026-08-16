using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LawConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLawyerRegistrationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApplicationSubmittedAtUtc",
                table: "Lawyers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CityName",
                table: "Lawyers",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryName",
                table: "Lawyers",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewReason",
                table: "Lawyers",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAtUtc",
                table: "Lawyers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByUserId",
                table: "Lawyers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StateBarCouncil",
                table: "Lawyers",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StateName",
                table: "Lawyers",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            // NOTE: this migration originally also added EmergencyRequests.CurrentSearchScope,
            // CustomerCityId/CityName/CountryId/CountryName/StateId/StateName, Cities.Latitude/
            // Longitude, and a composite EmergencyRequests index. Those columns/index already
            // exist in the database (applied — and recorded in __EFMigrationsHistory — by the
            // 20260808143500_EmergencySosEscalationIndexes and 20260808154500_EmergencyGeoHierarchy
            // migrations, whose .Designer.cs files are missing from this migrations folder, which
            // is a separate pre-existing issue). Re-adding them here failed with "column already
            // exists", so those duplicate operations were removed; only the genuinely new Lawyers
            // columns/index (needed by the Admin lawyer-verification feature) remain below.

            migrationBuilder.CreateIndex(
                name: "IX_Lawyers_ApplicationSubmittedAtUtc",
                table: "Lawyers",
                column: "ApplicationSubmittedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lawyers_ApplicationSubmittedAtUtc",
                table: "Lawyers");

            migrationBuilder.DropColumn(
                name: "ApplicationSubmittedAtUtc",
                table: "Lawyers");

            migrationBuilder.DropColumn(
                name: "CityName",
                table: "Lawyers");

            migrationBuilder.DropColumn(
                name: "CountryName",
                table: "Lawyers");

            migrationBuilder.DropColumn(
                name: "ReviewReason",
                table: "Lawyers");

            migrationBuilder.DropColumn(
                name: "ReviewedAtUtc",
                table: "Lawyers");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "Lawyers");

            migrationBuilder.DropColumn(
                name: "StateBarCouncil",
                table: "Lawyers");

            migrationBuilder.DropColumn(
                name: "StateName",
                table: "Lawyers");
        }
    }
}
