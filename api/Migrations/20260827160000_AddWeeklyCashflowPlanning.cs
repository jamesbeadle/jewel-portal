using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The Weekly Cashflow's two stored halves (Financial Reports — the accountant's live
    /// 13-week payment plan): manual outgoings (subcontractors, staff, subscriptions — anything
    /// Xero doesn't yet hold a bill for) and per-entry week placements ("I'm paying this in THAT
    /// week"), keyed by the stable placement-key vocabulary WeeklyCashflowMaths owns. Purely
    /// additive; the Xero-fed side of the grid is read live and never stored. No FKs — the same
    /// string-keyed arrangement as to-dos and calendar events; the handlers own the lifecycle.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260827160000_AddWeeklyCashflowPlanning")]
    public partial class AddWeeklyCashflowPlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeeklyCashflowItems",
                columns: table => new
                {
                    WeeklyCashflowItemId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Recurrence = table.Column<int>(type: "int", nullable: false),
                    FirstDueOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastDueOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_WeeklyCashflowItems", x => x.WeeklyCashflowItemId));

            migrationBuilder.CreateTable(
                name: "WeeklyCashflowPlacements",
                columns: table => new
                {
                    PlacementKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PlannedWeekStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    MovedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    MovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_WeeklyCashflowPlacements", x => x.PlacementKey));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "WeeklyCashflowItems");

            migrationBuilder.DropTable(name: "WeeklyCashflowPlacements");
        }
    }
}
