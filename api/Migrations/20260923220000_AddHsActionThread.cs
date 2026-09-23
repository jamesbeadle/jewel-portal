using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The thread on a corrective action and the digest behind it (2026-09-23, Katy-Louise's asks
    /// of 15 Sep): HsRecordComments (what the site manager and the officer say), HsRecordPhotos
    /// (the photograph of the work done, stored through the progress photo store), HsRecordEvents
    /// (the outbox the digest sweep sends from), and the project's site manager — a person, not a
    /// login — whose address the digest goes to. Additive only; no FKs, as everywhere else.
    /// Script: add-hs-action-thread.sql. Apply before or with the deploy.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260923220000_AddHsActionThread")]
    public partial class AddHsActionThread : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "SiteManagerName", table: "Projects", type: "nvarchar(256)", maxLength: 256, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "SiteManagerEmail", table: "Projects", type: "nvarchar(256)", maxLength: 256, nullable: false, defaultValue: "");

            migrationBuilder.CreateTable(
                name: "HsRecordComments",
                columns: table => new
                {
                    HsRecordCommentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    HsRecordId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AuthorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AuthorName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_HsRecordComments", x => x.HsRecordCommentId));
            migrationBuilder.CreateIndex(name: "IX_HsRecordComments_HsRecordId", table: "HsRecordComments", column: "HsRecordId");

            migrationBuilder.CreateTable(
                name: "HsRecordPhotos",
                columns: table => new
                {
                    HsRecordPhotoId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    HsRecordId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    HsRecordCommentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    BlobRef = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_HsRecordPhotos", x => x.HsRecordPhotoId));
            migrationBuilder.CreateIndex(name: "IX_HsRecordPhotos_HsRecordId", table: "HsRecordPhotos", column: "HsRecordId");

            migrationBuilder.CreateTable(
                name: "HsRecordEvents",
                columns: table => new
                {
                    HsRecordEventId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    HsRecordId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    ByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ByName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    NotifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_HsRecordEvents", x => x.HsRecordEventId));
            migrationBuilder.CreateIndex(name: "IX_HsRecordEvents_NotifiedAt", table: "HsRecordEvents", column: "NotifiedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "HsRecordEvents");
            migrationBuilder.DropTable(name: "HsRecordPhotos");
            migrationBuilder.DropTable(name: "HsRecordComments");
            migrationBuilder.DropColumn(name: "SiteManagerEmail", table: "Projects");
            migrationBuilder.DropColumn(name: "SiteManagerName", table: "Projects");
        }
    }
}
