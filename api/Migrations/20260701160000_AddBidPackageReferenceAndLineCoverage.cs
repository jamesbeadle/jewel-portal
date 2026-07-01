using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Entity refactor — Phase 2: Bid Package references + line-item commercial coverage.
    //  - BidPackages gains a sequential Number (rendered BPI-0001). Existing rows are backfilled in
    //    CreatedAt order so their BPI reference is stable; the reference is the tag stem an inbox
    //    email is tagged with ("JPMS/BPI-0001") so RFT responses group under the package in triage.
    //  - BidPackageLineItems gains one-of coverage: Coverage (0 Unassigned / 1 ContractLine /
    //    2 Variation) plus BoqLineItemId and VariationOrderQuoteId (both nullable; exactly one set to
    //    match Coverage, enforced by the handler). Links each tendered line either to a contract BoQ
    //    line (flows into the Programme Valuation Report) or to a Variation Order Quote.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260701160000_AddBidPackageReferenceAndLineCoverage")]
    public partial class AddBidPackageReferenceAndLineCoverage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Number", table: "BidPackages", type: "int", nullable: false, defaultValue: 0);

            // Backfill sequential numbers for existing packages so their BPI-0001 reference is stable
            // and ordered by when they were created. New rows take MAX(Number)+1 at insert.
            migrationBuilder.Sql(@"
WITH numbered AS (
    SELECT BidPackageId,
           ROW_NUMBER() OVER (ORDER BY CreatedAt, BidPackageId) AS rn
    FROM BidPackages
)
UPDATE bp
SET bp.Number = n.rn
FROM BidPackages bp
INNER JOIN numbered n ON n.BidPackageId = bp.BidPackageId;");

            migrationBuilder.AddColumn<int>(
                name: "Coverage", table: "BidPackageLineItems", type: "int", nullable: false, defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BoqLineItemId", table: "BidPackageLineItems", type: "nvarchar(64)", maxLength: 64, nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariationOrderQuoteId", table: "BidPackageLineItems", type: "nvarchar(64)", maxLength: 64, nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "VariationOrderQuoteId", table: "BidPackageLineItems");
            migrationBuilder.DropColumn(name: "BoqLineItemId", table: "BidPackageLineItems");
            migrationBuilder.DropColumn(name: "Coverage", table: "BidPackageLineItems");
            migrationBuilder.DropColumn(name: "Number", table: "BidPackages");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
