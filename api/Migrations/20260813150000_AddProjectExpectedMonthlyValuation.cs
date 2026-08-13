using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Projects gain ExpectedMonthlyValuation (nullable): the FD's forecast assumption of how
    // much the architect is expected to certify per valuation month (2026-08-13). Null means
    // the Cash Forecast keeps its even spread; set, it claims at this rate until left-to-claim
    // runs out. Edited inline on the Cash Forecast page; forecasting only.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260813150000_AddProjectExpectedMonthlyValuation")]
    public partial class AddProjectExpectedMonthlyValuation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedMonthlyValuation",
                table: "Projects",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedMonthlyValuation",
                table: "Projects");
        }
    }
}
