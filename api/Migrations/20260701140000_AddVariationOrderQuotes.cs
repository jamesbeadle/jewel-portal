using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Entity refactor — Phase 2: Variation Order Quotes (VOQ).
    //  - New VariationOrderQuotes table: the procurement container an RFQ creates. Holds the selected
    //    bid package/subcontractor and estimated value; status drives Draft -> Inviting -> Selected.
    //  - BidPackages gains VariationOrderQuoteId (nullable) so a package can belong to a VOQ. Existing
    //    standalone packages keep NULL and are unaffected.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260701140000_AddVariationOrderQuotes")]
    public partial class AddVariationOrderQuotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VariationOrderQuotes",
                columns: table => new
                {
                    VariationOrderQuoteId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SelectedBidPackageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SelectedSubcontractorId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    EstimatedValue = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ApprovedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariationOrderQuotes", x => x.VariationOrderQuoteId);
                });

            migrationBuilder.AddColumn<string>(
                name: "VariationOrderQuoteId", table: "BidPackages", type: "nvarchar(64)", maxLength: 64, nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "VariationOrderQuoteId", table: "BidPackages");
            migrationBuilder.DropTable(name: "VariationOrderQuotes");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
