using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Bid packages — cost-centre linkage + materials question.
    //  - BidPackageLineItems gains CostCode (nvarchar(32), NOT NULL, default ''): the cost centre
    //    (from the master list) each tendered line's committed value lands on. Required by the
    //    Set/Add line-item validations for every line saved from now on; existing rows stay ''
    //    (legacy) until next edited, when the editor forces a code onto every line.
    //  - BidPackages gains MaterialsApplicable (bit, NOT NULL, default 0): materials matter to this
    //    scope, so the tender-invite email asks each subcontractor to state whether they will
    //    supply their own materials or price labour-only.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260715160000_AddBidPackageLineCostCodesAndMaterials")]
    public partial class AddBidPackageLineCostCodesAndMaterials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CostCode", table: "BidPackageLineItems", type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "MaterialsApplicable", table: "BidPackages", type: "bit", nullable: false, defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "MaterialsApplicable", table: "BidPackages");
            migrationBuilder.DropColumn(name: "CostCode", table: "BidPackageLineItems");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
