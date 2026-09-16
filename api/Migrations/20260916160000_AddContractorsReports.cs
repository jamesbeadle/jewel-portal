using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The weekly Contractor's Report (the FD's spec of 2026-09-15, change 4): one table holding
    /// what a person enters per report — the header fields, Look Ahead, Neighbours, H&amp;S, the
    /// Building Control liaison line, the subcontractors' attendance and the chosen updates.
    /// Every section read from the register is composed at build time and never stored. One
    /// report per project per period (unique on ProjectId + PeriodEnd). Additive only; no FKs,
    /// as everywhere else. Scoped script: api/Migrations/add-contractors-reports.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260916160000_AddContractorsReports")]
    public partial class AddContractorsReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContractorsReports",
                columns: table => new
                {
                    ContractorsReportId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    ValuationNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ProgrammeReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PreparedByName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IssuedTo = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DateOfIssue = table.Column<DateOnly>(type: "date", nullable: false),
                    LookAheadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Neighbours = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    HealthAndSafety = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    BuildingControlLiaison = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AttendanceJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SelectedUpdateIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_ContractorsReports", x => x.ContractorsReportId));

            migrationBuilder.CreateIndex(
                name: "IX_ContractorsReports_ProjectId_PeriodEnd",
                table: "ContractorsReports",
                columns: new[] { "ProjectId", "PeriodEnd" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ContractorsReports");
        }
    }
}
