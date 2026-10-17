using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LawConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenRefreshTokensAndOpenTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "RefreshTokens"
                SET "Token" = 'revoked-' || "Id"::text,
                    "ReplacedByToken" = NULL,
                    "RevokedAtUtc" = COALESCE("RevokedAtUtc", NOW() AT TIME ZONE 'UTC');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTransferSessions_DocumentId",
                table: "DocumentTransferSessions",
                column: "DocumentId",
                unique: true,
                filter: "\"Status\" IN ('Pending', 'Approved', 'Transferring') AND \"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DocumentTransferSessions_DocumentId",
                table: "DocumentTransferSessions");
        }
    }
}
