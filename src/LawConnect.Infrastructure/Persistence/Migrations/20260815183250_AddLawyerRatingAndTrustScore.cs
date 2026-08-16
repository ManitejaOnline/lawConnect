using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LawConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLawyerRatingAndTrustScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TrustLevel",
                table: "Lawyers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrustScore",
                table: "Lawyers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrustScoreUpdatedAtUtc",
                table: "Lawyers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NoShowReportedAtUtc",
                table: "Appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LawyerComplaints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LawyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    AdminNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_LawyerComplaints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LawyerComplaints_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LawyerComplaints_Customers_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LawyerComplaints_Lawyers_LawyerId",
                        column: x => x.LawyerId,
                        principalTable: "Lawyers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LawyerRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LawyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Stars = table.Column<int>(type: "integer", nullable: false),
                    ReviewText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_LawyerRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LawyerRatings_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LawyerRatings_Customers_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LawyerRatings_Lawyers_LawyerId",
                        column: x => x.LawyerId,
                        principalTable: "Lawyers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LawyerTrustScoreHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LawyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ClientRatingPoints = table.Column<double>(type: "double precision", nullable: false),
                    AppointmentReliabilityPoints = table.Column<double>(type: "double precision", nullable: false),
                    ResponsivenessPoints = table.Column<double>(type: "double precision", nullable: false),
                    CaseUpdatePoints = table.Column<double>(type: "double precision", nullable: false),
                    CaseManagementPoints = table.Column<double>(type: "double precision", nullable: false),
                    VerifiedComplaintPoints = table.Column<double>(type: "double precision", nullable: false),
                    ComputedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_LawyerTrustScoreHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LawyerTrustScoreHistories_Lawyers_LawyerId",
                        column: x => x.LawyerId,
                        principalTable: "Lawyers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LawyerComplaints_AppointmentId",
                table: "LawyerComplaints",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_LawyerComplaints_ClientId",
                table: "LawyerComplaints",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_LawyerComplaints_CreatedAt",
                table: "LawyerComplaints",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LawyerComplaints_IsDeleted",
                table: "LawyerComplaints",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_LawyerComplaints_LawyerId",
                table: "LawyerComplaints",
                column: "LawyerId");

            migrationBuilder.CreateIndex(
                name: "IX_LawyerComplaints_Status",
                table: "LawyerComplaints",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LawyerRatings_AppointmentId",
                table: "LawyerRatings",
                column: "AppointmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LawyerRatings_ClientId",
                table: "LawyerRatings",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_LawyerRatings_CreatedAt",
                table: "LawyerRatings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LawyerRatings_IsDeleted",
                table: "LawyerRatings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_LawyerRatings_LawyerId",
                table: "LawyerRatings",
                column: "LawyerId");

            migrationBuilder.CreateIndex(
                name: "IX_LawyerTrustScoreHistories_ComputedAtUtc",
                table: "LawyerTrustScoreHistories",
                column: "ComputedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_LawyerTrustScoreHistories_CreatedAt",
                table: "LawyerTrustScoreHistories",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LawyerTrustScoreHistories_IsDeleted",
                table: "LawyerTrustScoreHistories",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_LawyerTrustScoreHistories_LawyerId",
                table: "LawyerTrustScoreHistories",
                column: "LawyerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LawyerComplaints");

            migrationBuilder.DropTable(
                name: "LawyerRatings");

            migrationBuilder.DropTable(
                name: "LawyerTrustScoreHistories");

            migrationBuilder.DropColumn(
                name: "TrustLevel",
                table: "Lawyers");

            migrationBuilder.DropColumn(
                name: "TrustScore",
                table: "Lawyers");

            migrationBuilder.DropColumn(
                name: "TrustScoreUpdatedAtUtc",
                table: "Lawyers");

            migrationBuilder.DropColumn(
                name: "NoShowReportedAtUtc",
                table: "Appointments");
        }
    }
}
