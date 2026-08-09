using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LawConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationPreferencesAndLawyerOnline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "Lawyers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastOnlineAtUtc",
                table: "Lawyers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnablePush = table.Column<bool>(type: "boolean", nullable: false),
                    EnableSms = table.Column<bool>(type: "boolean", nullable: false),
                    EnableEmail = table.Column<bool>(type: "boolean", nullable: false),
                    EnableInApp = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_NotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lawyers_IsOnline",
                table: "Lawyers",
                column: "IsOnline");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_CreatedAt",
                table: "NotificationPreferences",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_IsDeleted",
                table: "NotificationPreferences",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_UserId",
                table: "NotificationPreferences",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropIndex(
                name: "IX_Lawyers_IsOnline",
                table: "Lawyers");

            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "Lawyers");

            migrationBuilder.DropColumn(
                name: "LastOnlineAtUtc",
                table: "Lawyers");
        }
    }
}
