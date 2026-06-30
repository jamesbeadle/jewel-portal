using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>Discard = tag the email "not a request". Like linking to a record, this applies across the
/// WHOLE Inbox conversation (the email + its replies share a Graph conversationId), so discarding a
/// thread removes all of it from the queue, not just the one message. The anchor tag is verified;
/// sibling tagging is best-effort. The screen re-reads the Inbox live, so the result shows immediately.</summary>
public sealed class DiscardMessageHandler : ICommandHandler<DiscardMessage, Acknowledgement>
{
    private readonly IMailboxGraphClient graph;
    private readonly RecordThreadTagger threadTagger;
    public DiscardMessageHandler(IMailboxGraphClient graph, RecordThreadTagger threadTagger)
    { this.graph = graph; this.threadTagger = threadTagger; }

    public async Task<Acknowledgement> HandleAsync(DiscardMessage command, CancellationToken cancellationToken)
    {
        // Read the email back to pick up its conversation id, then tag the whole thread discarded
        // (verified on the anchor). Fail loudly if the anchor doesn't stick, so the screen shows an
        // error rather than a false "done".
        var snapshot = await graph.GetSnapshotAsync(command.MessageId, command.InternetMessageId, cancellationToken);
        var ok = await threadTagger.TagThreadAsync(
            command.MessageId, command.InternetMessageId, snapshot?.ConversationId, TriageCategories.Discarded, cancellationToken);
        if (!ok) throw new InvalidOperationException("The email couldn't be tagged as discarded. Please try again.");
        return new Acknowledgement(command.MessageId);
    }
}

/// <summary>Restore = the inverse of discard: remove the discarded tag from the email AND the rest of
/// its conversation, putting the whole thread back into the triage queue.</summary>
public sealed class RestoreMessageHandler : ICommandHandler<RestoreMessage, Acknowledgement>
{
    private readonly IMailboxGraphClient graph;
    private readonly RecordThreadTagger threadTagger;
    public RestoreMessageHandler(IMailboxGraphClient graph, RecordThreadTagger threadTagger)
    { this.graph = graph; this.threadTagger = threadTagger; }

    public async Task<Acknowledgement> HandleAsync(RestoreMessage command, CancellationToken cancellationToken)
    {
        var snapshot = await graph.GetSnapshotAsync(command.MessageId, command.InternetMessageId, cancellationToken);
        var ok = await threadTagger.UntagThreadAsync(
            command.MessageId, command.InternetMessageId, snapshot?.ConversationId, TriageCategories.Discarded, cancellationToken);
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
