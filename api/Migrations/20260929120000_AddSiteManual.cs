using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The site manual as controlled modules (2026-09-29, Nigel's Site Manuals and Operating Systems
    /// request): ManualModules holds each module's working text and controls beside the text the site
    /// sees; ManualModuleVersions keeps every approved version whole; ManualAcknowledgements records
    /// who read which version. Additive — three new tables. Script: add-site-manual.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260929120000_AddSiteManual")]
    public partial class AddSiteManual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManualModules",
                columns: table => new
                {
                    ManualModuleId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublishedBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ApproverEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    PublishedVersion = table.Column<int>(type: "int", nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ApprovedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NextReviewAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsForSiteManagers = table.Column<bool>(type: "bit", nullable: false),
                    IsForHealthAndSafetyOfficer = table.Column<bool>(type: "bit", nullable: false),
                    IsForForemen = table.Column<bool>(type: "bit", nullable: false),
                    LinkedFormSlugs = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    LinkedStandards = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ChangeSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SourceSections = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_ManualModules", x => x.ManualModuleId));

            migrationBuilder.CreateTable(
                name: "ManualModuleVersions",
                columns: table => new
                {
                    ManualModuleVersionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ManualModuleId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangeSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ApprovedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SupersededAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_ManualModuleVersions", x => x.ManualModuleVersionId));

            migrationBuilder.CreateTable(
                name: "ManualAcknowledgements",
                columns: table => new
                {
                    ManualAcknowledgementId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ManualModuleId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TypedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AcknowledgedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_ManualAcknowledgements", x => x.ManualAcknowledgementId));

            migrationBuilder.CreateIndex(name: "IX_ManualModules_Code", table: "ManualModules", column: "Code", unique: true);
            migrationBuilder.CreateIndex(name: "IX_ManualModuleVersions_ManualModuleId_Version", table: "ManualModuleVersions",
                columns: new[] { "ManualModuleId", "Version" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_ManualAcknowledgements_ManualModuleId_Version_Email", table: "ManualAcknowledgements",
                columns: new[] { "ManualModuleId", "Version", "Email" }, unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ManualAcknowledgements");
            migrationBuilder.DropTable(name: "ManualModuleVersions");
            migrationBuilder.DropTable(name: "ManualModules");
        }
    }
}
