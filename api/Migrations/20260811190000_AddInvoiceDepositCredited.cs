using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Certification runs GROSS of the cash-up-front deposit (how the QS and the
    // accountant certify): each invoice records the deposit credit embedded in its cash
    // amount, so the gross certificate = Amount + DepositCredited. "Certified to date"
    // sums gross certificates, and a claim's outstanding deposit deduction nets off the
    // credits already taken — returning to zero once the period's invoice is issued.
    // Stamped from the claim's outstanding deduction when an invoice is raised; 0 for
    // manual/historic entries and every pre-deposit invoice.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260811190000_AddInvoiceDepositCredited")]
    public partial class AddInvoiceDepositCredited : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DepositCredited", table: "ValuationInvoices", type: "decimal(18,4)",
                precision: 18, scale: 4, nullable: false, defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "DepositCredited", table: "ValuationInvoices");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
