using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Valuation-invoice approval workflow + immutable report snapshots + manual historic entries
    // (see docs/Valuation-Invoice-Approval-Snapshot-Spec.md):
    //  - ValuationInvoices gains the approval-window columns (SubmittedAt/ApprovedAt/RejectedAt/
    //    CancelledAt/RejectionReason), an AmendmentCount, an IsManual flag for backdated historic
    //    entries, and a link to the latest report snapshot backing the invoice. Status ints 0-2
    //    keep their meanings; Submitted(3)/Approved(4)/Rejected(5)/Cancelled(6) are appended.
    //  - ValuationInvoiceEvents: per-invoice audit trail (amendments tracked on the same invoice).
    //  - ValuationReportSnapshots + ValuationReportSnapshotLines: a frozen line-level copy of the
    //    valuation report taken when an invoice is submitted/issued or on demand at period end.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260710160000_AddInvoiceApprovalAndReportSnapshots")]
    public partial class AddInvoiceApprovalAndReportSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubmittedAt", table: "ValuationInvoices", type: "datetimeoffset", nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedAt", table: "ValuationInvoices", type: "datetimeoffset", nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RejectedAt", table: "ValuationInvoices", type: "datetimeoffset", nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelledAt", table: "ValuationInvoices", type: "datetimeoffset", nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason", table: "ValuationInvoices", type: "nvarchar(1024)", maxLength: 1024, nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "AmendmentCount", table: "ValuationInvoices", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<bool>(
                name: "IsManual", table: "ValuationInvoices", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>(
                name: "ValuationReportSnapshotId", table: "ValuationInvoices", type: "nvarchar(64)", maxLength: 64, nullable: true);

            migrationBuilder.CreateTable(
                name: "ValuationInvoiceEvents",
                columns: table => new
                {
                    ValuationInvoiceEventId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ValuationInvoiceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    AmountBefore = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    AmountAfter = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValuationInvoiceEvents", x => x.ValuationInvoiceEventId);
                });

            migrationBuilder.CreateTable(
                name: "ValuationReportSnapshots",
                columns: table => new
                {
                    ValuationReportSnapshotId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ValuationInvoiceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ValuationClaimId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Label = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TakenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsSuperseded = table.Column<bool>(type: "bit", nullable: false),
                    ContractSum = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    NetVariations = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RevisedContractSum = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalWorksComplete = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RetentionPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RetentionHeld = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RetentionReleasePercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RetentionReleased = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CertifiedToDate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PaymentDueExVat = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValuationReportSnapshots", x => x.ValuationReportSnapshotId);
                });

            migrationBuilder.CreateTable(
                name: "ValuationReportSnapshotLines",
                columns: table => new
                {
                    ValuationReportSnapshotLineId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ValuationReportSnapshotId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceValuationLineItemId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ElementType = table.Column<int>(type: "int", nullable: false),
                    SectionCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    SectionName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    VariationRef = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    VariationTitle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LineType = table.Column<int>(type: "int", nullable: false),
                    CostCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LineAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PercentComplete = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CumulativeClaimed = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PeriodIncrement = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValuationReportSnapshotLines", x => x.ValuationReportSnapshotLineId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ValuationInvoiceEvents_ValuationInvoiceId",
                table: "ValuationInvoiceEvents",
                column: "ValuationInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ValuationReportSnapshots_ProjectId",
                table: "ValuationReportSnapshots",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ValuationReportSnapshots_ValuationInvoiceId",
                table: "ValuationReportSnapshots",
                column: "ValuationInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ValuationReportSnapshotLines_ValuationReportSnapshotId",
                table: "ValuationReportSnapshotLines",
                column: "ValuationReportSnapshotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ValuationReportSnapshotLines");
            migrationBuilder.DropTable(name: "ValuationReportSnapshots");
            migrationBuilder.DropTable(name: "ValuationInvoiceEvents");

            migrationBuilder.DropColumn(name: "ValuationReportSnapshotId", table: "ValuationInvoices");
            migrationBuilder.DropColumn(name: "IsManual", table: "ValuationInvoices");
            migrationBuilder.DropColumn(name: "AmendmentCount", table: "ValuationInvoices");
            migrationBuilder.DropColumn(name: "RejectionReason", table: "ValuationInvoices");
            migrationBuilder.DropColumn(name: "CancelledAt", table: "ValuationInvoices");
            migrationBuilder.DropColumn(name: "RejectedAt", table: "ValuationInvoices");
            migrationBuilder.DropColumn(name: "ApprovedAt", table: "ValuationInvoices");
            migrationBuilder.DropColumn(name: "SubmittedAt", table: "ValuationInvoices");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
