using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Adds cost centre groups: named roll-ups that combine several cost centres into one
    /// line on the project's Financials tab (e.g. aluminium windows + specialist glazing).
    /// Presentation only — all figures remain stored per cost centre. The unique
    /// (ProjectId, CostCode) index on members keeps a centre in at most one group per project.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260709200000_AddCostCentreGroups")]
    public partial class AddCostCentreGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CostCentreGroups",
                columns: table => new
                {
                    CostCentreGroupId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostCentreGroups", x => x.CostCentreGroupId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CostCentreGroups_ProjectId",
                table: "CostCentreGroups", column: "ProjectId");

            migrationBuilder.CreateTable(
                name: "CostCentreGroupMembers",
                columns: table => new
                {
                    CostCentreGroupMemberId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CostCentreGroupId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CostCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostCentreGroupMembers", x => x.CostCentreGroupMemberId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CostCentreGroupMembers_CostCentreGroupId",
                table: "CostCentreGroupMembers", column: "CostCentreGroupId");

            migrationBuilder.CreateIndex(
                name: "UX_CostCentreGroupMembers_Project_CostCode",
                table: "CostCentreGroupMembers", columns: new[] { "ProjectId", "CostCode" }, unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CostCentreGroupMembers");
            migrationBuilder.DropTable(name: "CostCentreGroups");
        }
    }
}
