using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Widens a claim line's % complete from decimal(18,4) to decimal(28,20) — on the live
    /// ClaimLines row and on its frozen ValuationReportSnapshotLines copy. A line's % is whatever
    /// reproduces its claimed value (% x line amount), and four decimal places clipped it: a QS
    /// working a claimed amount back to a % of a large line got the report out by pennies. Twenty
    /// decimal places keep any % a user can type or derive; 28 digits total is what a .NET decimal
    /// round-trips exactly. Pure widening — every existing value is preserved — so it is safe to
    /// apply before the deploy (2026-09-02).
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260902100000_WidenClaimPercentPrecision")]
    public partial class WidenClaimPercentPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "PercentComplete",
                table: "ClaimLines",
                type: "decimal(28,20)",
                precision: 28,
                scale: 20,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "PercentComplete",
                table: "ValuationReportSnapshotLines",
                type: "decimal(28,20)",
                precision: 28,
                scale: 20,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Narrowing rounds any value with more than four decimal places back to four.
            migrationBuilder.AlterColumn<decimal>(
                name: "PercentComplete",
                table: "ClaimLines",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(28,20)",
                oldPrecision: 28,
                oldScale: 20);

            migrationBuilder.AlterColumn<decimal>(
                name: "PercentComplete",
                table: "ValuationReportSnapshotLines",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(28,20)",
                oldPrecision: 28,
                oldScale: 20);
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
