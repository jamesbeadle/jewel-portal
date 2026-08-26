using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Line-level client references (2026-08-26, Abbot Road): the client's schedule-of-works item
    // number on each valuation line itself ("1.03"), so the report matches the contract document
    // line by line. The per-cost-centre ClientCostReferences map (2026-08-25) stays as the
    // fallback for lines without one. Purely additive; apply BEFORE the deploy, because the
    // deployed code reads the new column on every line fetch and snapshot capture.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260826100000_AddValuationLineItemClientReference")]
    public partial class AddValuationLineItemClientReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientReference",
                table: "ValuationLineItems",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ClientReference", table: "ValuationLineItems");
        }
    }
}
