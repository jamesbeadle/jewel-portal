using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // The assistant's tool_use blocks, persisted on the row that emitted them.
    //
    // The turn loop moved from "one request runs the whole loop" to "one request is one hop, the
    // client pumps" — so the panel can say what it is doing instead of showing a spinner. That means
    // the transcript is rebuilt between hops, and Anthropic rejects a tool_result whose matching
    // tool_use is not in the assistant turn above it. Storing the blocks is what makes the replay
    // faithful.
    //
    // Rows written before this column exists have it null; the rebuilder falls back to replaying
    // their results as prose, so old conversations still load.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260726230000_AddAiToolCallsJson")]
    public partial class AddAiToolCallsJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'[dbo].[AiConversationMessages]', N'ToolCallsJson') IS NULL
    ALTER TABLE [dbo].[AiConversationMessages] ADD [ToolCallsJson] nvarchar(max) NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'[dbo].[AiConversationMessages]', N'ToolCallsJson') IS NOT NULL
    ALTER TABLE [dbo].[AiConversationMessages] DROP COLUMN [ToolCallsJson];
");
        }
    }
}
