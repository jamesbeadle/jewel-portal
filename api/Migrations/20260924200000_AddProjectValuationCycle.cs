using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// How often a project is valued, from its contract terms (2026-09-24, Jeremy's By France
    /// report): Projects.ValuationCycle, 0 = not set for every existing project, so the next
    /// valuation date reads exactly as before until someone picks a cycle. Additive — apply
    /// before or with the deploy. Script: add-project-valuation-cycle.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260924200000_AddProjectValuationCycle")]
    public partial class AddProjectValuationCycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ValuationCycle",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ValuationCycle", table: "Projects");
        }
    }
}
