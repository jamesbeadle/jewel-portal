using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.RecordLinks;

// Reads a record's linked emails LIVE from the mailbox by its workflow tag (JPMS/<TagReference>),
// for any record type. Nothing is stored — the tag is the only link, so removing the tag removes the
// email from the record's context, and an email tagged to several records feeds all of them.
//
// This is the record-agnostic generalisation of RequestEmailReader: it resolves the tag via the
// record's provider instead of reading a RequestEntity directly. RequestEmailReader now delegates here.
public sealed class RecordEmailReader
{
    private readonly RecordProviderRegistry providers;
    private readonly IMailboxGraphClient graph;

    public RecordEmailReader(RecordProviderRegistry providers, IMailboxGraphClient graph)
    {
        this.providers = providers;
        this.graph = graph;
    }

    // All emails currently tagged to the record, oldest-first. Empty if the record is gone, has no
    // tagged mail, there's no provider for the type, or Graph isn't configured (null client → nothing).
    public async Task<IReadOnlyList<MailboxMessage>> ForRecordAsync(RecordType type, string recordId, CancellationToken ct)
    {
        if (!providers.TryGet(type, out var provider))
            return Array.Empty<MailboxMessage>();

        var record = await provider.FindAsync(recordId, ct);
        if (record is null)
            return Array.Empty<MailboxMessage>();

        var tag = TriageCategories.ForRecord(record.TagReference);

        var emails = new List<MailboxMessage>();
        string? cursor = null;
        var guard = 0;
        do
        {
            var page = await graph.ListByTagAsync(tag, cursor, 50, ct);
            emails.AddRange(page.Items);
            cursor = page.NextCursor;
        }
        while (cursor is not null && ++guard < 20);

        return emails.OrderBy(e => e.ReceivedAt).ToList();
    }
}
