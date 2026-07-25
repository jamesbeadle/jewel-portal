using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Read-path indexes for the columns the hot queries filter and join on.
    //
    // Why these were missing: JPMS links records by loose string id and declares no EF navigation
    // properties or FK constraints, so EF's automatic FK-index convention never fired. Requests,
    // VariationOrderQuotes and WorkOrders — three of the busiest tables in the product — carried no
    // index at all, which means every "the requests on this project" read was a table scan whose
    // cost grows with the data and multiplies under concurrency. That is the most likely cause of
    // the intermittent slowness, and it is already documented once: infra/perf-financials-indexes.sql
    // was hand-run against production after the financials query started returning 504s at the
    // Static Web Apps managed-functions gateway timeout (~45s).
    //
    // Every statement is written as guarded raw SQL rather than migrationBuilder.CreateIndex for
    // two reasons:
    //   1. Three of these indexes ALREADY EXIST in production (the hand-run script above), so a
    //      plain CREATE INDEX would fail the whole migration on the one database that matters.
    //   2. The same guard makes the migration safe to re-run and safe on a freshly built schema.
    // The three pre-existing ones keep the exact names the hand-run script used, so they are
    // adopted rather than duplicated — and they are recreated here WITH their INCLUDE columns, so
    // a rebuilt-from-migrations database gets the same covering indexes production already has.
    // (INCLUDE is a storage detail; it is not modelled in JpmsContext and EF never validates it.)
    [DbContext(typeof(JpmsContext))]
    [Migration("20260725100000_AddPerformanceIndexes")]
    public partial class AddPerformanceIndexes : Migration
    {
        // (table, index name, index definition tail) — kept as data so Up/Down stay symmetrical.
        private static readonly (string Table, string Name, string Definition)[] Indexes =
        {
            // ---- Auth: read on every single authenticated request ----------------------------
            // UserSessions.SessionId and DirectoryUsers.Email are primary keys and already seek;
            // the role list was the one scanning on every call.
            ("DirectoryUserRoles", "IX_DirectoryUserRoles_DirectoryUserEmail",
                "(DirectoryUserEmail) INCLUDE (Role)"),

            // ---- Requests / RFIs ---------------------------------------------------------------
            ("Requests", "IX_Requests_ProjectId_Status", "(ProjectId, Status)"),
            ("Requests", "IX_Requests_Kind_Status",      "(Kind, Status)"),
            ("RequestMessages", "IX_RequestMessages_RequestId", "(RequestId)"),

            // ---- Variations (table keeps its historic VariationOrderQuotes name) ---------------
            ("VariationOrderQuotes", "IX_VariationOrderQuotes_ProjectId", "(ProjectId)"),
            ("VariationOrderQuotes", "IX_VariationOrderQuotes_RequestId", "(RequestId)"),

            // ---- Procurement --------------------------------------------------------------------
            ("WorkOrders", "IX_WorkOrders_ProjectId",        "(ProjectId)"),
            ("WorkOrders", "IX_WorkOrders_VariationOrderId", "(VariationOrderId)"),
            ("BidPackages", "IX_BidPackages_ProjectId",      "(ProjectId)"),
            // BidPackages.VariationOrderId maps to the historic VariationOrderQuoteId column.
            ("BidPackages", "IX_BidPackages_VariationOrderQuoteId", "(VariationOrderQuoteId)"),
            ("Quotes", "IX_Quotes_BidPackageId", "(BidPackageId)"),

            // Already in production from infra/perf-financials-indexes.sql — same name, same
            // definition, so this adopts it rather than creating a second copy.
            ("WorkOrderLines", "IX_WorkOrderLines_WorkOrderId",
                "(WorkOrderId) INCLUDE (CostCode, LineTotal)"),

            // ---- Labour / financials -------------------------------------------------------------
            // Also already in production from the hand-run script.
            ("Timesheets", "IX_Timesheets_ProjectId_Status",
                "(ProjectId, Status) INCLUDE (CostCode, CostAmount, WorkerId, Hours)"),
            ("XeroLineTimesheetCovers", "IX_XeroLineTimesheetCovers_XeroLedgerLineId",
                "(XeroLedgerLineId)"),
            ("SiteAttendances", "IX_SiteAttendances_ProjectId_WorkDate", "(ProjectId, WorkDate)"),

            // ---- Project-scoped registers ---------------------------------------------------------
            ("Drawings", "IX_Drawings_ProjectId",                 "(ProjectId)"),
            ("DrawingRevisions", "IX_DrawingRevisions_DrawingId", "(DrawingId)"),
            ("HsRecords", "IX_HsRecords_ProjectId",               "(ProjectId)"),
            ("TodoItems", "IX_TodoItems_ProjectId",               "(ProjectId)"),
            ("Defects", "IX_Defects_ProjectId",                   "(ProjectId)"),
            ("ComplianceDocuments", "IX_ComplianceDocuments_SubcontractorId", "(SubcontractorId)"),
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var (table, name, definition) in Indexes)
            {
                migrationBuilder.Sql($"""
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                                   WHERE name = N'{name}'
                                     AND object_id = OBJECT_ID(N'dbo.{table}'))
                    BEGIN
                        CREATE INDEX {name} ON dbo.{table} {definition};
                    END;
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var (table, name, _) in Indexes)
            {
                migrationBuilder.Sql($"""
                    IF EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'{name}'
                                 AND object_id = OBJECT_ID(N'dbo.{table}'))
                    BEGIN
                        DROP INDEX {name} ON dbo.{table};
                    END;
                    """);
            }
        }
    }
}
