using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Profit / Loss is now measured against Contract Sales Value, not target cost (agreed
    // 2026-07): a locked line's profit is what it sold for less what it cost, not what was
    // left of its markup-backed-out budget. SetReconciliationPackageLockHandler banks
    // sales − invoiced for locks from here on; this data-only migration recomputes every
    // already-locked package from its stored lock snapshot so the whole Profit / Loss
    // column reads the same way. Unlocked packages carry no banked figure and are untouched.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260721100000_RecomputeLockedPackageProfitToSales")]
    public partial class RecomputeLockedPackageProfitToSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE [ReconciliationPackages] SET [LockedProfitLoss] = [LockedSalesValue] - [LockedInvoicedCost] WHERE [IsLocked] = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The old target-cost basis is still derivable from the snapshot, so Down
            // restores it exactly.
            migrationBuilder.Sql(
                "UPDATE [ReconciliationPackages] SET [LockedProfitLoss] = [LockedTargetCost] - [LockedInvoicedCost] WHERE [IsLocked] = 1;");
        }
    }
}
