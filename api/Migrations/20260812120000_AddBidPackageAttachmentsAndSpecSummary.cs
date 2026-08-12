using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Tender documents on a bid package:
    //  - BidPackageAttachments — the supplier-facing attachment register (specification extracts,
    //    schedules of finishes, survey photos). Bytes live in the bid-package-attachments blob
    //    container; these files travel with the invite email alongside the linked drawings.
    //  - BidPackages.SpecificationSummary — the "what this package covers" bullets printed at the
    //    top of the pricing schedule workbook the invite carries. Optional, additive.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260812120000_AddBidPackageAttachmentsAndSpecSummary")]
    public partial class AddBidPackageAttachmentsAndSpecSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SpecificationSummary", table: "BidPackages", type: "nvarchar(4000)",
                maxLength: 4000, nullable: false, defaultValue: "");

            migrationBuilder.CreateTable(
                name: "BidPackageAttachments",
                columns: table => new
                {
                    BidPackageAttachmentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BidPackageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    BlobRef = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AddedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BidPackageAttachments", x => x.BidPackageAttachmentId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BidPackageAttachments_BidPackageId",
                table: "BidPackageAttachments",
                column: "BidPackageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BidPackageAttachments");
            migrationBuilder.DropColumn(name: "SpecificationSummary", table: "BidPackages");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
