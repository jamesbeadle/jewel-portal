using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Adds drawing folders: one flat level of named groups on a project's drawing register
    /// (typically a discipline or package split), plus a nullable DrawingFolderId on Drawings.
    /// Null = ungrouped. No FK — deleting a folder nulls the column out in the handler so the
    /// drawings survive the folder, and both migrate independently.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260819100000_AddDrawingFolders")]
    public partial class AddDrawingFolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DrawingFolders",
                columns: table => new
                {
                    DrawingFolderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrawingFolders", x => x.DrawingFolderId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DrawingFolders_ProjectId",
                table: "DrawingFolders",
                column: "ProjectId");

            migrationBuilder.AddColumn<string>(
                name: "DrawingFolderId",
                table: "Drawings",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "DrawingFolderId", table: "Drawings");
            migrationBuilder.DropTable(name: "DrawingFolders");
        }
    }
}
