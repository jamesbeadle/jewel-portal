using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Per-line pricing on bid-package quotes (Phase 5 — tender responses). A quote keeps its
    /// package-total Value; QuoteLineItems carries the subbie's rate against each package line so
    /// submissions can be compared side by side. BidPackageLineItemId is null for lines the subbie
    /// quoted outside the package's scope.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260706100000_AddQuoteLineItems")]
    public partial class AddQuoteLineItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuoteLineItems",
                columns: table => new
                {
                    QuoteLineItemId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    QuoteId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BidPackageLineItemId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteLineItems", x => x.QuoteLineItemId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "QuoteLineItems");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // The runtime migrator applies Up()/Down() directly; it does not read this model. The
            // design-time model diff for future migrations uses JpmsContextModelSnapshot (updated to
            // include QuoteLineItems). Declared explicitly so the migration compiles regardless of
            // whether the base BuildTargetModel is virtual or abstract.
        }
    }
}
