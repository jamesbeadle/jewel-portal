using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The site photo pool's archive (2026-09-24, Nigel, for Jeremy's weekly report): a photograph
    /// the report run judges not to be progress — a screenshot, a drawing, a marked-up photo, a
    /// snag — is set aside with its reason and the project's week it was dumped in, and kept.
    /// Additive only. Script: add-site-photo-archive.sql. Apply before or with the deploy.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260924100000_AddSitePhotoArchive")]
    public partial class AddSitePhotoArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(name: "ArchivedAt", table: "SitePhotos", type: "datetimeoffset", nullable: true);
            migrationBuilder.AddColumn<int>(name: "ArchiveReason", table: "SitePhotos", type: "int", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ArchiveNote", table: "SitePhotos", type: "nvarchar(1024)", maxLength: 1024, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "ArchivedForProjectId", table: "SitePhotos", type: "nvarchar(64)", maxLength: 64, nullable: true);
            migrationBuilder.AddColumn<DateOnly>(name: "ArchivedForPeriodEnd", table: "SitePhotos", type: "date", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ArchivedByEmail", table: "SitePhotos", type: "nvarchar(256)", maxLength: 256, nullable: false, defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ArchivedAt", table: "SitePhotos");
            migrationBuilder.DropColumn(name: "ArchiveReason", table: "SitePhotos");
            migrationBuilder.DropColumn(name: "ArchiveNote", table: "SitePhotos");
            migrationBuilder.DropColumn(name: "ArchivedForProjectId", table: "SitePhotos");
            migrationBuilder.DropColumn(name: "ArchivedForPeriodEnd", table: "SitePhotos");
            migrationBuilder.DropColumn(name: "ArchivedByEmail", table: "SitePhotos");
        }
    }
}
