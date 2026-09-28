using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The site drawing links (2026-09-28): a QR poster's link onto one folder of a project's
    /// document register — the folder, whether the folders inside it are included, the poster's
    /// label, who minted it, when it expires and when it was revoked, and its scan count. Only the
    /// SHA-256 of the secret is stored, unique. Additive only: one new table and its two indexes,
    /// no data changes. Safe to apply before or with the deploy. Scoped script:
    /// api/Migrations/add-site-drawing-links.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260928115521_AddSiteDrawingLinks")]
    public partial class AddSiteDrawingLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SiteDrawingLinks",
                columns: table => new
                {
                    SiteDrawingLinkId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DrawingFolderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IncludeSubFolders = table.Column<bool>(type: "bit", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ScanCount = table.Column<int>(type: "int", nullable: false),
                    LastScannedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_SiteDrawingLinks", x => x.SiteDrawingLinkId));

            migrationBuilder.CreateIndex(name: "IX_SiteDrawingLinks_ProjectId", table: "SiteDrawingLinks", column: "ProjectId");
            migrationBuilder.CreateIndex(name: "IX_SiteDrawingLinks_TokenHash", table: "SiteDrawingLinks", column: "TokenHash", unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SiteDrawingLinks");
        }
    }
}
