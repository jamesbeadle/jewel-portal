using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Adds reconciliation packages: named per-project groupings that match work orders
    /// (cost side) against valuation sales lines (sales side) at the level the work was
    /// actually bought and sold — because a sub's single order routinely spans several
    /// cost centres while the client valuation prices the same scope on differently-shaped
    /// lines. Sales lines join whole or as a partial £ slice (a client line can cover more
    /// than one sub's scope); a work order sits in at most one package. Locking freezes a
    /// snapshot and realises profit / loss against actual invoiced cost. Presentation only —
    /// nothing writes to Xero and dissolving a package changes nothing underneath.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260711120000_AddReconciliationPackages")]
    public partial class AddReconciliationPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReconciliationPackages",
                columns: table => new
                {
                    ReconciliationPackageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LockedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockedSalesValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    LockedClaimedToDate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    LockedTargetCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    LockedWoCommitted = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    LockedInvoicedCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    LockedProfitLoss = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationPackages", x => x.ReconciliationPackageId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationPackages_ProjectId",
                table: "ReconciliationPackages", column: "ProjectId");

            migrationBuilder.CreateTable(
                name: "ReconciliationPackageOrders",
                columns: table => new
                {
                    ReconciliationPackageOrderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ReconciliationPackageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkOrderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationPackageOrders", x => x.ReconciliationPackageOrderId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationPackageOrders_ReconciliationPackageId",
                table: "ReconciliationPackageOrders", column: "ReconciliationPackageId");

            migrationBuilder.CreateIndex(
                name: "UX_ReconciliationPackageOrders_Project_Order",
                table: "ReconciliationPackageOrders", columns: new[] { "ProjectId", "WorkOrderId" }, unique: true);

            migrationBuilder.CreateTable(
                name: "ReconciliationPackageSalesLines",
                columns: table => new
                {
                    ReconciliationPackageSalesLineId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ReconciliationPackageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ValuationLineItemId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationPackageSalesLines", x => x.ReconciliationPackageSalesLineId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationPackageSalesLines_ReconciliationPackageId",
                table: "ReconciliationPackageSalesLines", column: "ReconciliationPackageId");

            migrationBuilder.CreateIndex(
                name: "UX_ReconciliationPackageSalesLines_Package_Line",
                table: "ReconciliationPackageSalesLines", columns: new[] { "ReconciliationPackageId", "ValuationLineItemId" }, unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationPackageSalesLines_ValuationLineItemId",
                table: "ReconciliationPackageSalesLines", column: "ValuationLineItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ReconciliationPackageSalesLines");
            migrationBuilder.DropTable(name: "ReconciliationPackageOrders");
            migrationBuilder.DropTable(name: "ReconciliationPackages");
        }
    }
}
