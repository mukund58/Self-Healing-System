using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskApi.Migrations
{
    /// <inheritdoc />
    public partial class AddFailureEventsAndRecoveryActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FailureEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FailureType = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Resolved = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailureEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecoveryActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FailureEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "text", nullable: false),
                    TargetDeployment = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: true),
                    PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoveryActions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FailureEvents_DetectedAt",
                table: "FailureEvents",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FailureEvents_FailureType",
                table: "FailureEvents",
                column: "FailureType");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryActions_FailureEventId",
                table: "RecoveryActions",
                column: "FailureEventId");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryActions_PerformedAt",
                table: "RecoveryActions",
                column: "PerformedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FailureEvents");

            migrationBuilder.DropTable(
                name: "RecoveryActions");
        }
    }
}
