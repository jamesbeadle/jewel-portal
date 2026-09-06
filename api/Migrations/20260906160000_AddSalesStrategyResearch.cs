using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The strategy brief and AI research (2026-09-06, same day as AddSalesStrategies): the idea in
    /// the team's own words, and the state + findings of the worker-run research that fills in the
    /// rest. Additive columns on SalesStrategies only. Id timestamped after every migration on disk.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260906160000_AddSalesStrategyResearch")]
    public partial class AddSalesStrategyResearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "Brief", table: "SalesStrategies", type: "nvarchar(4000)", maxLength: 4000, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<int>(name: "ResearchStatus", table: "SalesStrategies", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<DateTimeOffset>(name: "ResearchRequestedAt", table: "SalesStrategies", type: "datetimeoffset", nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(name: "ResearchCompletedAt", table: "SalesStrategies", type: "datetimeoffset", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ResearchError", table: "SalesStrategies", type: "nvarchar(2000)", maxLength: 2000, nullable: true);
            migrationBuilder.AddColumn<string>(name: "ResearchFindings", table: "SalesStrategies", type: "nvarchar(max)", nullable: false, defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Brief", table: "SalesStrategies");
            migrationBuilder.DropColumn(name: "ResearchStatus", table: "SalesStrategies");
            migrationBuilder.DropColumn(name: "ResearchRequestedAt", table: "SalesStrategies");
            migrationBuilder.DropColumn(name: "ResearchCompletedAt", table: "SalesStrategies");
            migrationBuilder.DropColumn(name: "ResearchError", table: "SalesStrategies");
            migrationBuilder.DropColumn(name: "ResearchFindings", table: "SalesStrategies");
        }
    }
}
