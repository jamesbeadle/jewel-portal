using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Two accountant's requests on the Weekly Cashflow (2026-08-28): supplier groups — sets of
    /// Xero supplier names the Supplier bills band pulls together into one line (the material
    /// suppliers: Grant &amp; Stone, HSS Hire, Skip IT) — and exclusions — "don't count this
    /// Xero entry, a direct-debit item already covers it" (the Jaguar case: a monthly DD spread
    /// as a manual item, plus a one-off bill in Xero that must not double-count). Purely
    /// additive; both are string-keyed with no FKs, same as the planning tables.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260828110000_AddWeeklyCashflowGroupsAndExclusions")]
    public partial class AddWeeklyCashflowGroupsAndExclusions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeeklyCashflowSupplierGroups",
                columns: table => new
                {
                    SupplierGroupId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactNamesJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_WeeklyCashflowSupplierGroups", x => x.SupplierGroupId));

            migrationBuilder.CreateTable(
                name: "WeeklyCashflowExclusions",
                columns: table => new
                {
                    PlacementKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExcludedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ExcludedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_WeeklyCashflowExclusions", x => x.PlacementKey));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "WeeklyCashflowSupplierGroups");

            migrationBuilder.DropTable(name: "WeeklyCashflowExclusions");
        }
    }
}
