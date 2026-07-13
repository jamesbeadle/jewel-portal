using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Compliance documents gain stored files (blob path/content type/size) and versioning
    // (re-uploads supersede rather than replace). Existing rows stay metadata-only (BlobPath = "")
    // at Version 1. See docs/06-backlog/subcontractor-crm-scope.md §4.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260713110000_AddComplianceDocumentFiles")]
    public partial class AddComplianceDocumentFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlobPath", table: "ComplianceDocuments", type: "nvarchar(1024)", maxLength: 1024, nullable: false, defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContentType", table: "ComplianceDocuments", type: "nvarchar(256)", maxLength: 256, nullable: false, defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "FileSize", table: "ComplianceDocuments", type: "bigint", nullable: false, defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "Version", table: "ComplianceDocuments", type: "int", nullable: false, defaultValue: 1);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SupersededAt", table: "ComplianceDocuments", type: "datetimeoffset", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "BlobPath", table: "ComplianceDocuments");
            migrationBuilder.DropColumn(name: "ContentType", table: "ComplianceDocuments");
            migrationBuilder.DropColumn(name: "FileSize", table: "ComplianceDocuments");
            migrationBuilder.DropColumn(name: "Version", table: "ComplianceDocuments");
            migrationBuilder.DropColumn(name: "SupersededAt", table: "ComplianceDocuments");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
