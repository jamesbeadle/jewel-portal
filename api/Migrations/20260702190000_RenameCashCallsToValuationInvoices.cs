using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Cash calls are really the client-facing valuation invoices raised against the valuation
    // report, so the whole feature is renamed end-to-end:
    //  - CashCalls table -> ValuationInvoices, with the lifecycle columns renamed to match the new
    //    statuses (Raised -> Issued -> Paid): AmountRequested -> Amount, AmountReceived -> AmountPaid,
    //    RequestedAt -> RaisedAt, InvoicedAt -> IssuedAt, ReceivedAt -> PaidAt. Status ints are
    //    unchanged (0/1/2), only their meaning labels moved.
    //  - Projects.CashCallTotal -> ValuationInvoicePaidTotal.
    //  - Existing references are re-prefixed CC- -> VI- so history reads consistently.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260702190000_RenameCashCallsToValuationInvoices")]
    public partial class RenameCashCallsToValuationInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(name: "CashCalls", newName: "ValuationInvoices");

            migrationBuilder.RenameColumn(name: "CashCallId", table: "ValuationInvoices", newName: "ValuationInvoiceId");
            migrationBuilder.RenameColumn(name: "AmountRequested", table: "ValuationInvoices", newName: "Amount");
            migrationBuilder.RenameColumn(name: "AmountReceived", table: "ValuationInvoices", newName: "AmountPaid");
            migrationBuilder.RenameColumn(name: "RequestedAt", table: "ValuationInvoices", newName: "RaisedAt");
            migrationBuilder.RenameColumn(name: "InvoicedAt", table: "ValuationInvoices", newName: "IssuedAt");
            migrationBuilder.RenameColumn(name: "ReceivedAt", table: "ValuationInvoices", newName: "PaidAt");

            migrationBuilder.RenameColumn(name: "CashCallTotal", table: "Projects", newName: "ValuationInvoicePaidTotal");

            migrationBuilder.Sql("UPDATE ValuationInvoices SET Reference = REPLACE(Reference, 'CC-', 'VI-') WHERE Reference LIKE 'CC-%';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE ValuationInvoices SET Reference = REPLACE(Reference, 'VI-', 'CC-') WHERE Reference LIKE 'VI-%';");

            migrationBuilder.RenameColumn(name: "ValuationInvoicePaidTotal", table: "Projects", newName: "CashCallTotal");

            migrationBuilder.RenameColumn(name: "PaidAt", table: "ValuationInvoices", newName: "ReceivedAt");
            migrationBuilder.RenameColumn(name: "IssuedAt", table: "ValuationInvoices", newName: "InvoicedAt");
            migrationBuilder.RenameColumn(name: "RaisedAt", table: "ValuationInvoices", newName: "RequestedAt");
            migrationBuilder.RenameColumn(name: "AmountPaid", table: "ValuationInvoices", newName: "AmountReceived");
            migrationBuilder.RenameColumn(name: "Amount", table: "ValuationInvoices", newName: "AmountRequested");
            migrationBuilder.RenameColumn(name: "ValuationInvoiceId", table: "ValuationInvoices", newName: "CashCallId");

            migrationBuilder.RenameTable(name: "ValuationInvoices", newName: "CashCalls");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
