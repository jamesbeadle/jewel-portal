using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The Contractor's Report's rewrite (2026-09-28): the week's daily logs rewritten in house
    /// language with the flag list the office clears before the build, kept as one JSON column
    /// beside the report's other JSON lists, null until asked for. Additive only: one nullable
    /// column, no data changes. Safe to apply before or with the deploy. Scoped script:
    /// api/Migrations/add-contractors-report-rewrite.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260928223551_AddContractorsReportRewrite")]
    public partial class AddContractorsReportRewrite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "RewriteJson", table: "ContractorsReports", type: "nvarchar(max)", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "RewriteJson", table: "ContractorsReports");
        }
    }
}
