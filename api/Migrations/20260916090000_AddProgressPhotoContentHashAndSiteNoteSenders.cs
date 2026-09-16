using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The FD's weekly-report spec, changes 1–3 (2026-09-16). Two additive columns:
    /// ProgressPhotos.ContentHash — the SHA-256 of a photo as received, what "the same image
    /// posted twice" is judged by (empty on photos stored before this, so they are never
    /// deduplicated against); Projects.SiteNoteSenderNames — the people whose WhatsApp
    /// messages are the project's site notes, one per line, NULL until someone lists them.
    /// (ProgressUpdates.Description was already nvarchar(max) — a MaxLength over 4000 maps to
    /// it — so dropping its 4096 cap in the model changes nothing in SQL.) Safe to apply before
    /// or with the deploy. Scoped script:
    /// api/Migrations/add-progress-photo-content-hash-and-site-note-senders.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260916090000_AddProgressPhotoContentHashAndSiteNoteSenders")]
    public partial class AddProgressPhotoContentHashAndSiteNoteSenders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "ProgressPhotos",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SiteNoteSenderNames",
                table: "Projects",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ContentHash", table: "ProgressPhotos");

            migrationBuilder.DropColumn(name: "SiteNoteSenderNames", table: "Projects");
        }
    }
}
