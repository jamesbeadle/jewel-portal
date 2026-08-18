using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLabourPlanningAndRegisters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyRegisterItems",
                columns: table => new
                {
                    RegisterItemId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Counterparty = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OwnerEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    BillingCycle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    KeyDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SecondaryDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyRegisterItems", x => x.RegisterItemId);
                });

            migrationBuilder.CreateTable(
                name: "CostCodeXeroMappings",
                columns: table => new
                {
                    CostCodeXeroMappingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CostCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    XeroTrackingOptionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    XeroTrackingOptionName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LabourAccountCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    MaterialsAccountCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TravelAccountCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostCodeXeroMappings", x => x.CostCodeXeroMappingId);
                });

            migrationBuilder.CreateTable(
                name: "LabourWeekSignOffs",
                columns: table => new
                {
                    LabourWeekSignOffId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkerId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WeekStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SignedOffByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SignedOffAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabourWeekSignOffs", x => x.LabourWeekSignOffId);
                });

            migrationBuilder.CreateTable(
                name: "PolicyDocuments",
                columns: table => new
                {
                    PolicyDocumentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    PublishedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyDocuments", x => x.PolicyDocumentId);
                });

            migrationBuilder.CreateTable(
                name: "PolicySignOffs",
                columns: table => new
                {
                    PolicySignOffId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PolicyDocumentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SignedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicySignOffs", x => x.PolicySignOffId);
                });

            migrationBuilder.CreateTable(
                name: "SiteXeroMappings",
                columns: table => new
                {
                    SiteXeroMappingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    XeroTrackingOptionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    XeroTrackingOptionName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteXeroMappings", x => x.SiteXeroMappingId);
                });

            migrationBuilder.CreateTable(
                name: "WorkerAbsences",
                columns: table => new
                {
                    WorkerAbsenceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkerId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    RecordedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerAbsences", x => x.WorkerAbsenceId);
                });

            migrationBuilder.CreateTable(
                name: "WorkerCisStatuses",
                columns: table => new
                {
                    WorkerCisStatusId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkerId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CisRatePercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    VerifiedRef = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerCisStatuses", x => x.WorkerCisStatusId);
                });

            migrationBuilder.CreateTable(
                name: "WorkerContracts",
                columns: table => new
                {
                    WorkerContractId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkerId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ContractedDaysPerMonth = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerContracts", x => x.WorkerContractId);
                });

            migrationBuilder.CreateTable(
                name: "WorkerSettlementLines",
                columns: table => new
                {
                    WorkerSettlementLineId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkerId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Month = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CostCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Nature = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerSettlementLines", x => x.WorkerSettlementLineId);
                });

            migrationBuilder.CreateTable(
                name: "XeroCodingRuns",
                columns: table => new
                {
                    XeroCodingRunId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkerId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Month = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    XeroBillId = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    RunByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RunAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XeroCodingRuns", x => x.XeroCodingRunId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyRegisterItems_Kind",
                table: "CompanyRegisterItems",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_CostCodeXeroMappings_CostCode",
                table: "CostCodeXeroMappings",
                column: "CostCode");

            migrationBuilder.CreateIndex(
                name: "IX_LabourWeekSignOffs_WorkerId_WeekStart",
                table: "LabourWeekSignOffs",
                columns: new[] { "WorkerId", "WeekStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PolicySignOffs_PolicyDocumentId_RecipientEmail",
                table: "PolicySignOffs",
                columns: new[] { "PolicyDocumentId", "RecipientEmail" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteXeroMappings_ProjectId",
                table: "SiteXeroMappings",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerAbsences_WorkerId_Date",
                table: "WorkerAbsences",
                columns: new[] { "WorkerId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkerCisStatuses_WorkerId",
                table: "WorkerCisStatuses",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerContracts_WorkerId",
                table: "WorkerContracts",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerSettlementLines_WorkerId_Month",
                table: "WorkerSettlementLines",
                columns: new[] { "WorkerId", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_XeroCodingRuns_WorkerId_Month",
                table: "XeroCodingRuns",
                columns: new[] { "WorkerId", "Month" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyRegisterItems");

            migrationBuilder.DropTable(
                name: "CostCodeXeroMappings");

            migrationBuilder.DropTable(
                name: "LabourWeekSignOffs");

            migrationBuilder.DropTable(
                name: "PolicyDocuments");

            migrationBuilder.DropTable(
                name: "PolicySignOffs");

            migrationBuilder.DropTable(
                name: "SiteXeroMappings");

            migrationBuilder.DropTable(
                name: "WorkerAbsences");

            migrationBuilder.DropTable(
                name: "WorkerCisStatuses");

            migrationBuilder.DropTable(
                name: "WorkerContracts");

            migrationBuilder.DropTable(
                name: "WorkerSettlementLines");

            migrationBuilder.DropTable(
                name: "XeroCodingRuns");
        }
    }
}
