using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// A day filed or changed by the worker after its date (2026-09-30, Jeremy on Jack's phone: a
    /// missed day filled in from My day, a day amended the day after) is marked late for the office.
    /// One bit column on Timesheets, false for every day already there. Script: add-timesheet-filed-late.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260930120000_AddTimesheetFiledLate")]
    public partial class AddTimesheetFiledLate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFiledLate",
                table: "Timesheets",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFiledLate",
                table: "Timesheets");
        }
    }
}
