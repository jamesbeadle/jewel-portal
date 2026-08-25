using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // A Claude call now runs on a background task with its own budget, and the request that
    // started it — or a later collect — reads the answer from this table instead of waiting
    // inside the Static Web Apps gateway's ~45s limit (docs/ai/07-reply-collection.md). Additive
    // only — a new table — so it is safe to apply ahead of the deploy.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260825160000_AddAiPendingReplies")]
    public partial class AddAiPendingReplies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiPendingReplies",
                columns: table => new
                {
                    ReplyId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ConversationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AfterSequence = table.Column<int>(type: "int", nullable: false),
                    ModelTier = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReplyJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AnsweredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiPendingReplies", x => x.ReplyId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiPendingReplies_ConversationId_Status",
                table: "AiPendingReplies",
                columns: new[] { "ConversationId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AiPendingReplies");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
