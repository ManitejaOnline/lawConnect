using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LawConnect.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class EmergencySosEscalationIndexes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_EmergencyRequests_SearchExpiresAtUtc",
            table: "EmergencyRequests");

        migrationBuilder.CreateIndex(
            name: "IX_EmergencyRequests_Status_SearchExpiresAtUtc",
            table: "EmergencyRequests",
            columns: new[] { "Status", "SearchExpiresAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_EmergencyAssignments_EmergencyRequestId_AttemptNumber_Status",
            table: "EmergencyAssignments",
            columns: new[] { "EmergencyRequestId", "AttemptNumber", "Status" });

        migrationBuilder.CreateIndex(
            name: "UX_EmergencyAssignments_OneAccepted",
            table: "EmergencyAssignments",
            column: "EmergencyRequestId",
            unique: true,
            filter: "\"IsDeleted\" = false AND \"Status\" = 'Accepted'");

        migrationBuilder.CreateIndex(
            name: "IX_EmergencyHistories_LawyerId",
            table: "EmergencyHistories",
            column: "LawyerId");

        migrationBuilder.CreateIndex(
            name: "IX_EmergencyHistories_CustomerId",
            table: "EmergencyHistories",
            column: "CustomerId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_EmergencyHistories_CustomerId",
            table: "EmergencyHistories");

        migrationBuilder.DropIndex(
            name: "IX_EmergencyHistories_LawyerId",
            table: "EmergencyHistories");

        migrationBuilder.DropIndex(
            name: "UX_EmergencyAssignments_OneAccepted",
            table: "EmergencyAssignments");

        migrationBuilder.DropIndex(
            name: "IX_EmergencyAssignments_EmergencyRequestId_AttemptNumber_Status",
            table: "EmergencyAssignments");

        migrationBuilder.DropIndex(
            name: "IX_EmergencyRequests_Status_SearchExpiresAtUtc",
            table: "EmergencyRequests");

        migrationBuilder.CreateIndex(
            name: "IX_EmergencyRequests_SearchExpiresAtUtc",
            table: "EmergencyRequests",
            column: "SearchExpiresAtUtc");
    }
}
