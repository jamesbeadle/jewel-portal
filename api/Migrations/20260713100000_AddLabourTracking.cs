using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLabourTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkerId",
                table: "Timesheets",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SiteAttendanceId",
                table: "Timesheets",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Timesheets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "RateApplied",
                table: "Timesheets",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CostAmount",
                table: "Timesheets",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByEmail",
                table: "Timesheets",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedAt",
                table: "Timesheets",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Timesheets",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "");

            // Legacy rows: IsApproved=true means Approved (1); otherwise Submitted (0).
            migrationBuilder.Sql("UPDATE Timesheets SET Status = 1 WHERE IsApproved = 1");

            migrationBuilder.CreateTable(
                name: "Workers",
                columns: table => new
                {
                    WorkerId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SubcontractorId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workers", x => x.WorkerId);
                });

            migrationBuilder.CreateTable(
                name: "WorkerRateHistories",
                columns: table => new
                {
                    WorkerRateHistoryId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkerId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerRateHistories", x => x.WorkerRateHistoryId);
                });

            migrationBuilder.CreateTable(
                name: "ProjectWorkerAssignments",
                columns: table => new
                {
                    ProjectWorkerAssignmentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkerId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectWorkerAssignments", x => x.ProjectWorkerAssignmentId);
                });

            migrationBuilder.CreateTable(
                name: "SiteAttendances",
                columns: table => new
                {
                    SiteAttendanceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkerId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SignedInAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SignedOutAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteAttendances", x => x.SiteAttendanceId);
                });

            migrationBuilder.CreateTable(
                name: "SiteAccessTokens",
                columns: table => new
                {
                    SiteAccessTokenId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Token = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteAccessTokens", x => x.SiteAccessTokenId);
                });

            migrationBuilder.CreateTable(
                name: "XeroLineTimesheetCovers",
                columns: table => new
                {
                    XeroLineTimesheetCoverId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    XeroLedgerLineId = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SubcontractorId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XeroLineTimesheetCovers", x => x.XeroLineTimesheetCoverId);
                });

            migrationBuilder.CreateTable(
                name: "LabourSettlementVariances",
                columns: table => new
                {
                    LabourSettlementVarianceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CostCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SubcontractorId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    XeroLedgerLineId = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: true),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabourSettlementVariances", x => x.LabourSettlementVarianceId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Workers");
            migrationBuilder.DropTable(name: "WorkerRateHistories");
            migrationBuilder.DropTable(name: "ProjectWorkerAssignments");
            migrationBuilder.DropTable(name: "SiteAttendances");
            migrationBuilder.DropTable(name: "SiteAccessTokens");
            migrationBuilder.DropTable(name: "XeroLineTimesheetCovers");
            migrationBuilder.DropTable(name: "LabourSettlementVariances");

            migrationBuilder.DropColumn(name: "WorkerId", table: "Timesheets");
            migrationBuilder.DropColumn(name: "SiteAttendanceId", table: "Timesheets");
            migrationBuilder.DropColumn(name: "Status", table: "Timesheets");
            migrationBuilder.DropColumn(name: "RateApplied", table: "Timesheets");
            migrationBuilder.DropColumn(name: "CostAmount", table: "Timesheets");
            migrationBuilder.DropColumn(name: "ApprovedByEmail", table: "Timesheets");
            migrationBuilder.DropColumn(name: "ApprovedAt", table: "Timesheets");
            migrationBuilder.DropColumn(name: "RejectionReason", table: "Timesheets");
        }
    }
}
