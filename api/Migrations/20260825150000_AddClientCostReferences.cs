using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // The client's schedule-of-works references (2026-08-25, the Abbot Road architect's ask):
    // a per-project map from our cost centre to the client's item number, and the frozen copy
    // of that reference on every valuation report snapshot line so the client PDF can print it
    // and a later remap never rewrites an issued statement. Purely additive; apply BEFORE the
    // deploy, because the deployed code reads the new column on every snapshot.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260825150000_AddClientCostReferences")]
    public partial class AddClientCostReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientReference",
                table: "ValuationReportSnapshotLines",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ClientCostReferences",
                columns: table => new
                {
                    ClientCostReferenceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CostCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ClientReference = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientCostReferences", x => x.ClientCostReferenceId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientCostReferences_ProjectId_CostCode",
                table: "ClientCostReferences",
                columns: new[] { "ProjectId", "CostCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ClientCostReferences");
            migrationBuilder.DropColumn(name: "ClientReference", table: "ValuationReportSnapshotLines");
        }
    }
}
