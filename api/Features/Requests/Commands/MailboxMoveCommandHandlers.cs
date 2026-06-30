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
        // Tag, then read back to confirm. Only report success once it's verified; otherwise fail so
        // the screen surfaces an error rather than a false "done".
        var ok = await graph.DiscardAsync(command.MessageId, command.InternetMessageId, cancellationToken);
        if (!ok) throw new InvalidOperationException("The email couldn't be tagged as discarded. Please try again.");
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
        var ok = await graph.RestoreAsync(command.MessageId, command.InternetMessageId, cancellationToken);
        if (!ok) throw new InvalidOperationException("The email couldn't be restored to the queue. Please try again.");
        return new Acknowledgement(command.MessageId);
    }
}

/// <summary>Remove one workflow tag from an email (unlink it from that process). If it was the last
/// tag the email returns to the triage queue. Verified by read-back; fails loudly if it doesn't stick.</summary>
public sealed class RemoveTagFromMessageHandler : ICommandHandler<RemoveTagFromMessage, Acknowledgement>
{
    private readonly IMailboxGraphClient graph;
    public RemoveTagFromMessageHandler(IMailboxGraphClient graph) { this.graph = graph; }

    public async Task<Acknowledgement> HandleAsync(RemoveTagFromMessage command, CancellationToken cancellationToken)
    {
        var ok = await graph.RemoveTagAsync(command.MessageId, command.InternetMessageId, command.Tag, cancellationToken);
        if (!ok) throw new InvalidOperationException("The tag couldn't be removed from the email. Please try again.");
        return new Acknowledgement(command.MessageId);
    }
}
