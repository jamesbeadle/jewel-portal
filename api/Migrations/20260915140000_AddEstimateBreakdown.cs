using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The estimate's priced breakdown and client-facing narrative (2026-09-15, Nigel: the
    /// estimate document in the tender's shape). LeadEstimateLines — one row per priced line,
    /// carrying the section it prints under (name, order, provisional flag), a cost code from the
    /// master, quantity × unit price = total; replaced whole by SetEstimateBreakdown. On
    /// LeadEstimates the executive summary, build time and exclusions the document prints.
    /// Additive only; no FKs, as everywhere else. Scoped script:
    /// api/Migrations/add-estimate-breakdown.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260915140000_AddEstimateBreakdown")]
    public partial class AddEstimateBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "ExecutiveSummary", table: "LeadEstimates", type: "nvarchar(max)", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "BuildTime", table: "LeadEstimates", type: "nvarchar(1024)", maxLength: 1024, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "Exclusions", table: "LeadEstimates", type: "nvarchar(4000)", maxLength: 4000, nullable: false, defaultValue: "");

            migrationBuilder.CreateTable(
                name: "LeadEstimateLines",
                columns: table => new
                {
                    LineId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EstimateId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Section = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SectionOrder = table.Column<int>(type: "int", nullable: false),
                    SectionProvisional = table.Column<bool>(type: "bit", nullable: false),
                    CostCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_LeadEstimateLines", x => x.LineId));
            migrationBuilder.CreateIndex(name: "IX_LeadEstimateLines_EstimateId", table: "LeadEstimateLines", column: "EstimateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "LeadEstimateLines");
            migrationBuilder.DropColumn(name: "ExecutiveSummary", table: "LeadEstimates");
            migrationBuilder.DropColumn(name: "BuildTime", table: "LeadEstimates");
            migrationBuilder.DropColumn(name: "Exclusions", table: "LeadEstimates");
        }
    }
}
