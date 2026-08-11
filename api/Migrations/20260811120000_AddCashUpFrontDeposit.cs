using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Cash-up-front deposit (the Ravenswood model): the client pays deposit % of the
    // contract sum before works start, and it is released back to them pro rata against
    // each valuation's contract-side works (contract works + PC sums + contingency —
    // variations excluded), reducing the payment due on every claim.
    //  - ProjectRetentions gains DepositPercent — the term, alongside the retention terms
    //    on the Deposits, retentions & valuation tab. 0 = no deposit (every existing row).
    //  - ValuationClaims gains DepositPercent (stamped from terms at claim start, kept
    //    live on Drafts) and DepositReleased (cumulative, frozen when the claim locks).
    //  - ValuationReportSnapshots gains the same pair, frozen at capture, so submitted
    //    reports keep showing the deposit deduction they were sent with.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260811120000_AddCashUpFrontDeposit")]
    public partial class AddCashUpFrontDeposit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DepositPercent", table: "ProjectRetentions", type: "decimal(18,4)",
                precision: 18, scale: 4, nullable: false, defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositPercent", table: "ValuationClaims", type: "decimal(18,4)",
                precision: 18, scale: 4, nullable: false, defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositReleased", table: "ValuationClaims", type: "decimal(18,4)",
                precision: 18, scale: 4, nullable: false, defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositPercent", table: "ValuationReportSnapshots", type: "decimal(18,4)",
                precision: 18, scale: 4, nullable: false, defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositReleased", table: "ValuationReportSnapshots", type: "decimal(18,4)",
                precision: 18, scale: 4, nullable: false, defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "DepositPercent", table: "ProjectRetentions");
            migrationBuilder.DropColumn(name: "DepositPercent", table: "ValuationClaims");
            migrationBuilder.DropColumn(name: "DepositReleased", table: "ValuationClaims");
            migrationBuilder.DropColumn(name: "DepositPercent", table: "ValuationReportSnapshots");
            migrationBuilder.DropColumn(name: "DepositReleased", table: "ValuationReportSnapshots");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
