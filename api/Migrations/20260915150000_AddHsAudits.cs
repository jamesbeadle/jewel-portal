using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// H&S site audits (2026-09-15, Katy-Louise's inspection workbook brought into the portal):
    /// the HsAudits header table (per-project HSA-#### number, status Draft → Issued → Closed,
    /// the header fields, the spreadsheet's score) and HsAuditItems — one row per framework item
    /// per audit, with what the officer found and the corrective action Issue minted for it.
    /// HsRecords gains AssignedToName so a corrective action can be owned by a person with no
    /// portal login. Additive only; no FKs, as everywhere else. Scoped script:
    /// api/Migrations/add-hs-audits.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260915150000_AddHsAudits")]
    public partial class AddHsAudits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedToName",
                table: "HsRecords",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "HsAudits",
                columns: table => new
                {
                    HsAuditId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    InspectionDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SiteManagerName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SafetyOfficerName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SummaryOfWorkActivities = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SiteOperativeCount = table.Column<int>(type: "int", nullable: true),
                    FurtherComments = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    PreviousScore = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    TemplateVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ManagerName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_HsAudits", x => x.HsAuditId));
            migrationBuilder.CreateIndex(name: "IX_HsAudits_ProjectId", table: "HsAudits", column: "ProjectId");

            migrationBuilder.CreateTable(
                name: "HsAuditItems",
                columns: table => new
                {
                    HsAuditItemId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    HsAuditId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Section = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<int>(type: "int", nullable: true),
                    Rate = table.Column<int>(type: "int", nullable: true),
                    Class = table.Column<int>(type: "int", nullable: true),
                    Minus = table.Column<int>(type: "int", nullable: false),
                    TimeScale = table.Column<int>(type: "int", nullable: true),
                    Findings = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OwnerName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DateRectified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    HsRecordId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_HsAuditItems", x => x.HsAuditItemId));
            migrationBuilder.CreateIndex(name: "IX_HsAuditItems_HsAuditId", table: "HsAuditItems", column: "HsAuditId");
            migrationBuilder.CreateIndex(name: "IX_HsAuditItems_HsRecordId", table: "HsAuditItems", column: "HsRecordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "HsAuditItems");
            migrationBuilder.DropTable(name: "HsAudits");
            migrationBuilder.DropColumn(name: "AssignedToName", table: "HsRecords");
        }
    }
}
