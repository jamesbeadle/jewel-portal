using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // The bill's payment state, carried onto every stored line of it.
    //
    // Xero holds AmountDue and Total per INVOICE; XeroLedgerLines stores one row per LINE, so
    // both are repeated on each line of the bill exactly as InvoiceStatus already is. Without
    // them a work order can be fully invoiced and still read as nothing paid, because the only
    // "paid" figure JPMS held was WorkOrderLines.PaidToDate — a Buildertrend opening balance
    // that nothing has ever written since. See XeroPaymentMaths / WorkOrderPaidPositions.
    //
    // Both default to 0. That is deliberate and self-healing: a line whose amounts have not yet
    // been synced has InvoiceTotal 0, and the paid maths falls back to InvoiceStatus for it —
    // so every already-PAID bill reads correctly the moment this deploys, and gains part-payment
    // precision on the next ledger sync without a backfill.
    //
    // Guarded raw SQL, matching the house pattern: safe to re-run.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260727120000_AddXeroLinePaymentState")]
    public partial class AddXeroLinePaymentState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'[dbo].[XeroLedgerLines]', N'InvoiceTotal') IS NULL
BEGIN
    ALTER TABLE [dbo].[XeroLedgerLines]
        ADD [InvoiceTotal] decimal(18,4) NOT NULL
            CONSTRAINT [DF_XeroLedgerLines_InvoiceTotal] DEFAULT (0);
END;
");

            migrationBuilder.Sql(@"
IF COL_LENGTH(N'[dbo].[XeroLedgerLines]', N'AmountDue') IS NULL
BEGIN
    ALTER TABLE [dbo].[XeroLedgerLines]
        ADD [AmountDue] decimal(18,4) NOT NULL
            CONSTRAINT [DF_XeroLedgerLines_AmountDue] DEFAULT (0);
END;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'[dbo].[XeroLedgerLines]', N'InvoiceTotal') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[XeroLedgerLines] DROP CONSTRAINT [DF_XeroLedgerLines_InvoiceTotal];
    ALTER TABLE [dbo].[XeroLedgerLines] DROP COLUMN [InvoiceTotal];
END;
");

            migrationBuilder.Sql(@"
IF COL_LENGTH(N'[dbo].[XeroLedgerLines]', N'AmountDue') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[XeroLedgerLines] DROP CONSTRAINT [DF_XeroLedgerLines_AmountDue];
    ALTER TABLE [dbo].[XeroLedgerLines] DROP COLUMN [AmountDue];
END;
");
        }
    }
}
