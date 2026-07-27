using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Ai.Commands;

/// <summary>
/// Starts a turn: records the user's message, then runs the first hop.
///
/// <para>Thin on purpose — the work is in <see cref="AiTurnRunner"/>, which
/// <c>ContinueAiTurnHandler</c> also drives, so a first hop and a fifth hop cannot diverge.</para>
/// </summary>
public sealed class SendAiMessageHandler : ICommandHandler<SendAiMessage, AiTurnResult>
{
    private readonly JpmsContext context;
    private readonly AiCaller caller;
    private readonly AiTurnRunner runner;

    public SendAiMessageHandler(JpmsContext context, AiCaller caller, AiTurnRunner runner)
    {
        this.context = context;
        this.caller = caller;
        this.runner = runner;
    }

    public async Task<AiTurnResult> HandleAsync(SendAiMessage command, CancellationToken cancellationToken)
    {
        var user = caller.Current
            ?? throw new InvalidOperationException("The assistant needs a signed-in user.");

        var conversation = await LoadOrStartAsync(command, cancellationToken);

        var sequence = await context.AiConversationMessages
            .Where(row => row.ConversationId == conversation.ConversationId)
            .Select(row => (int?)row.Sequence)
            .MaxAsync(cancellationToken) ?? 0;

        context.AiConversationMessages.Add(new AiConversationMessageEntity
        {
            MessageId = Guid.NewGuid().ToString("N"),
            ConversationId = conversation.ConversationId,
            Role = (int)AiChatRole.User,
            Body = command.Message,
            Sequence = sequence + 1,
            PostedAt = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync(cancellationToken);

        var result = await runner.RunHopAsync(conversation, user, command.Scope, cancellationToken);

        // The client rendered the user's message optimistically; echoing the server's copy back keeps
        // ids consistent for anything that later wants to reference a specific turn.
        return result;
    }

    private async Task<AiConversationEntity> LoadOrStartAsync(SendAiMessage command, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(command.ConversationId))
        {
            var existing = await context.AiConversations
                .FirstOrDefaultAsync(row => row.ConversationId == command.ConversationId, ct);

            // Scoped to whoever started it — a conversation id is not a capability.
            if (existing is not null
                && string.Equals(existing.StartedByEmail, command.SentByEmail, StringComparison.OrdinalIgnoreCase))
            {
                return existing;
            }
        }

        var now = DateTimeOffset.UtcNow;
        var conversation = new AiConversationEntity
        {
            ConversationId = Guid.NewGuid().ToString("N"),
            ProjectId = command.Scope?.ProjectId,
            Route = command.Scope?.Route,
            CapabilityKey = "orchestrator",
            StartedByEmail = command.SentByEmail,
            Title = command.Message.Length <= 120 ? command.Message : command.Message[..120],
            StartedAt = now,
            LastMessageAt = now
        };
        context.AiConversations.Add(conversation);
        return conversation;
    }
}
