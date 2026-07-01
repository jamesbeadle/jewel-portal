using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Entity refactor — Phase 4: monthly Cash Calls.
    //  - New CashCalls table: the monthly demand for funds (Requested -> Invoiced -> Received),
    //    optionally drawn from a valuation claim.
    //  - Projects gains CashCallTotal (running total of amounts received), incremented when a cash
    //    call is marked Received. Additive and defaulted, so existing rows are unaffected.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260701170000_AddCashCalls")]
    public partial class AddCashCalls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashCalls",
                columns: table => new
                {
                    CashCallId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ValuationClaimId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PeriodMonth = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AmountRequested = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AmountReceived = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    InvoicedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashCalls", x => x.CashCallId);
                });

            migrationBuilder.AddColumn<decimal>(
                name: "CashCallTotal", table: "Projects", type: "decimal(18,4)", nullable: false, defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CashCallTotal", table: "Projects");
            migrationBuilder.DropTable(name: "CashCalls");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
