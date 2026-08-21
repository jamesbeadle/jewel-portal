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
                          // them would rewrite what the user actually saw. The Context rows that
                          // DO replay are attachments (ToolName marks them, image or text): the
                          // panel shows "Attached file.xlsx" in the transcript, never the contents.
                          && row.Role != (int)AiChatRole.Tool
                          && (row.Role != (int)AiChatRole.Context
                              || row.ToolName == "attachment" || row.ToolName == "attachment-image")
                          && (row.Role != (int)AiChatRole.Assistant || row.ToolCallsJson == null)
                          && row.Body != null && row.Body != "")
            .OrderBy(row => row.Sequence)
            .Select(row => new AiChatMessage(
                row.MessageId, (AiChatRole)row.Role, row.Body, row.ToolName, row.PostedAt))
            .ToListAsync(cancellationToken);

        // An attachment row's body is the full extracted file — the transcript wants only its
        // first line ("The user attached … (2 sheets · 148 rows).") as the display label.
        return rows
            .Select(row => row.Role == AiChatRole.Context
                ? row with { Body = FirstLine(row.Body) }
                : row)
            .ToList();
    }

    private static string FirstLine(string body)
    {
        var lineBreak = body.IndexOf('\n');
        return lineBreak < 0 ? body : body[..lineBreak].TrimEnd();
    }
}
