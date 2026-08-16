using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LawConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LawyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecializationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CaseNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CourtName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SourceAppointmentId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_Cases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cases_Appointments_SourceAppointmentId",
                        column: x => x.SourceAppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Cases_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cases_Lawyers_LawyerId",
                        column: x => x.LawyerId,
                        principalTable: "Lawyers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cases_Specializations_SpecializationId",
                        column: x => x.SpecializationId,
                        principalTable: "Specializations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaseMilestones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_CaseMilestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseMilestones_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseUpdates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdateType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
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
                    table.PrimaryKey("PK_CaseUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseUpdates_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hearings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    HearingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CourtName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    HearingType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Purpose = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NextAction = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_Hearings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hearings_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseUpdateId = table.Column<Guid>(type: "uuid", nullable: true),
                    HearingId = table.Column<Guid>(type: "uuid", nullable: true),
                    MilestoneId = table.Column<Guid>(type: "uuid", nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
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
                    table.PrimaryKey("PK_CaseDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseDocuments_CaseMilestones_MilestoneId",
                        column: x => x.MilestoneId,
                        principalTable: "CaseMilestones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CaseDocuments_CaseUpdates_CaseUpdateId",
                        column: x => x.CaseUpdateId,
                        principalTable: "CaseUpdates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CaseDocuments_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseDocuments_Hearings_HearingId",
                        column: x => x.HearingId,
                        principalTable: "Hearings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaseDocuments_CaseId",
                table: "CaseDocuments",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseDocuments_CaseUpdateId",
                table: "CaseDocuments",
                column: "CaseUpdateId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseDocuments_CreatedAt",
                table: "CaseDocuments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CaseDocuments_HearingId",
                table: "CaseDocuments",
                column: "HearingId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseDocuments_IsDeleted",
                table: "CaseDocuments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CaseDocuments_MilestoneId",
                table: "CaseDocuments",
                column: "MilestoneId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseDocuments_StorageKey",
                table: "CaseDocuments",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseMilestones_CaseId",
                table: "CaseMilestones",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseMilestones_CreatedAt",
                table: "CaseMilestones",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CaseMilestones_DueDate",
                table: "CaseMilestones",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_CaseMilestones_IsDeleted",
                table: "CaseMilestones",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_CreatedAt",
                table: "Cases",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_CustomerId",
                table: "Cases",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_IsDeleted",
                table: "Cases",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_LawyerId",
                table: "Cases",
                column: "LawyerId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_SourceAppointmentId",
                table: "Cases",
                column: "SourceAppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_SpecializationId",
                table: "Cases",
                column: "SpecializationId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_Status",
                table: "Cases",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CaseUpdates_CaseId",
                table: "CaseUpdates",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseUpdates_CreatedAt",
                table: "CaseUpdates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CaseUpdates_IsDeleted",
                table: "CaseUpdates",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Hearings_CaseId",
                table: "Hearings",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Hearings_CreatedAt",
                table: "Hearings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Hearings_HearingDate",
                table: "Hearings",
                column: "HearingDate");

            migrationBuilder.CreateIndex(
                name: "IX_Hearings_IsDeleted",
                table: "Hearings",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseDocuments");

            migrationBuilder.DropTable(
                name: "CaseMilestones");

            migrationBuilder.DropTable(
                name: "CaseUpdates");

            migrationBuilder.DropTable(
                name: "Hearings");

            migrationBuilder.DropTable(
                name: "Cases");
        }
    }
}
