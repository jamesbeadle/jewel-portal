using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Retires the in-portal chat's storage (2026-08-27): conversations, their messages, chat file
    /// attachments, and the in-flight reply rows. The turn-based side chat is replaced by the MCP
    /// connector (docs/ai/10-mcp-connector.md), which keeps no conversation state — each person's
    /// own AI tool holds the conversation; the portal keeps the audit trail (AgentActivity) and
    /// the skill store, which stay. Purely subtractive; apply AFTER (or with) the deploy — the
    /// outgoing code still reads these tables. The ai-attachments blob container's bytes are
    /// swept separately by infra/run-ai-attachments-lifecycle.sh.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260827200000_DropAiChatTables")]
    public partial class DropAiChatTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AiPendingReplies");
            migrationBuilder.DropTable(name: "AiAttachments");
            migrationBuilder.DropTable(name: "AiConversationMessages");
            migrationBuilder.DropTable(name: "AiConversations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Subtractive by design — recreating the chat tables would resurrect a retired
            // feature. Restore from backup if this ever genuinely needs reversing.
            throw new NotSupportedException("The in-portal chat's tables are not recreated by rollback.");
        }
    }
}
