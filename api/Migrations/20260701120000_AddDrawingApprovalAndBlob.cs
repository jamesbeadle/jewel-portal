using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Adds the drawing approval workflow + stored-file metadata.
    //  - DrawingRevisions gains ApprovalStatus (0=Unapproved,1=Approved,2=Archived) plus the blob
    //    reference + file metadata and approver stamps.
    //  - Drawings.CurrentRevision is renamed to CurrentApprovedRevisionLabel and made nullable
    //    (null until something is approved).
    //  - Backfill: each drawing's currently non-superseded revision becomes Approved; superseded
    //    revisions become Archived. Existing data therefore keeps a single sensible latest-approved.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260701120000_AddDrawingApprovalAndBlob")]
    public partial class AddDrawingApprovalAndBlob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- Drawings: repurpose CurrentRevision -> CurrentApprovedRevisionLabel (nullable) ---
            migrationBuilder.RenameColumn(
                name: "CurrentRevision", table: "Drawings", newName: "CurrentApprovedRevisionLabel");

            migrationBuilder.AlterColumn<string>(
                name: "CurrentApprovedRevisionLabel", table: "Drawings", type: "nvarchar(16)", maxLength: 16,
                nullable: true, oldClrType: typeof(string), oldType: "nvarchar(16)", oldMaxLength: 16, oldNullable: false);

            // --- DrawingRevisions: approval workflow + stored file ---
            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus", table: "DrawingRevisions", type: "int", nullable: false, defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BlobRef", table: "DrawingRevisions", type: "nvarchar(1024)", maxLength: 1024, nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentType", table: "DrawingRevisions", type: "nvarchar(128)", maxLength: 128, nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes", table: "DrawingRevisions", type: "bigint", nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByEmail", table: "DrawingRevisions", type: "nvarchar(256)", maxLength: 256, nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedAt", table: "DrawingRevisions", type: "datetimeoffset", nullable: true);

            // --- Backfill existing rows to a coherent approval state ---
            // Non-superseded == the current version under the old model (issue superseded all priors),
            // so at most one per drawing -> exactly one Approved per drawing.
            migrationBuilder.Sql(@"
UPDATE [DrawingRevisions]
SET [ApprovalStatus] = 1,
    [ApprovedAt] = [ReceivedAt],
    [ApprovedByEmail] = [IssuedByEmail]
WHERE [SupersededAt] IS NULL;");

            migrationBuilder.Sql(@"
UPDATE [DrawingRevisions]
SET [ApprovalStatus] = 2
WHERE [SupersededAt] IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ApprovalStatus", table: "DrawingRevisions");
            migrationBuilder.DropColumn(name: "BlobRef", table: "DrawingRevisions");
            migrationBuilder.DropColumn(name: "ContentType", table: "DrawingRevisions");
            migrationBuilder.DropColumn(name: "FileSizeBytes", table: "DrawingRevisions");
            migrationBuilder.DropColumn(name: "ApprovedByEmail", table: "DrawingRevisions");
            migrationBuilder.DropColumn(name: "ApprovedAt", table: "DrawingRevisions");

            migrationBuilder.Sql(
                "UPDATE [Drawings] SET [CurrentApprovedRevisionLabel] = '' WHERE [CurrentApprovedRevisionLabel] IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "CurrentApprovedRevisionLabel", table: "Drawings", type: "nvarchar(16)", maxLength: 16,
                nullable: false, defaultValue: "", oldClrType: typeof(string), oldType: "nvarchar(16)",
                oldMaxLength: 16, oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "CurrentApprovedRevisionLabel", table: "Drawings", newName: "CurrentRevision");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
