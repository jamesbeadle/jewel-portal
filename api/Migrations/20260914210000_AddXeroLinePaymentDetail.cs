using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The bill's payment detail, carried onto every stored line of it beside InvoiceTotal and
    /// AmountDue (2026-09-14, the accountant's ask: the supplier account must show the actual
    /// payment made and the CIS deducted). AmountPaid is the cash Xero recorded against the bill,
    /// CisDeduction what Xero calculated and withheld for HMRC, FullyPaidOnDate when nothing
    /// further was owed. The two money columns default to 0 and the date to NULL, so a line synced
    /// before this deploy reads "no detail yet" until the next ledger sync fills it — additive,
    /// self-healing, safe to apply before or with the deploy. Scoped script:
    /// api/Migrations/add-xero-line-payment-detail.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260914210000_AddXeroLinePaymentDetail")]
    public partial class AddXeroLinePaymentDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountPaid",
                table: "XeroLedgerLines",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CisDeduction",
                table: "XeroLedgerLines",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "FullyPaidOnDate",
                table: "XeroLedgerLines",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AmountPaid", table: "XeroLedgerLines");
            migrationBuilder.DropColumn(name: "CisDeduction", table: "XeroLedgerLines");
            migrationBuilder.DropColumn(name: "FullyPaidOnDate", table: "XeroLedgerLines");
        }
    }
}
