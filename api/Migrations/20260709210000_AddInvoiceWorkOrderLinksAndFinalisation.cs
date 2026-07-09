using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Two Financials-tab additions. XeroLedgerLines.LinkedWorkOrderId ties an allocated
    /// purchase line to the work order it pays against (null = non-work-order cost of
    /// sales; drawdown = target cost − work orders − those unlinked costs).
    /// CostCentreCostProgress.IsFinalised locks a centre down: no more spend expected, so
    /// its remaining drawdown reads as realised profit / loss instead of available funds.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260709210000_AddInvoiceWorkOrderLinksAndFinalisation")]
    public partial class AddInvoiceWorkOrderLinksAndFinalisation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkedWorkOrderId",
                table: "XeroLedgerLines",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFinalised",
                table: "CostCentreCostProgress",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "LinkedWorkOrderId", table: "XeroLedgerLines");
            migrationBuilder.DropColumn(name: "IsFinalised", table: "CostCentreCostProgress");
        }
    }
}
