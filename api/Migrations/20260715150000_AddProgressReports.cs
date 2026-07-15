using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Progress reporting: site managers record progress updates (a group of photos with a
    // description); client-facing progress reports select updates and add narrative sections.
    // The PDF is rendered on download, never stored.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260715150000_AddProgressReports")]
    public partial class AddProgressReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProgressUpdates",
                columns: table => new
                {
                    ProgressUpdateId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: false),
                    WorkDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressUpdates", x => x.ProgressUpdateId);
                });

            migrationBuilder.CreateTable(
                name: "ProgressPhotos",
                columns: table => new
                {
                    ProgressPhotoId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProgressUpdateId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    BlobRef = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UploadedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressPhotos", x => x.ProgressPhotoId);
                });

            migrationBuilder.CreateTable(
                name: "ProgressReports",
                columns: table => new
                {
                    ProgressReportId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Introduction = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: false),
                    WorkCompleted = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: false),
                    UpcomingWorks = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressReports", x => x.ProgressReportId);
                });

            migrationBuilder.CreateTable(
                name: "ProgressReportSelections",
                columns: table => new
                {
                    ProgressReportSelectionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProgressReportId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProgressUpdateId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressReportSelections", x => x.ProgressReportSelectionId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProgressUpdates_ProjectId",
                table: "ProgressUpdates",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressPhotos_ProgressUpdateId",
                table: "ProgressPhotos",
                column: "ProgressUpdateId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressPhotos_ProjectId",
                table: "ProgressPhotos",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressReports_ProjectId",
                table: "ProgressReports",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressReportSelections_ProgressReportId",
                table: "ProgressReportSelections",
                column: "ProgressReportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ProgressReportSelections");
            migrationBuilder.DropTable(name: "ProgressReports");
            migrationBuilder.DropTable(name: "ProgressPhotos");
            migrationBuilder.DropTable(name: "ProgressUpdates");
        }
    }
}
