using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Entity refactor — Phase 3: Variation Orders + "Add VO to CVR".
    //  - New VariationOrders table: the approved change raised from a VOQ.
    //  - CostCodeBudgets gains CommittedAmount (resolves the Valuation-Report spec's open decision D5)
    //    so an approved VO's value is visible as committed-but-not-yet-spent against its cost code.
    // The approval handler also writes a ValuationLineItem (Variation) and a QsAccrual — those use
    // existing tables, so no schema change is needed for them here.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260701150000_AddVariationOrders")]
    public partial class AddVariationOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VariationOrders",
                columns: table => new
                {
                    VariationOrderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    VariationOrderQuoteId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    VariationRef = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SubcontractorId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CostCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApprovedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariationOrders", x => x.VariationOrderId);
                });

            migrationBuilder.AddColumn<decimal>(
                name: "CommittedAmount", table: "CostCodeBudgets", type: "decimal(18,4)", nullable: false, defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CommittedAmount", table: "CostCodeBudgets");
            migrationBuilder.DropTable(name: "VariationOrders");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
