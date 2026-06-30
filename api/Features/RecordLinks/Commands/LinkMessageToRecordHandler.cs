using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;

namespace Jewel.JPMS.Api.Features.RecordLinks.Commands;

// Record-agnostic link: tag the email "JPMS/<record.TagReference>" (verified by read-back). The tag
// IS the association — no copy of the email is stored; the record reads its emails back live by the
// same tag. This is the single code path for linking an email to any record type; the legacy
// AssignMessageToRequest handler is a thin Request-typed adapter over this.
public sealed class LinkMessageToRecordHandler : ICommandHandler<LinkMessageToRecord, Acknowledgement>
{
    private readonly RecordProviderRegistry providers;
    private readonly IMailboxGraphClient graph;

    public LinkMessageToRecordHandler(RecordProviderRegistry providers, IMailboxGraphClient graph)
    {
        this.providers = providers;
        this.graph = graph;
    }

    public async Task<Acknowledgement> HandleAsync(LinkMessageToRecord command, CancellationToken cancellationToken)
    {
        var provider = providers.For(command.Type);

        var record = await provider.FindAsync(command.RecordId, cancellationToken)
            ?? throw new InvalidOperationException($"{command.Type} record '{command.RecordId}' not found.");

        // Read the email back from the mailbox to confirm it's there and pick up a fresh threading id.
        var snapshot = await graph.GetSnapshotAsync(command.MessageId, command.InternetMessageId, cancellationToken)
            ?? throw new InvalidOperationException("The email could not be read from the mailbox.");

        // The tag is the only link to the email — identical mechanism for every record type.
        var tagged = await graph.AssignAsync(
            command.MessageId, snapshot.InternetMessageId,
            TriageCategories.ForRecord(record.TagReference), cancellationToken);
        if (!tagged)
            throw new InvalidOperationException("The email couldn't be tagged to the record. Please try again.");

        return new Acknowledgement(record.RecordId);
    }
}
