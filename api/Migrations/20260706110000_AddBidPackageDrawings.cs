using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Links project drawings to bid packages (the tender documents). The drawing and its files
    /// stay in the Drawings section; this junction is what the invite email attaches from.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260706110000_AddBidPackageDrawings")]
    public partial class AddBidPackageDrawings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BidPackageDrawings",
                columns: table => new
                {
                    BidPackageDrawingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BidPackageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DrawingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LinkedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BidPackageDrawings", x => x.BidPackageDrawingId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BidPackageDrawings");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // The runtime migrator applies Up()/Down() directly; the design-time model diff uses
            // JpmsContextModelSnapshot (updated to include BidPackageDrawings).
        }
    }
}
