using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The company-wide site photo pool (2026-09-16, James: "a big dumping ground for photos and
    /// any project"): one table of photographs dropped in before anyone has said which project or
    /// day they belong to, keyed by the SHA-256 of the file so the assistant can find a row from a
    /// laptop-side fingerprint and file it onto the right progress update. Additive only; no FKs,
    /// as everywhere else. Scoped script: api/Migrations/add-site-photos.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260916230000_AddSitePhotos")]
    public partial class AddSitePhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SitePhotos",
                columns: table => new
                {
                    SitePhotoId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    BlobRef = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UploadedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FiledToProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FiledToProgressUpdateId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FiledToProgressPhotoId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FiledByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FiledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_SitePhotos", x => x.SitePhotoId));

            migrationBuilder.CreateIndex(
                name: "IX_SitePhotos_ContentHash",
                table: "SitePhotos",
                column: "ContentHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SitePhotos_FiledToProgressUpdateId",
                table: "SitePhotos",
                column: "FiledToProgressUpdateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SitePhotos");
        }
    }
}
