using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Bid packages can end without a winner: BidPackageStatus gains Closed (5), and ClosedAt
    // records when the tender process was ended. Nullable and additive — null on every open
    // package, cleared again on reopen. No reason column by design: closing without incident
    // needs no paperwork (decision 2026-08-12).
    [DbContext(typeof(JpmsContext))]
    [Migration("20260812090000_AddBidPackageClosedAt")]
    public partial class AddBidPackageClosedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ClosedAt", table: "BidPackages", type: "datetimeoffset", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ClosedAt", table: "BidPackages");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
