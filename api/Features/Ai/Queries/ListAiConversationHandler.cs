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
                          && row.Role != (int)AiChatRole.Tool)
            .OrderBy(row => row.Sequence)
            .Select(row => new AiChatMessage(
                row.MessageId, (AiChatRole)row.Role, row.Body, row.ToolName, row.PostedAt))
            .ToListAsync(cancellationToken);

        return rows;
    }
}
