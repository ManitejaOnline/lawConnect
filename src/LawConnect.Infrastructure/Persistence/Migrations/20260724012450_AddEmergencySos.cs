using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LawConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmergencySos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmergencyCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SpecializationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_EmergencyCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmergencyCategories_Specializations_SpecializationId",
                        column: x => x.SpecializationId,
                        principalTable: "Specializations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmergencyRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    CurrentRadiusKm = table.Column<double>(type: "double precision", nullable: false),
                    AssignedLawyerId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcceptedAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    SearchStartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SearchExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AssignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_EmergencyRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmergencyRequests_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmergencyRequests_EmergencyCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "EmergencyCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmergencyRequests_Lawyers_AssignedLawyerId",
                        column: x => x.AssignedLawyerId,
                        principalTable: "Lawyers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmergencyAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmergencyRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    LawyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    DistanceKm = table.Column<double>(type: "double precision", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NotifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResponseTimeSeconds = table.Column<double>(type: "double precision", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
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
                    table.PrimaryKey("PK_EmergencyAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmergencyAssignments_EmergencyRequests_EmergencyRequestId",
                        column: x => x.EmergencyRequestId,
                        principalTable: "EmergencyRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmergencyAssignments_Lawyers_LawyerId",
                        column: x => x.LawyerId,
                        principalTable: "Lawyers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmergencyHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmergencyRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    LawyerId = table.Column<Guid>(type: "uuid", nullable: true),
                    FinalStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    AcceptanceTimeSeconds = table.Column<double>(type: "double precision", nullable: true),
                    ResponseTimeSeconds = table.Column<double>(type: "double precision", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ClosedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_EmergencyHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmergencyHistories_EmergencyRequests_EmergencyRequestId",
                        column: x => x.EmergencyRequestId,
                        principalTable: "EmergencyRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmergencyTimelineEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmergencyRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    MetadataJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_EmergencyTimelineEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmergencyTimelineEvents_EmergencyRequests_EmergencyRequestId",
                        column: x => x.EmergencyRequestId,
                        principalTable: "EmergencyRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "EmergencyCategories",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "DisplayOrder", "IsActive", "IsDeleted", "Name", "Slug", "SpecializationId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("bbbbbbbb-0001-0001-0001-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Immediate criminal legal assistance.", 1, true, false, "Criminal Emergency", "criminal-emergency", new Guid("aaaaaaaa-0001-0001-0001-000000000001"), null, null },
                    { new Guid("bbbbbbbb-0001-0001-0001-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Urgent civil dispute assistance.", 2, true, false, "Civil Emergency", "civil-emergency", new Guid("aaaaaaaa-0001-0001-0001-000000000002"), null, null },
                    { new Guid("bbbbbbbb-0001-0001-0001-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Family law emergency support.", 3, true, false, "Family Crisis", "family-crisis", new Guid("aaaaaaaa-0001-0001-0001-000000000003"), null, null },
                    { new Guid("bbbbbbbb-0001-0001-0001-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Cybercrime and digital fraud emergency.", 4, true, false, "Cyber Crime", "cyber-crime", new Guid("aaaaaaaa-0001-0001-0001-00000000000b"), null, null },
                    { new Guid("bbbbbbbb-0001-0001-0001-000000000005"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Consumer protection emergency.", 5, true, false, "Consumer Emergency", "consumer-emergency", new Guid("aaaaaaaa-0001-0001-0001-00000000000a"), null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyAssignments_CreatedAt",
                table: "EmergencyAssignments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyAssignments_EmergencyRequestId_LawyerId_AttemptNum~",
                table: "EmergencyAssignments",
                columns: new[] { "EmergencyRequestId", "LawyerId", "AttemptNumber" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyAssignments_IsDeleted",
                table: "EmergencyAssignments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyAssignments_LawyerId_Status",
                table: "EmergencyAssignments",
                columns: new[] { "LawyerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyCategories_CreatedAt",
                table: "EmergencyCategories",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyCategories_IsDeleted",
                table: "EmergencyCategories",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyCategories_Slug",
                table: "EmergencyCategories",
                column: "Slug",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyCategories_SpecializationId",
                table: "EmergencyCategories",
                column: "SpecializationId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyHistories_ClosedAtUtc",
                table: "EmergencyHistories",
                column: "ClosedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyHistories_CreatedAt",
                table: "EmergencyHistories",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyHistories_EmergencyRequestId",
                table: "EmergencyHistories",
                column: "EmergencyRequestId",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyHistories_FinalStatus",
                table: "EmergencyHistories",
                column: "FinalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyHistories_IsDeleted",
                table: "EmergencyHistories",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyRequests_AssignedLawyerId",
                table: "EmergencyRequests",
                column: "AssignedLawyerId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyRequests_CategoryId",
                table: "EmergencyRequests",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyRequests_CreatedAt",
                table: "EmergencyRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyRequests_CustomerId_Status",
                table: "EmergencyRequests",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyRequests_IsDeleted",
                table: "EmergencyRequests",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyRequests_SearchExpiresAtUtc",
                table: "EmergencyRequests",
                column: "SearchExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyRequests_Status",
                table: "EmergencyRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyTimelineEvents_CreatedAt",
                table: "EmergencyTimelineEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyTimelineEvents_EmergencyRequestId_OccurredAtUtc",
                table: "EmergencyTimelineEvents",
                columns: new[] { "EmergencyRequestId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyTimelineEvents_IsDeleted",
                table: "EmergencyTimelineEvents",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmergencyAssignments");

            migrationBuilder.DropTable(
                name: "EmergencyHistories");

            migrationBuilder.DropTable(
                name: "EmergencyTimelineEvents");

            migrationBuilder.DropTable(
                name: "EmergencyRequests");

            migrationBuilder.DropTable(
                name: "EmergencyCategories");
        }
    }
}
