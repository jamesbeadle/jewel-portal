using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Variation orders are now ONE document with ONE lifecycle: Quoting (0) → Issued (1) →
    // Approved (2) / Rejected (3). A "VOQ" was this document in its quoting stage, so the
    // VariationOrderQuotes row becomes the variation order itself and the separate VariationOrders
    // table (the row approval used to mint) is folded in and dropped. The table and its key column
    // keep their historic names — persisted identifiers survive renames (see CLAUDE.md).
    //
    // Contract-stage columns move onto the surviving row: VariationRef, Value, CostCode, IssuedAt,
    // RejectedAt (the old VO's CancelledAt).
    //
    // Status remap, in the new values:
    //   old VOQ Draft(0)/Inviting(1)/Tendering(2)/Selected(3)  -> Quoting (0)  — all tendering
    //     activity is the Quoting stage now; the recorded tender itself survives on the row.
    //   old VOQ Rejected(5)                                    -> Rejected (3)
    //   old VOQ Approved(4) + VO Approved(0) or Issued(1)      -> Approved (2) — an old "Issued"
    //     VO was approved-then-instructed; under the new meaning (issued to the CLIENT, awaiting
    //     decision) it is simply Approved. Its instruction survives as the linked work orders.
    //   old VOQ Approved(4) + VO Cancelled(2)                  -> Rejected (3) — cancellation
    //     already reversed the approval's valuation/CVR/budget writes, which is exactly what
    //     rejecting an approved variation now means. CancelledAt becomes RejectedAt.
    //   old VOQ Approved(4) with no VO row (seed anomaly)      -> Approved (2)
    //
    // WorkOrders.VariationOrderId and AuditEvents.RecordId (RecordType 6 = Variation) carried the
    // old VO row's id — both are re-pointed at the surviving document's id.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260723120000_UnifyVariationOrders")]
    public partial class UnifyVariationOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VariationRef",
                table: "VariationOrderQuotes",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Value",
                table: "VariationOrderQuotes",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CostCode",
                table: "VariationOrderQuotes",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "IssuedAt",
                table: "VariationOrderQuotes",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RejectedAt",
                table: "VariationOrderQuotes",
                type: "datetimeoffset",
                nullable: true);

            // Fold the approval row's contract data onto the surviving document. At most one VO
            // ever existed per VOQ (approve refused an already-approved VOQ). Seeded approvals
            // sometimes stamped only the VO, hence the COALESCEs on the approval stamps.
            migrationBuilder.Sql(@"
UPDATE q SET
    q.[VariationRef]            = vo.[VariationRef],
    q.[Value]                   = vo.[Value],
    q.[CostCode]                = vo.[CostCode],
    q.[IssuedAt]                = vo.[IssuedAt],
    q.[RejectedAt]              = vo.[CancelledAt],
    q.[ApprovedAt]              = COALESCE(q.[ApprovedAt], vo.[ApprovedAt]),
    q.[ApprovedByEmail]         = COALESCE(q.[ApprovedByEmail], vo.[ApprovedByEmail]),
    q.[SelectedSubcontractorId] = COALESCE(q.[SelectedSubcontractorId], vo.[SubcontractorId])
FROM [VariationOrderQuotes] q
INNER JOIN [VariationOrders] vo ON vo.[VariationOrderQuoteId] = q.[VariationOrderQuoteId];");

            migrationBuilder.Sql(@"
UPDATE q SET q.[Status] = CASE
    WHEN q.[Status] IN (0, 1, 2, 3) THEN 0
    WHEN q.[Status] = 5 THEN 3
    WHEN q.[Status] = 4 AND EXISTS (
        SELECT 1 FROM [VariationOrders] vo
        WHERE vo.[VariationOrderQuoteId] = q.[VariationOrderQuoteId] AND vo.[Status] = 2) THEN 3
    ELSE 2
END
FROM [VariationOrderQuotes] q;");

            // Work orders instructed from a VO pointed at the approval row — re-point them at the
            // surviving document.
            migrationBuilder.Sql(@"
UPDATE w SET w.[VariationOrderId] = vo.[VariationOrderQuoteId]
FROM [WorkOrders] w
INNER JOIN [VariationOrders] vo ON w.[VariationOrderId] = vo.[VariationOrderId];");

            // Audit history for RecordType 6 (Variation) carried the approval row's id — re-point
            // so the append-only trail still resolves to the record it describes.
            migrationBuilder.Sql(@"
UPDATE a SET a.[RecordId] = vo.[VariationOrderQuoteId]
FROM [AuditEvents] a
INNER JOIN [VariationOrders] vo ON a.[RecordId] = vo.[VariationOrderId]
WHERE a.[RecordType] = 6;");

            migrationBuilder.DropTable(
                name: "VariationOrders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Structural reverse only — the status consolidation is irreversible by design (there
            // is no record of which Quoting rows were Draft/Inviting/Tendering/Selected), matching
            // the ConsolidateRequestStatuses precedent. Recreates the old VariationOrders table
            // empty and drops the folded-in columns.
            migrationBuilder.CreateTable(
                name: "VariationOrders",
                columns: table => new
                {
                    VariationOrderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    VariationOrderQuoteId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    VariationRef = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SubcontractorId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CostCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApprovedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariationOrders", x => x.VariationOrderId);
                });

            migrationBuilder.DropColumn(name: "VariationRef", table: "VariationOrderQuotes");
            migrationBuilder.DropColumn(name: "Value", table: "VariationOrderQuotes");
            migrationBuilder.DropColumn(name: "CostCode", table: "VariationOrderQuotes");
            migrationBuilder.DropColumn(name: "IssuedAt", table: "VariationOrderQuotes");
            migrationBuilder.DropColumn(name: "RejectedAt", table: "VariationOrderQuotes");
        }
    }
}
