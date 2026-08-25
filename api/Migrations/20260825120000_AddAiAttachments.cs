using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Files attached to an assistant conversation now keep their bytes (blob container
    // ai-attachments) so any part — the V01 tab of a forty-tab valuation — can be read on demand
    // instead of the first 25,000 characters being extracted once and the rest lost. This table
    // points a conversation at its blobs and holds each file's manifest (docs/ai/06-context-
    // retrieval.md). Additive only — a new table — so it is safe to apply ahead of the deploy.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260825120000_AddAiAttachments")]
    public partial class AddAiAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiAttachments",
                columns: table => new
                {
                    AttachmentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ConversationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    BlobRef = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ManifestJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiAttachments", x => x.AttachmentId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiAttachments_ConversationId",
                table: "AiAttachments",
                column: "ConversationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AiAttachments");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
