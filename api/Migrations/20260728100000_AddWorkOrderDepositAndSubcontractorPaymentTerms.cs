using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Deposit + payment terms on the printed purchase order:
    //  - WorkOrders gains DepositRequired / DepositPercent — a deposit the supplier requires,
    //    recorded as a percentage of the order value only (never a £ figure) and printed at the
    //    foot of the PO. Captured on manually raised orders via the Add/Edit work order form;
    //    Percent stays null unless the flag is set.
    //  - Subcontractors gains PaymentTermsDays — the "N day terms" printed in the PO's Invoice
    //    and Payment Requirements section (previously hard-coded to 30). Every company defaults
    //    to 30 days, overridable per record from the directory's Edit details dialog.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260728100000_AddWorkOrderDepositAndSubcontractorPaymentTerms")]
    public partial class AddWorkOrderDepositAndSubcontractorPaymentTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DepositRequired", table: "WorkOrders", type: "bit",
                nullable: false, defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositPercent", table: "WorkOrders", type: "decimal(18,4)",
                precision: 18, scale: 4, nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentTermsDays", table: "Subcontractors", type: "int",
                nullable: false, defaultValue: 30);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "DepositRequired", table: "WorkOrders");
            migrationBuilder.DropColumn(name: "DepositPercent", table: "WorkOrders");
            migrationBuilder.DropColumn(name: "PaymentTermsDays", table: "Subcontractors");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
