using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // The record a conversation was about, so a task conversation can be found from the record
    // rather than only from the person who started it.
    //
    // The assistant now drafts a variation inside the Create Variation Order Quote dialog, back and
    // forth with a PM, and that exchange is the account of how the variation came to say what it
    // says. docs/ai/00-agent-architecture.md §8 sets the test: "the assistant suggested it and Nigel
    // confirmed it at 09:14, having changed the value" has to be reconstructable years later. Two
    // nullable columns are what turns that from a scan into a lookup.
    //
    // Both null for a general conversation — those are scoped to a page, not a record.
    //
    // Guarded raw SQL, matching the house pattern: safe to re-run.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260727100000_AddAiConversationRecordScope")]
    public partial class AddAiConversationRecordScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'[dbo].[AiConversations]', N'ScopeRecordType') IS NULL
    ALTER TABLE [dbo].[AiConversations] ADD [ScopeRecordType] nvarchar(64) NULL;
");
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'[dbo].[AiConversations]', N'ScopeRecordId') IS NULL
    ALTER TABLE [dbo].[AiConversations] ADD [ScopeRecordId] nvarchar(64) NULL;
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AiConversations_ScopeRecordId'
               AND object_id = OBJECT_ID(N'[dbo].[AiConversations]'))
    CREATE INDEX [IX_AiConversations_ScopeRecordId]
        ON [dbo].[AiConversations] ([ScopeRecordId], [LastMessageAt]);
");
        }

        /// <inheritdoc />
        // Deliberately empty. Dropping the columns would discard the only link between a conversation
        // and the record it produced, and nothing reads them that a rollback would break.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
