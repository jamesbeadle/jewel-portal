using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The accountant's month-end unblockers (2026-08-31): Workers gain IsSoleTrader (the worker
    /// is their own settlement counterparty — no invented directory company) and an engagement
    /// window (EngagedFrom/EngagedTo — bounds what the chase list EXPECTS, never what counts);
    /// LabourChaseDismissals records reviewed chase-days dismissed with a reason, so the derived
    /// chase list and the unconfirmed-cost accrual can finally be cleared without inventing a
    /// timesheet or an absence. Additive — apply before or with the deploy.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260831220000_AddWorkerSettlementIdentityAndChaseDismissals")]
    public partial class AddWorkerSettlementIdentityAndChaseDismissals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSoleTrader",
                table: "Workers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EngagedFrom",
                table: "Workers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EngagedTo",
                table: "Workers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LabourChaseDismissals",
                columns: table => new
                {
                    LabourChaseDismissalId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkerId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    DismissedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DismissedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabourChaseDismissals", x => x.LabourChaseDismissalId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabourChaseDismissals_WorkerId_Date",
                table: "LabourChaseDismissals",
                columns: new[] { "WorkerId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "LabourChaseDismissals");
            migrationBuilder.DropColumn(name: "IsSoleTrader", table: "Workers");
            migrationBuilder.DropColumn(name: "EngagedFrom", table: "Workers");
            migrationBuilder.DropColumn(name: "EngagedTo", table: "Workers");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
