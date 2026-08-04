using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // The stored site P&L: one row per project per month, pulled from Xero's profit & loss
    // report filtered by the project's "Sites" tracking option (SyncXeroSitePnlHandler — the
    // nightly worker refreshes it, the Profit Summary's Refresh button re-pulls on demand).
    // Feeds the Profit Summary's cumulative invoiced-vs-cost chart, which replaced the
    // reconstructed margin-% lines (accountant's request, 2026-08-04): the accounts' own
    // monthly figures rather than a client-side reconstruction from certification dates.
    // Purely additive — deploy order with the code doesn't matter, but run it first anyway.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260804120000_AddXeroSitePnlMonths")]
    public partial class AddXeroSitePnlMonths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "XeroSitePnlMonths",
                columns: table => new
                {
                    XeroSitePnlMonthId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Month = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Income = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CostOfSales = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OperatingExpenses = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LastSyncedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_XeroSitePnlMonths", x => x.XeroSitePnlMonthId));

            migrationBuilder.CreateIndex(
                name: "IX_XeroSitePnlMonths_ProjectId",
                table: "XeroSitePnlMonths",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "XeroSitePnlMonths");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
