using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Ai.Commands;

/// <summary>A reply that outlived its request's inline wait, collected and applied as the hop it
/// belongs to (docs/ai/07-reply-collection.md). Thin like its siblings — the work is in
/// <see cref="AiTurnRunner.CollectAsync"/>.</summary>
public sealed class CollectAiReplyHandler : ICommandHandler<CollectAiReply, AiTurnResult>
{
    private readonly JpmsContext context;
    private readonly AiCaller caller;
    private readonly AiTurnRunner runner;

    public CollectAiReplyHandler(JpmsContext context, AiCaller caller, AiTurnRunner runner)
    {
        this.context = context;
        this.caller = caller;
        this.runner = runner;
    }

    public async Task<AiTurnResult> HandleAsync(CollectAiReply command, CancellationToken cancellationToken)
    {
        var user = caller.Current
            ?? throw new InvalidOperationException("The assistant needs a signed-in user.");

        var conversation = await context.AiConversations
            .FirstOrDefaultAsync(row => row.ConversationId == command.ConversationId, cancellationToken);

        if (conversation is null
            || !string.Equals(conversation.StartedByEmail, command.SentByEmail, StringComparison.OrdinalIgnoreCase))
        {
            // Same rule as starting a turn: you can only collect for your own conversation.
            throw new InvalidOperationException("That conversation could not be found.");
        }

        return await runner.CollectAsync(conversation, user, command.Scope, command.ReplyId, cancellationToken);
    }
}
