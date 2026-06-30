using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;

namespace Jewel.JPMS.Api.Features.RecordLinks.Commands;

// Catch-up handler: resolve the record's tag, then re-tag any Inbox replies that joined its threads
// after the original link. Record-agnostic via the provider registry. Returns the record id; the count
// tagged is incidental (best-effort).
public sealed class SyncRecordThreadTagsHandler : ICommandHandler<SyncRecordThreadTags, Acknowledgement>
{
    private readonly RecordProviderRegistry providers;
    private readonly RecordThreadTagger threadTagger;

    public SyncRecordThreadTagsHandler(RecordProviderRegistry providers, RecordThreadTagger threadTagger)
    {
        this.providers = providers;
        this.threadTagger = threadTagger;
    }

    public async Task<Acknowledgement> HandleAsync(SyncRecordThreadTags command, CancellationToken cancellationToken)
    {
        var provider = providers.For(command.Type);
        var record = await provider.FindAsync(command.RecordId, cancellationToken)
            ?? throw new InvalidOperationException($"{command.Type} record '{command.RecordId}' not found.");

        await threadTagger.SyncThreadsAsync(TriageCategories.ForRecord(record.TagReference), cancellationToken);
        return new Acknowledgement(record.RecordId);
    }
}
