using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The drawing transcription as rows (2026-09-16): one table each for the figured dimensions,
    /// callouts and closed shapes the extraction reads from a revision's PDF, so work can be done
    /// over a drawing — filtered, paged, totalled, across a project — without loading the
    /// structure blob; plus DrawingExtractions.RowsWrittenAt, the stamp the rebuild uses to find
    /// revisions extracted before the rows existed. Purely additive: safe before or with the
    /// deploy. Id timestamped after every migration already on disk.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260916150000_AddDrawingDataRows")]
    public partial class AddDrawingDataRows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RowsWrittenAt", table: "DrawingExtractions", type: "datetimeoffset", nullable: true);

            migrationBuilder.CreateTable(
                name: "DrawingDimensions",
                columns: table => new
                {
                    DrawingDimensionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DrawingExtractionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DrawingRevisionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DrawingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Page = table.Column<int>(type: "int", nullable: false),
                    ValueMm = table.Column<int>(type: "int", nullable: false),
                    Axis = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    FromX = table.Column<double>(type: "float", nullable: false),
                    FromY = table.Column<double>(type: "float", nullable: false),
                    ToX = table.Column<double>(type: "float", nullable: false),
                    ToY = table.Column<double>(type: "float", nullable: false),
                    LabelX = table.Column<double>(type: "float", nullable: false),
                    LabelY = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_DrawingDimensions", x => x.DrawingDimensionId));

            migrationBuilder.CreateTable(
                name: "DrawingCallouts",
                columns: table => new
                {
                    DrawingCalloutId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DrawingExtractionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DrawingRevisionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DrawingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Page = table.Column<int>(type: "int", nullable: false),
                    X = table.Column<double>(type: "float", nullable: false),
                    Y = table.Column<double>(type: "float", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_DrawingCallouts", x => x.DrawingCalloutId));

            migrationBuilder.CreateTable(
                name: "DrawingShapes",
                columns: table => new
                {
                    DrawingShapeId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DrawingExtractionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DrawingRevisionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DrawingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Page = table.Column<int>(type: "int", nullable: false),
                    PointCount = table.Column<int>(type: "int", nullable: false),
                    IsRectangle = table.Column<bool>(type: "bit", nullable: false),
                    HasCurves = table.Column<bool>(type: "bit", nullable: false),
                    CentreX = table.Column<double>(type: "float", nullable: false),
                    CentreY = table.Column<double>(type: "float", nullable: false),
                    WidthMm = table.Column<double>(type: "float", nullable: false),
                    HeightMm = table.Column<double>(type: "float", nullable: false),
                    PerimeterMm = table.Column<double>(type: "float", nullable: false),
                    AreaSqM = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_DrawingShapes", x => x.DrawingShapeId));

            migrationBuilder.CreateIndex(name: "IX_DrawingDimensions_DrawingRevisionId", table: "DrawingDimensions", column: "DrawingRevisionId");
            migrationBuilder.CreateIndex(name: "IX_DrawingDimensions_ProjectId", table: "DrawingDimensions", column: "ProjectId");
            migrationBuilder.CreateIndex(name: "IX_DrawingCallouts_DrawingRevisionId", table: "DrawingCallouts", column: "DrawingRevisionId");
            migrationBuilder.CreateIndex(name: "IX_DrawingCallouts_ProjectId", table: "DrawingCallouts", column: "ProjectId");
            migrationBuilder.CreateIndex(name: "IX_DrawingShapes_DrawingRevisionId", table: "DrawingShapes", column: "DrawingRevisionId");
            migrationBuilder.CreateIndex(name: "IX_DrawingShapes_ProjectId", table: "DrawingShapes", column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DrawingShapes");
            migrationBuilder.DropTable(name: "DrawingCallouts");
            migrationBuilder.DropTable(name: "DrawingDimensions");
            migrationBuilder.DropColumn(name: "RowsWrittenAt", table: "DrawingExtractions");
        }
    }
}
