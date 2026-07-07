using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Stores Xero purchase-invoice lines in JPMS for cost allocation: each line is keyed on
    // its stable Xero invoice + line-item ids so syncs upsert without duplicating, and carries
    // allocation fields (status / project / master cost centre) that belong to JPMS and are
    // never overwritten by a sync. See XeroLedgerLineEntity.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260707150000_AddXeroLedger")]
    public partial class AddXeroLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "XeroLedgerLines",
                columns: table => new
                {
                    XeroLedgerLineId = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    XeroInvoiceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    XeroLineItemId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InvoiceStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Net = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Tax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AccountCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AccountName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    XeroSite = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    XeroCostCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AllocationStatus = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CostCenterCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AllocatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AllocatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    FirstSeenAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSyncedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XeroLedgerLines", x => x.XeroLedgerLineId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_XeroLedgerLines_AllocationStatus",
                table: "XeroLedgerLines",
                column: "AllocationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_XeroLedgerLines_ProjectId_CostCenterCode",
                table: "XeroLedgerLines",
                columns: new[] { "ProjectId", "CostCenterCode" });

            migrationBuilder.CreateIndex(
                name: "IX_XeroLedgerLines_XeroInvoiceId",
                table: "XeroLedgerLines",
                column: "XeroInvoiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "XeroLedgerLines");
        }
    }
}
