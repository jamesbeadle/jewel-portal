using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Replaces the one-to-one XeroLedgerLines.LinkedWorkOrderId with the
    /// XeroLineWorkOrderLinks table so one purchase invoice line can be split by amount
    /// across several work orders (a subcontractor billing a main order plus variation
    /// orders on one invoice). Existing whole-line links are carried over as a single
    /// full-amount slice — Amount is signed like the line's net, credit notes negative —
    /// then the old column is dropped.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260710180000_AddXeroLineWorkOrderLinkSplits")]
    public partial class AddXeroLineWorkOrderLinkSplits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "XeroLineWorkOrderLinks",
                columns: table => new
                {
                    XeroLineWorkOrderLinkId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    XeroLedgerLineId = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    WorkOrderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XeroLineWorkOrderLinks", x => x.XeroLineWorkOrderLinkId);
                });

            migrationBuilder.CreateIndex(
                name: "UX_XeroLineWorkOrderLinks_Line_Order",
                table: "XeroLineWorkOrderLinks", columns: new[] { "XeroLedgerLineId", "WorkOrderId" }, unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_XeroLineWorkOrderLinks_WorkOrderId",
                table: "XeroLineWorkOrderLinks", column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_XeroLineWorkOrderLinks_ProjectId",
                table: "XeroLineWorkOrderLinks", column: "ProjectId");

            // Carry existing whole-line links over as one full-amount slice each.
            migrationBuilder.Sql(@"
INSERT INTO XeroLineWorkOrderLinks (XeroLineWorkOrderLinkId, XeroLedgerLineId, WorkOrderId, ProjectId, Amount)
SELECT LOWER(REPLACE(CONVERT(nvarchar(36), NEWID()), '-', '')),
       XeroLedgerLineId,
       LinkedWorkOrderId,
       ProjectId,
       CASE WHEN [Type] = 'ACCPAYCREDIT' THEN -Net ELSE Net END
FROM XeroLedgerLines
WHERE LinkedWorkOrderId IS NOT NULL AND ProjectId IS NOT NULL;
");

            migrationBuilder.DropColumn(name: "LinkedWorkOrderId", table: "XeroLedgerLines");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkedWorkOrderId",
                table: "XeroLedgerLines",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            // Best effort: lines with exactly one slice get it back as a whole-line link;
            // genuine multi-order splits can't be represented and are dropped.
            migrationBuilder.Sql(@"
UPDATE lines
SET LinkedWorkOrderId = links.WorkOrderId
FROM XeroLedgerLines AS lines
JOIN XeroLineWorkOrderLinks AS links ON links.XeroLedgerLineId = lines.XeroLedgerLineId
WHERE (SELECT COUNT(*) FROM XeroLineWorkOrderLinks AS counting
       WHERE counting.XeroLedgerLineId = lines.XeroLedgerLineId) = 1;
");

            migrationBuilder.DropTable(name: "XeroLineWorkOrderLinks");
        }
    }
}
