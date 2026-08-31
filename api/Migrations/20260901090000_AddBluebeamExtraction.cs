using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Adds the Bluebeam drawing-data extraction schema: the single shared Studio connection row
    /// (tokens live in SQL because refresh tokens rotate on use and the api and worker share only
    /// the database), one extraction-status row per drawing revision, the normalised per-markup
    /// table the data view renders from, and Document Triage's archive-provenance column
    /// (DocumentControlItems.SourceDocumentControlItemId). Purely additive — apply before or with
    /// the deploy that ships the feature.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260901090000_AddBluebeamExtraction")]
    public partial class AddBluebeamExtraction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BluebeamConnections",
                columns: table => new
                {
                    BluebeamConnectionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    AccessTokenExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ConnectedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ConnectedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ConnectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RefreshTokenUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastRefreshSucceededAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastRefreshFailedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastRefreshError = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_BluebeamConnections", x => x.BluebeamConnectionId));

            migrationBuilder.CreateTable(
                name: "DrawingExtractions",
                columns: table => new
                {
                    DrawingExtractionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DrawingRevisionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DrawingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    QueuedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    QueuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    PageCount = table.Column<int>(type: "int", nullable: true),
                    PagesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MarkupCount = table.Column<int>(type: "int", nullable: true),
                    MarkupsBlobRef = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    TextBlobRef = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    BluebeamSessionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_DrawingExtractions", x => x.DrawingExtractionId));

            migrationBuilder.CreateTable(
                name: "DrawingMarkups",
                columns: table => new
                {
                    DrawingMarkupId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DrawingExtractionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DrawingRevisionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BluebeamMarkupId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false),
                    MarkupType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Author = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Colour = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtRaw = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ModifiedAtRaw = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MeasurementValue = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    MeasurementUnit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    RectJson = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    RawJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_DrawingMarkups", x => x.DrawingMarkupId));

            migrationBuilder.AddColumn<string>(
                name: "SourceDocumentControlItemId",
                table: "DocumentControlItems",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrawingExtractions_DrawingRevisionId",
                table: "DrawingExtractions", column: "DrawingRevisionId", unique: true);
            migrationBuilder.CreateIndex(
                name: "IX_DrawingExtractions_ProjectId_Status",
                table: "DrawingExtractions", columns: new[] { "ProjectId", "Status" });
            migrationBuilder.CreateIndex(
                name: "IX_DrawingMarkups_DrawingRevisionId",
                table: "DrawingMarkups", column: "DrawingRevisionId");
            migrationBuilder.CreateIndex(
                name: "IX_DrawingMarkups_DrawingExtractionId",
                table: "DrawingMarkups", column: "DrawingExtractionId");
            migrationBuilder.CreateIndex(
                name: "IX_DocumentControlItems_SourceDocumentControlItemId",
                table: "DocumentControlItems", column: "SourceDocumentControlItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DrawingMarkups");
            migrationBuilder.DropTable(name: "DrawingExtractions");
            migrationBuilder.DropTable(name: "BluebeamConnections");
            migrationBuilder.DropIndex(name: "IX_DocumentControlItems_SourceDocumentControlItemId", table: "DocumentControlItems");
            migrationBuilder.DropColumn(name: "SourceDocumentControlItemId", table: "DocumentControlItems");
        }
    }
}
