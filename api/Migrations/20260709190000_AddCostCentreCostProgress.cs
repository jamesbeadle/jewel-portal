using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Adds CostCentreCostProgress: the cost-side completion percentage per cost centre per
    /// project, edited inline on the Financials tab. Sales-side completion is not stored here —
    /// it is derived from the latest claim's cumulative claimed value on the valuation report.
    /// One row per (ProjectId, CostCode), upserted by SetCostCentreCostCompletion.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260709190000_AddCostCentreCostProgress")]
    public partial class AddCostCentreCostProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CostCentreCostProgress",
                columns: table => new
                {
                    CostCentreCostProgressId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CostCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CostCompletionPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostCentreCostProgress", x => x.CostCentreCostProgressId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CostCentreCostProgress_ProjectId_CostCode",
                table: "CostCentreCostProgress", columns: new[] { "ProjectId", "CostCode" }, unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CostCentreCostProgress");
        }
    }
}
