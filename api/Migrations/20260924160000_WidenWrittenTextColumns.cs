using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Nigel could not approve V33 on Abbot Road (2026-09-24, JPMS-4F2116): line A10's description
    /// was 550 characters, ValuationLineItems.Description was nvarchar(512), and SQL Server refused
    /// the save. The field-length audit the same day found the same wall on every column people
    /// write sentences into — line descriptions, notes, reasons, comments, narratives, message
    /// bodies — so all 82 go to nvarchar(max), as the request and variation text did on
    /// 2026-09-16. Copies travel with their source (a claim line freezes the valuation line, a
    /// quote or work-order line takes a bid-package line), so each family widens together. A
    /// reconciliation package is named from its work order's title (256), so its Name goes from
    /// 128 to 256.
    /// Widening only: no data changes, no index touches these columns. Safe to apply before or
    /// with the deploy. Scoped script: api/Migrations/widen-written-text-columns.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260924160000_WidenWrittenTextColumns")]
    public partial class WidenWrittenTextColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(name: "Notes", table: "ArchitectInstructions", type: "nvarchar(max)", nullable: true, oldClrType: typeof(string), oldType: "nvarchar(2048)", oldMaxLength: 2048, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Description", table: "BidPackageLineItems", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(512)", oldMaxLength: 512);
            migrationBuilder.AlterColumn<string>(name: "SpecificationSummary", table: "BidPackages", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000);
            migrationBuilder.AlterColumn<string>(name: "Description", table: "BoqLineItems", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(512)", oldMaxLength: 512);
            migrationBuilder.AlterColumn<string>(name: "Comments", table: "ClaimLines", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(512)", oldMaxLength: 512);
            migrationBuilder.AlterColumn<string>(name: "Description", table: "ClaimLines", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(512)", oldMaxLength: 512);
            migrationBuilder.AlterColumn<string>(name: "Notes", table: "CompanyRegisterItems", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(2048)", oldMaxLength: 2048);
            migrationBuilder.AlterColumn<string>(name: "Description", table: "ContraCharges", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(512)", oldMaxLength: 512);
            migrationBuilder.AlterColumn<string>(name: "Description", table: "Dayworks", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(512)", oldMaxLength: 512);
            migrationBuilder.AlterColumn<string>(name: "Description", table: "Defects", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(1024)", oldMaxLength: 1024);
            migrationBuilder.AlterColumn<string>(name: "Notes", table: "DrawingIssueRecords", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(2048)", oldMaxLength: 2048);
            migrationBuilder.AlterColumn<string>(name: "Note", table: "DrivingLicenceChecks", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(1000)", oldMaxLength: 1000);
            migrationBuilder.AlterColumn<string>(name: "Reason", table: "Eots", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(1024)", oldMaxLength: 1024);
            migrationBuilder.AlterColumn<string>(name: "Findings", table: "HsAuditItems", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(2000)", oldMaxLength: 2000);
            migrationBuilder.AlterColumn<string>(name: "FurtherComments", table: "HsAudits", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000);
            migrationBuilder.AlterColumn<string>(name: "Summary", table: "HsRecords", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(512)", oldMaxLength: 512);
            migrationBuilder.AlterColumn<string>(name: "Brief", table: "ImagineRounds", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000);
            migrationBuilder.AlterColumn<string>(name: "LocationDetails", table: "InventoryItems", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(2048)", oldMaxLength: 2048);
            migrationBuilder.AlterColumn<string>(name: "ProductDetails", table: "InventoryItems", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(2048)", oldMaxLength: 2048);
            migrationBuilder.AlterColumn<string>(name: "Note", table: "KpiEmails", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(2048)", oldMaxLength: 2048);
            migrationBuilder.AlterColumn<string>(name: "Reason", table: "LabourChaseDismissals", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(512)", oldMaxLength: 512);
            migrationBuilder.AlterColumn<string>(name: "Reason", table: "LabourSettlementVariances", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(1024)", oldMaxLength: 1024);
            migrationBuilder.AlterColumn<string>(name: "Description", table: "LadClaims", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(2048)", oldMaxLength: 2048);
            migrationBuilder.AlterColumn<string>(name: "Summary", table: "LeadActivities", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000);
            migrationBuilder.AlterColumn<string>(name: "Description", table: "LeadEstimateLines", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(1024)", oldMaxLength: 1024);
            migrationBuilder.AlterColumn<string>(name: "Exclusions", table: "LeadEstimates", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000);
            migrationBuilder.AlterColumn<string>(name: "Notes", table: "LeadEstimates", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000);
            migrationBuilder.AlterColumn<string>(name: "Scope", table: "LeadEstimates", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000);
            migrationBuilder.AlterColumn<string>(name: "LostReason", table: "Leads", type: "nvarchar(max)", nullable: true, oldClrType: typeof(string), oldType: "nvarchar(1024)", oldMaxLength: 1024, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Notes", table: "Leads", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000);
            migrationBuilder.AlterColumn<string>(name: "Summary", table: "Leads", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(512)", oldMaxLength: 512);
            migrationBuilder.AlterColumn<string>(name: "Description", table: "MobilisationItems", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(512)", oldMaxLength: 512);
            migrationBuilder.AlterColumn<string>(name: "Summary", table: "PolicyDocuments", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(4096)", oldMaxLength: 4096);
            migrationBuilder.AlterColumn<string>(name: "Note", table: "ProgrammeVariationEffects", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(512)", oldMaxLength: 512);
            migrationBuilder.AlterColumn<string>(name: "Introduction", table: "ProgressReports", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(4096)", oldMaxLength: 4096);
            migrationBuilder.AlterColumn<string>(name: "UpcomingWorks", table: "ProgressReports", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(4096)", oldMaxLength: 4096);
            migrationBuilder.AlterColumn<string>(name: "WorkCompleted", table: "ProgressReports", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(4096)", oldMaxLength: 4096);
            migrationBuilder.AlterColumn<string>(name: "Notes", table: "ProjectContractAmendments", type: "nvarchar(max)", nullable: true, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "BespokeDeviations", table: "ProjectContracts", type: "nvarchar(max)", nullable: true, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Description", table: "QsAccruals", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(1024)", oldMaxLength: 1024);
            migrationBuilder.AlterColumn<string>(name: "Description", table: "QuoteLineItems", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(512)", oldMaxLength: 512);
            migrationBuilder.AlterColumn<string>(name: "Notes", table: "Quotes", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(1024)", oldMaxLength: 1024);
            migrationBuilder.AlterColumn<string>(name: "Response", table: "RequestItems", type: "nvarchar(max)", nullable: true, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Body", table: "RequestMessages", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000);
            migrationBuilder.AlterColumn<string>(name: "ClientNotes", table: "Requests", type: "nvarchar(max)", nullable: true, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "InternalNotes", table: "Requests", type: "nvarchar(max)", nullable: true, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "RelatedDrawingSpec", table: "Requests", type: "nvarchar(max)", nullable: true, oldClrType: typeof(string), oldType: "nvarchar(512)", oldMaxLength: 512, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Notes", table: "RightToWorkChecks", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(2000)", oldMaxLength: 2000);
            migrationBuilder.AlterColumn<string>(name: "DeclineReason", table: "SalesProposals", type: "nvarchar(max)", nullable: true, oldClrType: typeof(string), oldType: "nvarchar(1024)", oldMaxLength: 1024, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Brief", table: "SalesStrategies", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000);
            migrationBuilder.AlterColumn<string>(name: "Evidence", table: "SalesStrategies", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000);
            migrationBuilder.AlterColumn<string>(name: "Hypothesis", table: "SalesStrategies", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000);
            migrationBuilder.AlterColumn<string>(name: "Proposition", table: "SalesStrategies", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(1024)", oldMaxLength: 1024);
            migrationBuilder.AlterColumn<string>(name: "Instruction", table: "SiteInstructions", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000);
            migrationBuilder.AlterColumn<string>(name: "ArchiveNote", table: "SitePhotos", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(1024)", oldMaxLength: 1024);
            migrationBuilder.AlterColumn<string>(name: "Narrative", table: "SiteReports", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(4096)", oldMaxLength: 4096);
            migrationBuilder.AlterColumn<string>(name: "RejectionReason", table: "SubcontractorVariationRequests", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(1024)", oldMaxLength: 1024);
            migrationBuilder.AlterColumn<string>(name: "DecisionNote", table: "TenderEnquiries", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(2048)", oldMaxLength: 2048);
            migrationBuilder.AlterColumn<string>(name: "ScopeSummary", table: "TenderEnquiries", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000);
            migrationBuilder.AlterColumn<string>(name: "RejectionReason", table: "Timesheets", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(1024)", oldMaxLength: 1024);
            migrationBuilder.AlterColumn<string>(name: "Notes", table: "TodoItems", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(2048)", oldMaxLength: 2048);
            migrationBuilder.AlterColumn<string>(name: "Body", table: "UsefulInformationNotes", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000);
            migrationBuilder.AlterColumn<string>(name: "Note", table: "ValuationInvoiceEvents", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(1024)", oldMaxLength: 1024);
            migrationBuilder.AlterColumn<string>(name: "RejectionReason", table: "ValuationInvoices", type: "nvarchar(max)", nullable: true, oldClrType: typeof(string), oldType: "nvarchar(1024)", oldMaxLength: 1024, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Comments", table: "ValuationLineItems", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(512)", oldMaxLength: 512);
            migrationBuilder.AlterColumn<string>(name: "Description", table: "ValuationLineItems", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(512)", oldMaxLength: 512);
            migrationBuilder.AlterColumn<string>(name: "Body", table: "VariationOrderMessages", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000);
            migrationBuilder.AlterColumn<string>(name: "CommercialBasis", table: "VariationOrderQuotes", type: "nvarchar(max)", nullable: true, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Exclusions", table: "VariationOrderQuotes", type: "nvarchar(max)", nullable: true, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "ProgrammeImpact", table: "VariationOrderQuotes", type: "nvarchar(max)", nullable: true, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Notes", table: "VatAnalyses", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(2048)", oldMaxLength: 2048);
            migrationBuilder.AlterColumn<string>(name: "Notes", table: "WalkRoundNotes", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(4096)", oldMaxLength: 4096);
            migrationBuilder.AlterColumn<string>(name: "Notes", table: "WeeklyCashflowItems", type: "nvarchar(max)", nullable: true, oldClrType: typeof(string), oldType: "nvarchar(1000)", oldMaxLength: 1000, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Description", table: "WorkOrderLines", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(1024)", oldMaxLength: 1024);
            migrationBuilder.AlterColumn<string>(name: "ProgrammeNotes", table: "WorkOrders", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(2000)", oldMaxLength: 2000);
            migrationBuilder.AlterColumn<string>(name: "Scope", table: "WorkOrders", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000);
            migrationBuilder.AlterColumn<string>(name: "Note", table: "WorkerAbsences", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(512)", oldMaxLength: 512);
            migrationBuilder.AlterColumn<string>(name: "Note", table: "WorkerSettlementLines", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(512)", oldMaxLength: 512);
            migrationBuilder.AlterColumn<string>(name: "Note", table: "WorkstationActions", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(1000)", oldMaxLength: 1000);
            migrationBuilder.AlterColumn<string>(name: "Body", table: "XeroDisputeMessages", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(2048)", oldMaxLength: 2048);
            migrationBuilder.AlterColumn<string>(name: "Description", table: "XeroLedgerLines", type: "nvarchar(max)", nullable: true, oldClrType: typeof(string), oldType: "nvarchar(1024)", oldMaxLength: 1024, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Note", table: "XeroLedgerLines", type: "nvarchar(max)", nullable: true, oldClrType: typeof(string), oldType: "nvarchar(512)", oldMaxLength: 512, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Name", table: "ReconciliationPackages", type: "nvarchar(256)", maxLength: 256, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(128)", oldMaxLength: 128);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(name: "Notes", table: "ArchitectInstructions", type: "nvarchar(2048)", maxLength: 2048, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Description", table: "BidPackageLineItems", type: "nvarchar(512)", maxLength: 512, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "SpecificationSummary", table: "BidPackages", type: "nvarchar(4000)", maxLength: 4000, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Description", table: "BoqLineItems", type: "nvarchar(512)", maxLength: 512, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Comments", table: "ClaimLines", type: "nvarchar(512)", maxLength: 512, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Description", table: "ClaimLines", type: "nvarchar(512)", maxLength: 512, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Notes", table: "CompanyRegisterItems", type: "nvarchar(2048)", maxLength: 2048, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Description", table: "ContraCharges", type: "nvarchar(512)", maxLength: 512, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Description", table: "Dayworks", type: "nvarchar(512)", maxLength: 512, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Description", table: "Defects", type: "nvarchar(1024)", maxLength: 1024, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Notes", table: "DrawingIssueRecords", type: "nvarchar(2048)", maxLength: 2048, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Note", table: "DrivingLicenceChecks", type: "nvarchar(1000)", maxLength: 1000, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Reason", table: "Eots", type: "nvarchar(1024)", maxLength: 1024, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Findings", table: "HsAuditItems", type: "nvarchar(2000)", maxLength: 2000, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "FurtherComments", table: "HsAudits", type: "nvarchar(4000)", maxLength: 4000, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Summary", table: "HsRecords", type: "nvarchar(512)", maxLength: 512, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Brief", table: "ImagineRounds", type: "nvarchar(4000)", maxLength: 4000, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "LocationDetails", table: "InventoryItems", type: "nvarchar(2048)", maxLength: 2048, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "ProductDetails", table: "InventoryItems", type: "nvarchar(2048)", maxLength: 2048, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Note", table: "KpiEmails", type: "nvarchar(2048)", maxLength: 2048, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Reason", table: "LabourChaseDismissals", type: "nvarchar(512)", maxLength: 512, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Reason", table: "LabourSettlementVariances", type: "nvarchar(1024)", maxLength: 1024, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Description", table: "LadClaims", type: "nvarchar(2048)", maxLength: 2048, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Summary", table: "LeadActivities", type: "nvarchar(4000)", maxLength: 4000, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Description", table: "LeadEstimateLines", type: "nvarchar(1024)", maxLength: 1024, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Exclusions", table: "LeadEstimates", type: "nvarchar(4000)", maxLength: 4000, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Notes", table: "LeadEstimates", type: "nvarchar(4000)", maxLength: 4000, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Scope", table: "LeadEstimates", type: "nvarchar(4000)", maxLength: 4000, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "LostReason", table: "Leads", type: "nvarchar(1024)", maxLength: 1024, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Notes", table: "Leads", type: "nvarchar(4000)", maxLength: 4000, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Summary", table: "Leads", type: "nvarchar(512)", maxLength: 512, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Description", table: "MobilisationItems", type: "nvarchar(512)", maxLength: 512, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Summary", table: "PolicyDocuments", type: "nvarchar(4096)", maxLength: 4096, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Note", table: "ProgrammeVariationEffects", type: "nvarchar(512)", maxLength: 512, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Introduction", table: "ProgressReports", type: "nvarchar(4096)", maxLength: 4096, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "UpcomingWorks", table: "ProgressReports", type: "nvarchar(4096)", maxLength: 4096, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "WorkCompleted", table: "ProgressReports", type: "nvarchar(4096)", maxLength: 4096, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Notes", table: "ProjectContractAmendments", type: "nvarchar(4000)", maxLength: 4000, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "BespokeDeviations", table: "ProjectContracts", type: "nvarchar(4000)", maxLength: 4000, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Description", table: "QsAccruals", type: "nvarchar(1024)", maxLength: 1024, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Description", table: "QuoteLineItems", type: "nvarchar(512)", maxLength: 512, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Notes", table: "Quotes", type: "nvarchar(1024)", maxLength: 1024, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Response", table: "RequestItems", type: "nvarchar(4000)", maxLength: 4000, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Body", table: "RequestMessages", type: "nvarchar(4000)", maxLength: 4000, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "ClientNotes", table: "Requests", type: "nvarchar(4000)", maxLength: 4000, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "InternalNotes", table: "Requests", type: "nvarchar(4000)", maxLength: 4000, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "RelatedDrawingSpec", table: "Requests", type: "nvarchar(512)", maxLength: 512, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Notes", table: "RightToWorkChecks", type: "nvarchar(2000)", maxLength: 2000, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "DeclineReason", table: "SalesProposals", type: "nvarchar(1024)", maxLength: 1024, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Brief", table: "SalesStrategies", type: "nvarchar(4000)", maxLength: 4000, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Evidence", table: "SalesStrategies", type: "nvarchar(4000)", maxLength: 4000, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Hypothesis", table: "SalesStrategies", type: "nvarchar(4000)", maxLength: 4000, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Proposition", table: "SalesStrategies", type: "nvarchar(1024)", maxLength: 1024, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Instruction", table: "SiteInstructions", type: "nvarchar(4000)", maxLength: 4000, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "ArchiveNote", table: "SitePhotos", type: "nvarchar(1024)", maxLength: 1024, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Narrative", table: "SiteReports", type: "nvarchar(4096)", maxLength: 4096, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "RejectionReason", table: "SubcontractorVariationRequests", type: "nvarchar(1024)", maxLength: 1024, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "DecisionNote", table: "TenderEnquiries", type: "nvarchar(2048)", maxLength: 2048, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "ScopeSummary", table: "TenderEnquiries", type: "nvarchar(4000)", maxLength: 4000, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "RejectionReason", table: "Timesheets", type: "nvarchar(1024)", maxLength: 1024, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Notes", table: "TodoItems", type: "nvarchar(2048)", maxLength: 2048, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Body", table: "UsefulInformationNotes", type: "nvarchar(4000)", maxLength: 4000, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Note", table: "ValuationInvoiceEvents", type: "nvarchar(1024)", maxLength: 1024, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "RejectionReason", table: "ValuationInvoices", type: "nvarchar(1024)", maxLength: 1024, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Comments", table: "ValuationLineItems", type: "nvarchar(512)", maxLength: 512, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Description", table: "ValuationLineItems", type: "nvarchar(512)", maxLength: 512, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Body", table: "VariationOrderMessages", type: "nvarchar(4000)", maxLength: 4000, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "CommercialBasis", table: "VariationOrderQuotes", type: "nvarchar(4000)", maxLength: 4000, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Exclusions", table: "VariationOrderQuotes", type: "nvarchar(4000)", maxLength: 4000, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "ProgrammeImpact", table: "VariationOrderQuotes", type: "nvarchar(4000)", maxLength: 4000, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Notes", table: "VatAnalyses", type: "nvarchar(2048)", maxLength: 2048, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Notes", table: "WalkRoundNotes", type: "nvarchar(4096)", maxLength: 4096, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Notes", table: "WeeklyCashflowItems", type: "nvarchar(1000)", maxLength: 1000, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Description", table: "WorkOrderLines", type: "nvarchar(1024)", maxLength: 1024, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "ProgrammeNotes", table: "WorkOrders", type: "nvarchar(2000)", maxLength: 2000, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Scope", table: "WorkOrders", type: "nvarchar(4000)", maxLength: 4000, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Note", table: "WorkerAbsences", type: "nvarchar(512)", maxLength: 512, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Note", table: "WorkerSettlementLines", type: "nvarchar(512)", maxLength: 512, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Note", table: "WorkstationActions", type: "nvarchar(1000)", maxLength: 1000, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Body", table: "XeroDisputeMessages", type: "nvarchar(2048)", maxLength: 2048, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Description", table: "XeroLedgerLines", type: "nvarchar(1024)", maxLength: 1024, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Note", table: "XeroLedgerLines", type: "nvarchar(512)", maxLength: 512, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Name", table: "ReconciliationPackages", type: "nvarchar(128)", maxLength: 128, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(256)", oldMaxLength: 256);
        }
    }
}
