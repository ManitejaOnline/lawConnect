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

            migrationBuilder.AddColumn<string>(
                name: "CurrentSearchScope",
                table: "EmergencyRequests",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerCityId",
                table: "EmergencyRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerCityName",
                table: "EmergencyRequests",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerCountryId",
                table: "EmergencyRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerCountryName",
                table: "EmergencyRequests",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerStateId",
                table: "EmergencyRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerStateName",
                table: "EmergencyRequests",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Cities",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Cities",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lawyers_ApplicationSubmittedAtUtc",
                table: "Lawyers",
                column: "ApplicationSubmittedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyRequests_CustomerCountryId_CustomerStateId_Custome~",
                table: "EmergencyRequests",
                columns: new[] { "CustomerCountryId", "CustomerStateId", "CustomerCityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lawyers_ApplicationSubmittedAtUtc",
                table: "Lawyers");

            migrationBuilder.DropIndex(
                name: "IX_EmergencyRequests_CustomerCountryId_CustomerStateId_Custome~",
                table: "EmergencyRequests");

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

            migrationBuilder.DropColumn(
                name: "CurrentSearchScope",
                table: "EmergencyRequests");

            migrationBuilder.DropColumn(
                name: "CustomerCityId",
                table: "EmergencyRequests");

            migrationBuilder.DropColumn(
                name: "CustomerCityName",
                table: "EmergencyRequests");

            migrationBuilder.DropColumn(
                name: "CustomerCountryId",
                table: "EmergencyRequests");

            migrationBuilder.DropColumn(
                name: "CustomerCountryName",
                table: "EmergencyRequests");

            migrationBuilder.DropColumn(
                name: "CustomerStateId",
                table: "EmergencyRequests");

            migrationBuilder.DropColumn(
                name: "CustomerStateName",
                table: "EmergencyRequests");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Cities");
        }
    }
}
