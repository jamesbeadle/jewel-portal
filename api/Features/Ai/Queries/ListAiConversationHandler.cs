using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Ai.Queries;

/// <summary>Replays a conversation for the panel. Tool rows are excluded — they are audit, not chat.</summary>
public sealed class ListAiConversationHandler
    : IQueryHandler<ListAiConversation, IReadOnlyList<AiChatMessage>>
{
    private readonly JpmsContext context;

    public ListAiConversationHandler(JpmsContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<AiChatMessage>> HandleAsync(
        ListAiConversation query, CancellationToken cancellationToken)
    {
        var rows = await context.AiConversationMessages
            .AsNoTracking()
            .Where(row => row.ConversationId == query.ConversationId
                          // Tool results and carried-over context are the model's reading, not the
                          // user's conversation; assistant rows that carried tool calls are working
                          // narration shown as a status line at the time, not bubbles — replaying
                          // them would rewrite what the user actually saw.
                          && row.Role != (int)AiChatRole.Tool
                          && row.Role != (int)AiChatRole.Context
                          && (row.Role != (int)AiChatRole.Assistant || row.ToolCallsJson == null)
                          && row.Body != null && row.Body != "")
            .OrderBy(row => row.Sequence)
            .Select(row => new AiChatMessage(
                row.MessageId, (AiChatRole)row.Role, row.Body, row.ToolName, row.PostedAt))
            .ToListAsync(cancellationToken);

        return rows;
    }
}
