using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Split allocation + Xero write-back:
    //  * XeroCostSplits — an allocated ledger line's value shared across several
    //    cost centres and/or projects, each row carrying its own project (whole-line
    //    allocations stay on XeroLedgerLines.ProjectId + CostCenterCode).
    //  * Write-back columns on XeroLedgerLines — the outcome of confirming the
    //    allocation back to Xero (Sites/Cost Code tracking + DRAFT → AUTHORISED).
    //  * Projects.XeroSiteName — the project's option in Xero's "Sites" tracking
    //    category, required before its invoices can be written back.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260709120000_AddXeroCostSplitsAndWriteBack")]
    public partial class AddXeroCostSplitsAndWriteBack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "XeroCostSplits",
                columns: table => new
                {
                    XeroCostSplitId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    XeroLedgerLineId = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CostCenterCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Net = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XeroCostSplits", x => x.XeroCostSplitId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_XeroCostSplits_XeroLedgerLineId",
                table: "XeroCostSplits",
                column: "XeroLedgerLineId");

            migrationBuilder.CreateIndex(
                name: "IX_XeroCostSplits_ProjectId_CostCenterCode",
                table: "XeroCostSplits",
                columns: new[] { "ProjectId", "CostCenterCode" });

            migrationBuilder.AddColumn<int>(
                name: "WriteBackStatus",
                table: "XeroLedgerLines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "WriteBackError",
                table: "XeroLedgerLines",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "WriteBackAtUtc",
                table: "XeroLedgerLines",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "XeroSiteName",
                table: "Projects",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "XeroCostSplits");
            migrationBuilder.DropColumn(name: "WriteBackStatus", table: "XeroLedgerLines");
            migrationBuilder.DropColumn(name: "WriteBackError", table: "XeroLedgerLines");
            migrationBuilder.DropColumn(name: "WriteBackAtUtc", table: "XeroLedgerLines");
            migrationBuilder.DropColumn(name: "XeroSiteName", table: "Projects");
        }
    }
}
