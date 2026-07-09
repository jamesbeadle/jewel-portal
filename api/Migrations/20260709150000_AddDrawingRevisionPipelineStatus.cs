using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Adds drawing-pipeline status stamps to drawing revisions: MetadataExtractedAt (Bluebeam has
    /// extracted the drawing's metadata/structural file data into the portal) and AnalysedAt (the
    /// change analysis has run against the prior revision). Null means the stage hasn't happened —
    /// the Drawings UI surfaces both states so un-analysed drawings can't be silently missed.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260709150000_AddDrawingRevisionPipelineStatus")]
    public partial class AddDrawingRevisionPipelineStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MetadataExtractedAt",
                table: "DrawingRevisions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AnalysedAt",
                table: "DrawingRevisions",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "MetadataExtractedAt", table: "DrawingRevisions");
            migrationBuilder.DropColumn(name: "AnalysedAt", table: "DrawingRevisions");
        }
    }
}
