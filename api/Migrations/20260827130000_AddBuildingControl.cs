using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Adds building control — the statutory sign-off trail: the project's case with a building
    /// control body (BC-#### tag stem), its inspection stages (BCI-#### tag stems, the Building
    /// Control tab's register), and the files on either (photos, site inspection reports,
    /// notices, the completion certificate; bytes live in the private building-control blob
    /// container, BlobRef points at them). No FKs — the same string-keyed arrangement as
    /// defects/calendar events; the handlers own the cascades.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260827130000_AddBuildingControl")]
    public partial class AddBuildingControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BuildingControlCases",
                columns: table => new
                {
                    BuildingControlCaseId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Regime = table.Column<int>(type: "int", nullable: false),
                    BodyName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    BodyReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    NoticeSubmittedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AcceptedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletionCertifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_BuildingControlCases", x => x.BuildingControlCaseId));

            migrationBuilder.CreateTable(
                name: "BuildingControlInspections",
                columns: table => new
                {
                    BuildingControlInspectionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BuildingControlCaseId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    StageName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BookedFor = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    InspectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OutcomeNotes = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    InspectorName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    RaisedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RaisedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_BuildingControlInspections", x => x.BuildingControlInspectionId));

            migrationBuilder.CreateTable(
                name: "BuildingControlAttachments",
                columns: table => new
                {
                    BuildingControlAttachmentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BuildingControlCaseId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    BuildingControlInspectionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    BlobRef = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AddedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_BuildingControlAttachments", x => x.BuildingControlAttachmentId));

            migrationBuilder.CreateIndex(name: "IX_BuildingControlCases_ProjectId", table: "BuildingControlCases", column: "ProjectId");
            migrationBuilder.CreateIndex(name: "IX_BuildingControlCases_Number", table: "BuildingControlCases", column: "Number");
            migrationBuilder.CreateIndex(name: "IX_BuildingControlInspections_ProjectId", table: "BuildingControlInspections", column: "ProjectId");
            migrationBuilder.CreateIndex(name: "IX_BuildingControlInspections_BuildingControlCaseId", table: "BuildingControlInspections", column: "BuildingControlCaseId");
            migrationBuilder.CreateIndex(name: "IX_BuildingControlInspections_Number", table: "BuildingControlInspections", column: "Number");
            migrationBuilder.CreateIndex(name: "IX_BuildingControlAttachments_ProjectId", table: "BuildingControlAttachments", column: "ProjectId");
            migrationBuilder.CreateIndex(name: "IX_BuildingControlAttachments_BuildingControlCaseId", table: "BuildingControlAttachments", column: "BuildingControlCaseId");
            migrationBuilder.CreateIndex(name: "IX_BuildingControlAttachments_BuildingControlInspectionId", table: "BuildingControlAttachments", column: "BuildingControlInspectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BuildingControlAttachments");
            migrationBuilder.DropTable(name: "BuildingControlInspections");
            migrationBuilder.DropTable(name: "BuildingControlCases");
        }
    }
}
