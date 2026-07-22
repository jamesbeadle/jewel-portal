using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>Discard = tag the email "not a request". Like linking to a record, this applies across the
/// WHOLE Inbox conversation (the email + its replies share a Graph conversationId), so discarding a
/// thread removes all of it from the queue, not just the one message. The anchor tag is verified;
/// sibling tagging is best-effort. The screen re-reads the Inbox live, so the result shows immediately.
/// A discard on a client-pathway thread is a client-facing decision, so it lands in the audit trail.</summary>
public sealed class DiscardMessageHandler : ICommandHandler<DiscardMessage, Acknowledgement>
{
    private readonly IMailboxGraphClient graph;
    private readonly RecordThreadTagger threadTagger;
    private readonly AuditTrail audit;
    public DiscardMessageHandler(IMailboxGraphClient graph, RecordThreadTagger threadTagger, AuditTrail audit)
    { this.graph = graph; this.threadTagger = threadTagger; this.audit = audit; }

    public async Task<Acknowledgement> HandleAsync(DiscardMessage command, CancellationToken cancellationToken)
    {
        // Read the email back to pick up its conversation id, then tag the whole thread discarded
        // (verified on the anchor). Fail loudly if the anchor doesn't stick, so the screen shows an
        // error rather than a false "done".
        var snapshot = await graph.GetSnapshotAsync(command.MessageId, command.InternetMessageId, cancellationToken);
        var ok = await threadTagger.TagThreadAsync(
            command.MessageId, command.InternetMessageId, snapshot?.ConversationId, TriageCategories.Discarded, cancellationToken);
        if (!ok) throw new InvalidOperationException("The email couldn't be tagged as discarded. Please try again.");

        if (HasClientBucket(snapshot))
            await audit.WriteAsync(
                AuditEventType.Discarded,
                "Client-pathway thread discarded.",
                pathway: "Client",
                conversationId: snapshot?.ConversationId,
                emailMessageId: command.MessageId,
                internetMessageId: snapshot?.InternetMessageId,
                cancellationToken: cancellationToken);

        return new Acknowledgement(command.MessageId);
    }

    internal static bool HasClientBucket(MailboxSnapshot? snapshot) =>
        snapshot?.Categories?.Any(c => c.Equals(TriageCategories.Client, StringComparison.OrdinalIgnoreCase)) == true;
}

/// <summary>Restore = the inverse of discard: remove the discarded tag from the email AND the rest of
/// its conversation, putting the whole thread back into the triage queue.</summary>
public sealed class RestoreMessageHandler : ICommandHandler<RestoreMessage, Acknowledgement>
{
    private readonly IMailboxGraphClient graph;
    private readonly RecordThreadTagger threadTagger;
    private readonly AuditTrail audit;
    public RestoreMessageHandler(IMailboxGraphClient graph, RecordThreadTagger threadTagger, AuditTrail audit)
    { this.graph = graph; this.threadTagger = threadTagger; this.audit = audit; }

    public async Task<Acknowledgement> HandleAsync(RestoreMessage command, CancellationToken cancellationToken)
    {
        var snapshot = await graph.GetSnapshotAsync(command.MessageId, command.InternetMessageId, cancellationToken);
        var ok = await threadTagger.UntagThreadAsync(
            command.MessageId, command.InternetMessageId, snapshot?.ConversationId, TriageCategories.Discarded, cancellationToken);
        if (!ok) throw new InvalidOperationException("The email couldn't be restored to the queue. Please try again.");

        if (DiscardMessageHandler.HasClientBucket(snapshot))
            await audit.WriteAsync(
                AuditEventType.Restored,
                "Client-pathway thread restored to the queue.",
                pathway: "Client",
                conversationId: snapshot?.ConversationId,
                emailMessageId: command.MessageId,
                internetMessageId: snapshot?.InternetMessageId,
                cancellationToken: cancellationToken);

        return new Acknowledgement(command.MessageId);
    }
}

/// <summary>Remove one workflow tag from an email (unlink it from that process). If it was the last
/// tag the email returns to the triage queue — and any pathway tag goes with it (an email never sits
/// outside the queue carrying only a pathway; see MailboxGraphClient.RemoveTagAsync). Verified by
/// read-back; fails loudly if it doesn't stick.</summary>
public sealed class RemoveTagFromMessageHandler : ICommandHandler<RemoveTagFromMessage, Acknowledgement>
{
    private readonly IMailboxGraphClient graph;
    private readonly AuditTrail audit;
    public RemoveTagFromMessageHandler(IMailboxGraphClient graph, AuditTrail audit)
    { this.graph = graph; this.audit = audit; }

    public async Task<Acknowledgement> HandleAsync(RemoveTagFromMessage command, CancellationToken cancellationToken)
    {
        // Snapshot first (for the pathway + conversation id) — the removal may take the bucket with it.
        var snapshot = await graph.GetSnapshotAsync(command.MessageId, command.InternetMessageId, cancellationToken);

        var ok = await graph.RemoveTagAsync(command.MessageId, command.InternetMessageId, command.Tag, cancellationToken);
        if (!ok) throw new InvalidOperationException("The tag couldn't be removed from the email. Please try again.");

        if (DiscardMessageHandler.HasClientBucket(snapshot))
            await audit.WriteAsync(
                AuditEventType.TagRemoved,
                $"Tag {command.Tag} removed from a client-pathway email.",
                pathway: "Client",
                conversationId: snapshot?.ConversationId,
                emailMessageId: command.MessageId,
                internetMessageId: snapshot?.InternetMessageId,
                cancellationToken: cancellationToken);

        return new Acknowledgement(command.MessageId);
    }
}
