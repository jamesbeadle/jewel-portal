using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Grows work orders from the header-only record the bid-package award created into the full
    // purchase-order shape (number, title, status, dates, source reference) and adds WorkOrderLines,
    // the priced lines each carrying their own cost centre code — the basis of the Work Orders tab
    // and of seeding the existing Buildertrend POs. BidPackageId becomes optional so orders can be
    // raised directly or seeded without a tender behind them; Scope widens to hold the full
    // scope-of-work text the printed orders carry.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260708130000_ExtendWorkOrdersForCostCenters")]
    public partial class ExtendWorkOrdersForCostCenters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "BidPackageId",
                table: "WorkOrders",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Scope",
                table: "WorkOrders",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)",
                oldMaxLength: 1024);

            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "WorkOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "WorkOrders",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "WorkOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "WorkOrders",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScheduledCompletion",
                table: "WorkOrders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceReference",
                table: "WorkOrders",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            // Existing rows all came from awarding a bid package: they are Released orders whose
            // creation date is the award date. Number them in award order so references stay stable.
            migrationBuilder.Sql("UPDATE WorkOrders SET CreatedAt = AwardedAt, Status = 1;");
            migrationBuilder.Sql(@"
WITH Numbered AS (
    SELECT WorkOrderId, ROW_NUMBER() OVER (ORDER BY AwardedAt, WorkOrderId) AS Rn
    FROM WorkOrders
)
UPDATE w SET w.Number = n.Rn
FROM WorkOrders w
JOIN Numbered n ON n.WorkOrderId = w.WorkOrderId;");

            migrationBuilder.CreateTable(
                name: "WorkOrderLines",
                columns: table => new
                {
                    WorkOrderLineId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkOrderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    CostType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CostCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LegacyCostCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PaidToDate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderLines", x => x.WorkOrderLineId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkOrderLines");

            migrationBuilder.DropColumn(name: "Number", table: "WorkOrders");
            migrationBuilder.DropColumn(name: "Title", table: "WorkOrders");
            migrationBuilder.DropColumn(name: "Status", table: "WorkOrders");
            migrationBuilder.DropColumn(name: "CreatedAt", table: "WorkOrders");
            migrationBuilder.DropColumn(name: "ScheduledCompletion", table: "WorkOrders");
            migrationBuilder.DropColumn(name: "SourceReference", table: "WorkOrders");

            migrationBuilder.AlterColumn<string>(
                name: "Scope",
                table: "WorkOrders",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<string>(
                name: "BidPackageId",
                table: "WorkOrders",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);
        }
    }
}
