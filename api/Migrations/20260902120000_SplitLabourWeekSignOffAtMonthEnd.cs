using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The weekly labour sign-off becomes per worker, week AND month (2026-09-02, the
    /// accountant's ask). A week that straddles a month end — Mon 31 Aug to Sun 6 Sep — held
    /// the old month's Xero run until the whole week had elapsed and every September day in it
    /// was approved; now the August part ("to 31 Aug") and the September part ("from 1 Sep") are
    /// separate markers. Additive: adds MonthStart, stamps every existing row with the month of
    /// its Monday, and gives each existing straddling-week marker a twin for the following
    /// month (the whole week HAD been signed off, so both parts stay signed), then widens the
    /// unique index. Safe to apply before the deploy — the old code never reads the new column.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260902120000_SplitLabourWeekSignOffAtMonthEnd")]
    public partial class SplitLabourWeekSignOffAtMonthEnd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LabourWeekSignOffs_WorkerId_WeekStart",
                table: "LabourWeekSignOffs");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MonthStart",
                table: "LabourWeekSignOffs",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            // Data moves ride inside sp_executesql so the batch compiles only when it runs
            // (CLAUDE.md: inline SQL that names a column a later migration drops poisons the
            // full script forever).
            migrationBuilder.Sql(@"
EXEC sp_executesql N'
UPDATE [LabourWeekSignOffs]
SET [MonthStart] = DATETIMEOFFSETFROMPARTS(YEAR([WeekStart]), MONTH([WeekStart]), 1, 0, 0, 0, 0, 0, 0, 7)
WHERE [MonthStart] = DATETIMEOFFSETFROMPARTS(1, 1, 1, 0, 0, 0, 0, 0, 0, 7);

INSERT INTO [LabourWeekSignOffs] ([LabourWeekSignOffId], [WorkerId], [WeekStart], [MonthStart], [SignedOffByEmail], [SignedOffAt])
SELECT LOWER(REPLACE(CONVERT(nvarchar(36), NEWID()), N''-'', N'''')),
       [WorkerId], [WeekStart],
       DATEADD(month, 1, [MonthStart]),
       [SignedOffByEmail], [SignedOffAt]
FROM [LabourWeekSignOffs] AS existing
WHERE MONTH(DATEADD(day, 6, [WeekStart])) <> MONTH([WeekStart])
  AND NOT EXISTS (
      SELECT 1 FROM [LabourWeekSignOffs] AS twin
      WHERE twin.[WorkerId] = existing.[WorkerId]
        AND twin.[WeekStart] = existing.[WeekStart]
        AND twin.[MonthStart] = DATEADD(month, 1, existing.[MonthStart]));
';");

            migrationBuilder.CreateIndex(
                name: "IX_LabourWeekSignOffs_WorkerId_WeekStart_MonthStart",
                table: "LabourWeekSignOffs",
                columns: new[] { "WorkerId", "WeekStart", "MonthStart" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LabourWeekSignOffs_WorkerId_WeekStart_MonthStart",
                table: "LabourWeekSignOffs");

            // Keep one marker per worker-week: the row for the Monday's own month.
            migrationBuilder.Sql(@"
EXEC sp_executesql N'
DELETE FROM [LabourWeekSignOffs]
WHERE [MonthStart] <> DATETIMEOFFSETFROMPARTS(YEAR([WeekStart]), MONTH([WeekStart]), 1, 0, 0, 0, 0, 0, 0, 7);
';");

            migrationBuilder.DropColumn(
                name: "MonthStart",
                table: "LabourWeekSignOffs");

            migrationBuilder.CreateIndex(
                name: "IX_LabourWeekSignOffs_WorkerId_WeekStart",
                table: "LabourWeekSignOffs",
                columns: new[] { "WorkerId", "WeekStart" },
                unique: true);
        }
    }
}
