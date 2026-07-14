using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Projects gain NextExpectedValuationDate (nullable): when the next valuation is expected,
    // set manually from the project view's date-maths editor. Informational only.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260714090000_AddProjectNextValuationDate")]
    public partial class AddProjectNextValuationDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextExpectedValuationDate",
                table: "Projects",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextExpectedValuationDate",
                table: "Projects");
        }
    }
}
