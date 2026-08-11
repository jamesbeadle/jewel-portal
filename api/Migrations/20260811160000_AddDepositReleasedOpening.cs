using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Deposit releases settled before the portal began deducting them from claims
    // (Ravenswood: £6,049 covering claims 1–2, which were invoiced gross). Stored on the
    // terms and stamped onto each claim; a claim's deduction is the release earned to
    // date less this opening balance, so the invoice raised matches what the client
    // actually pays. 0 for every project that starts tracking from day one.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260811160000_AddDepositReleasedOpening")]
    public partial class AddDepositReleasedOpening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DepositReleasedOpening", table: "ProjectRetentions", type: "decimal(18,4)",
                precision: 18, scale: 4, nullable: false, defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositReleasedOpening", table: "ValuationClaims", type: "decimal(18,4)",
                precision: 18, scale: 4, nullable: false, defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "DepositReleasedOpening", table: "ProjectRetentions");
            migrationBuilder.DropColumn(name: "DepositReleasedOpening", table: "ValuationClaims");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
