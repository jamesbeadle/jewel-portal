using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Adds bucket allocation to the Xero ledger: cost-of-sales lines with no identifiable
    // project can be put to a named bucket (Parking, Fuel, Software subscriptions, Other)
    // instead of a project — visible with per-bucket totals for later internal review.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260707200000_AddXeroLedgerBucket")]
    public partial class AddXeroLedgerBucket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bucket",
                table: "XeroLedgerLines",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Bucket", table: "XeroLedgerLines");
        }
    }
}
