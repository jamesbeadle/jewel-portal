using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Drawing folders nest: a nullable ParentDrawingFolderId on DrawingFolders (null = top
    /// level). No FK, matching Drawings.DrawingFolderId — deleting a folder re-parents its
    /// children in the handler rather than cascading.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260825150000_AddDrawingSubFolders")]
    public partial class AddDrawingSubFolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ParentDrawingFolderId",
                table: "DrawingFolders",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrawingFolders_ParentDrawingFolderId",
                table: "DrawingFolders",
                column: "ParentDrawingFolderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_DrawingFolders_ParentDrawingFolderId", table: "DrawingFolders");
            migrationBuilder.DropColumn(name: "ParentDrawingFolderId", table: "DrawingFolders");
        }
    }
}
