using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Adds direct purchase costs to reconciliation packages: a £ slice of an allocated
    /// Xero purchase line that isn't paying any work order — materials bought directly
    /// for the packaged scope (roofing supplies alongside the roofer's labour-only
    /// order). Without this, a package could gather the sales lines and work orders but
    /// the direct spend stayed stranded on the cost centre. Slices are signed like the
    /// line's net; across all packages they may never exceed the line's non-work-order
    /// remainder, and only whole-line allocations qualify (centre-split lines can't be
    /// sliced — the same rule as work-order links). Presentation only, like the rest of
    /// the package tables.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260716110000_AddReconciliationPackageCostLines")]
    public partial class AddReconciliationPackageCostLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReconciliationPackageCostLines",
                columns: table => new
                {
                    ReconciliationPackageCostLineId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ReconciliationPackageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    // 140 like XeroLineWorkOrderLinks / XeroCostSplits — the id is
                    // "{TransactionId}:{LineItemId}", two GUIDs plus a colon.
                    XeroLedgerLineId = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationPackageCostLines", x => x.ReconciliationPackageCostLineId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationPackageCostLines_ReconciliationPackageId",
                table: "ReconciliationPackageCostLines", column: "ReconciliationPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationPackageCostLines_ProjectId",
                table: "ReconciliationPackageCostLines", column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationPackageCostLines_XeroLedgerLineId",
                table: "ReconciliationPackageCostLines", column: "XeroLedgerLineId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ReconciliationPackageCostLines");
        }
    }
}
