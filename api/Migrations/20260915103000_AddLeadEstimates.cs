using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Estimates on a lead (2026-09-15, the Sales pane): the LeadEstimates table — Jewel's own
    /// pricing of one enquiry (scope, architect, price due date, budget mentioned, total) with
    /// its status ladder Received → Pricing → Submitted → Won / Lost, a global EST-#### number
    /// and the timestamps the moves stamp. Read per lead (IX_LeadEstimates_LeadId); Number
    /// resolves references (IX_LeadEstimates_Number). Additive only; no FKs, as everywhere
    /// else. Scoped script: api/Migrations/add-lead-estimates.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260915103000_AddLeadEstimates")]
    public partial class AddLeadEstimates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeadEstimates",
                columns: table => new
                {
                    EstimateId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LeadId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ArchitectName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PriceDueOn = table.Column<DateOnly>(type: "date", nullable: true),
                    BudgetMentioned = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Total = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StatusChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_LeadEstimates", x => x.EstimateId));
            migrationBuilder.CreateIndex(name: "IX_LeadEstimates_LeadId", table: "LeadEstimates", column: "LeadId");
            migrationBuilder.CreateIndex(name: "IX_LeadEstimates_Number", table: "LeadEstimates", column: "Number");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "LeadEstimates");
        }
    }
}
