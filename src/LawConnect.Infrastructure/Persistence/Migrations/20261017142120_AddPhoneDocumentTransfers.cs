using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LawConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneDocumentTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    FileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerDocuments_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerDocuments_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerDocuments_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentTransferSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    LawyerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UploadTokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BytesTransferred = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTransferSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentTransferSessions_CustomerDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "CustomerDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentTransferSessions_Users_LawyerUserId",
                        column: x => x.LawyerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDocuments_AppointmentId",
                table: "CustomerDocuments",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDocuments_CaseId",
                table: "CustomerDocuments",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDocuments_CreatedAt",
                table: "CustomerDocuments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDocuments_IsDeleted",
                table: "CustomerDocuments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDocuments_OwnerUserId",
                table: "CustomerDocuments",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTransferSessions_CreatedAt",
                table: "DocumentTransferSessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTransferSessions_DocumentId_Status",
                table: "DocumentTransferSessions",
                columns: new[] { "DocumentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTransferSessions_IsDeleted",
                table: "DocumentTransferSessions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTransferSessions_LawyerUserId",
                table: "DocumentTransferSessions",
                column: "LawyerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentTransferSessions");

            migrationBuilder.DropTable(
                name: "CustomerDocuments");
        }
    }
}
