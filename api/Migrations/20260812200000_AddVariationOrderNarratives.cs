using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Variation orders gain the narrative sections of their official document — the commercial
    // basis of the price, the programme impact and the exclusions — so the VO can be issued as a
    // complete client-facing PDF (rendered on demand, attached from the composer's system-documents
    // picker) rather than a bare line-item build-up. Free text, all optional, 4000 characters each:
    // the same allowance as the request document's narrative fields. Additive only — safe to apply
    // ahead of the deploy.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260812200000_AddVariationOrderNarratives")]
    public partial class AddVariationOrderNarratives : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommercialBasis", table: "VariationOrderQuotes",
                type: "nvarchar(4000)", maxLength: 4000, nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProgrammeImpact", table: "VariationOrderQuotes",
                type: "nvarchar(4000)", maxLength: 4000, nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Exclusions", table: "VariationOrderQuotes",
                type: "nvarchar(4000)", maxLength: 4000, nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CommercialBasis", table: "VariationOrderQuotes");
            migrationBuilder.DropColumn(name: "ProgrammeImpact", table: "VariationOrderQuotes");
            migrationBuilder.DropColumn(name: "Exclusions", table: "VariationOrderQuotes");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
