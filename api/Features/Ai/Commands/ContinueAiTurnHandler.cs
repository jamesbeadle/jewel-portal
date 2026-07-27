using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Ai.Commands;

/// <summary>The next hop of a turn already in flight.</summary>
public sealed class ContinueAiTurnHandler : ICommandHandler<ContinueAiTurn, AiTurnResult>
{
    private readonly JpmsContext context;
    private readonly AiCaller caller;
    private readonly AiTurnRunner runner;

    public ContinueAiTurnHandler(JpmsContext context, AiCaller caller, AiTurnRunner runner)
    {
        this.context = context;
        this.caller = caller;
        this.runner = runner;
    }

    public async Task<AiTurnResult> HandleAsync(ContinueAiTurn command, CancellationToken cancellationToken)
    {
        var user = caller.Current
            ?? throw new InvalidOperationException("The assistant needs a signed-in user.");

        var conversation = await context.AiConversations
            .FirstOrDefaultAsync(row => row.ConversationId == command.ConversationId, cancellationToken);

        if (conversation is null
            || !string.Equals(conversation.StartedByEmail, command.SentByEmail, StringComparison.OrdinalIgnoreCase))
        {
            // Same rule as starting a turn: you can only continue your own conversation.
            throw new InvalidOperationException("That conversation could not be found.");
        }

        return await runner.RunHopAsync(conversation, user, command.Scope, cancellationToken);
    }
}
