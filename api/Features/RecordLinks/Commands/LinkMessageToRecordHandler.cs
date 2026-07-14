using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.RecordLinks.Commands;

// Record-agnostic link: tag the email "JPMS/<record.TagReference>" (verified by read-back). The tag
// IS the association — no copy of the email is stored; the record reads its emails back live by the
// same tag. This is the single code path for linking an email to any record type; the legacy
// AssignMessageToRequest handler is a thin Request-typed adapter over this.
public sealed class LinkMessageToRecordHandler : ICommandHandler<LinkMessageToRecord, Acknowledgement>
{
    private readonly RecordProviderRegistry providers;
    private readonly IMailboxGraphClient graph;
    private readonly RecordThreadTagger threadTagger;

    public LinkMessageToRecordHandler(RecordProviderRegistry providers, IMailboxGraphClient graph, RecordThreadTagger threadTagger)
    {
        this.providers = providers;
        this.graph = graph;
        this.threadTagger = threadTagger;
    }

    public async Task<Acknowledgement> HandleAsync(LinkMessageToRecord command, CancellationToken cancellationToken)
    {
        var provider = providers.For(command.Type);

        var record = await provider.FindAsync(command.RecordId, cancellationToken)
            ?? throw new InvalidOperationException($"{command.Type} record '{command.RecordId}' not found.");

        // A closed request can't receive new triage emails — the pickers already hide them (see
        // RequestLinkProvider); this guards the command path itself so no caller can link one.
        if (record.Type == RecordType.Request &&
            string.Equals(record.StatusLabel, nameof(RequestStatus.Closed), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"{record.Reference} is closed, so this email can't be linked to it. Reopen the request first, or link the email to another record.");

        // Read the email back from the mailbox to confirm it's there and pick up a fresh threading id +
        // its conversation id (so we can tag the whole thread, not just this message).
        var snapshot = await graph.GetSnapshotAsync(command.MessageId, command.InternetMessageId, cancellationToken)
            ?? throw new InvalidOperationException("The email could not be read from the mailbox.");

        // The tag is the only link — and we apply it across the entire conversation so the record sees
        // the full thread context, not just the one clicked message. The anchor tag is verified; sibling
        // (reply/forward) tagging is best-effort.
        var tagged = await threadTagger.TagThreadAsync(
            command.MessageId, snapshot.InternetMessageId, snapshot.ConversationId,
            TriageCategories.ForRecord(record.TagReference), cancellationToken);
        if (!tagged)
            throw new InvalidOperationException("The email couldn't be tagged to the record. Please try again.");

        return new Acknowledgement(record.RecordId);
    }
}
