using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LawConnect.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class EmergencyGeoHierarchy : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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

        migrationBuilder.AddColumn<Guid>(
            name: "CustomerCountryId",
            table: "EmergencyRequests",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "CustomerStateId",
            table: "EmergencyRequests",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "CustomerCityId",
            table: "EmergencyRequests",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CustomerCountryName",
            table: "EmergencyRequests",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CustomerStateName",
            table: "EmergencyRequests",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CustomerCityName",
            table: "EmergencyRequests",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CurrentSearchScope",
            table: "EmergencyRequests",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "City");

        migrationBuilder.CreateIndex(
            name: "IX_EmergencyRequests_CustomerCountryId_CustomerStateId_CustomerCityId",
            table: "EmergencyRequests",
            columns: new[] { "CustomerCountryId", "CustomerStateId", "CustomerCityId" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_EmergencyRequests_CustomerCountryId_CustomerStateId_CustomerCityId",
            table: "EmergencyRequests");

        migrationBuilder.DropColumn(name: "Latitude", table: "Cities");
        migrationBuilder.DropColumn(name: "Longitude", table: "Cities");
        migrationBuilder.DropColumn(name: "CustomerCountryId", table: "EmergencyRequests");
        migrationBuilder.DropColumn(name: "CustomerStateId", table: "EmergencyRequests");
        migrationBuilder.DropColumn(name: "CustomerCityId", table: "EmergencyRequests");
        migrationBuilder.DropColumn(name: "CustomerCountryName", table: "EmergencyRequests");
        migrationBuilder.DropColumn(name: "CustomerStateName", table: "EmergencyRequests");
        migrationBuilder.DropColumn(name: "CustomerCityName", table: "EmergencyRequests");
        migrationBuilder.DropColumn(name: "CurrentSearchScope", table: "EmergencyRequests");
    }
}
