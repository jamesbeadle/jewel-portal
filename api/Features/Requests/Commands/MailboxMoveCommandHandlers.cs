using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>Discard = move the message out of the Inbox into the "General" folder. Best-effort and
/// idempotent: a message that has already gone is a no-op. The screen re-reads the Inbox live, so the
/// move result is reflected without any stored state.</summary>
public sealed class DiscardMessageHandler : ICommandHandler<DiscardMessage, Acknowledgement>
{
    private readonly IMailboxGraphClient graph;
    public DiscardMessageHandler(IMailboxGraphClient graph) { this.graph = graph; }

    public async Task<Acknowledgement> HandleAsync(DiscardMessage command, CancellationToken cancellationToken)
    {
        await graph.DiscardAsync(command.MessageId, command.InternetMessageId, cancellationToken);
        return new Acknowledgement(command.MessageId);
    }
}

/// <summary>Restore = move the message from "General" back into the Inbox.</summary>
public sealed class RestoreMessageHandler : ICommandHandler<RestoreMessage, Acknowledgement>
{
    private readonly IMailboxGraphClient graph;
    public RestoreMessageHandler(IMailboxGraphClient graph) { this.graph = graph; }

    public async Task<Acknowledgement> HandleAsync(RestoreMessage command, CancellationToken cancellationToken)
    {
        await graph.RestoreAsync(command.MessageId, command.InternetMessageId, cancellationToken);
        return new Acknowledgement(command.MessageId);
    }
}
