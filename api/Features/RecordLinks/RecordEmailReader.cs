using Jewel.JPMS.Api.Features.MailboxIntake.Graph;

namespace Jewel.JPMS.Api.Features.RecordLinks;

// Reads a record's linked emails LIVE from the mailbox by its workflow tag (JPMS/<TagReference>),
// for any record type. Nothing is stored — the tag is the only link, so removing the tag removes the
// email from the record's context, and an email tagged to several records feeds all of them.
//
// This is the record-agnostic generalisation of RequestEmailReader: it resolves the tag via the
// record's provider instead of reading a RequestEntity directly. RequestEmailReader now delegates here.
//
// A provider that names COMPANIONS (ICompanionRecordProvider, 2026-09-15 — a valuation claim and
// the statements frozen from it) has their tags read too, merged with the record's own: the two
// rows are one period's story, and an email filed to either shows on both, once.
public sealed class RecordEmailReader
{
    private readonly RecordProviderRegistry providers;
    private readonly IMailboxGraphClient graph;
    private readonly SignedInCaller caller;

    public RecordEmailReader(RecordProviderRegistry providers, IMailboxGraphClient graph, SignedInCaller caller)
    {
        this.providers = providers;
        this.graph = graph;
        this.caller = caller;
    }

    // All emails currently tagged to the record, oldest-first. Empty if the record is gone, has no
    // tagged mail, there's no provider for the type, or Graph isn't configured (null client → nothing).
    public async Task<IReadOnlyList<MailboxMessage>> ForRecordAsync(RecordType type, string recordId, CancellationToken ct)
    {
        if (!caller.MayReadInternalCorrespondence)
            return Array.Empty<MailboxMessage>();
        if (!providers.TryGet(type, out var provider))
            return Array.Empty<MailboxMessage>();

        var record = await provider.FindAsync(recordId, ct);
        if (record is null)
            return Array.Empty<MailboxMessage>();

        var tags = new List<string> { TriageCategories.ForRecord(record.TagReference) };
        if (provider is ICompanionRecordProvider companions)
            foreach (var stem in await companions.CompanionTagReferencesAsync(record, ct))
                tags.Add(TriageCategories.ForRecord(stem));

        var emails = new List<MailboxMessage>();
        foreach (var tag in tags.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            string? cursor = null;
            var guard = 0;
            do
            {
                var page = await graph.ListByTagAsync(tag, cursor, 50, ct);
                emails.AddRange(page.Items);
                cursor = page.NextCursor;
            }
            while (cursor is not null && ++guard < 20);
        }

        // An email tagged to the record AND a companion arrives from both reads — keep it once,
        // keyed the way the snapshot viewer always did (internet message id, else the Graph id).
        return emails
            .GroupBy(email => string.IsNullOrEmpty(email.InternetMessageId) ? email.Id : email.InternetMessageId)
            .Select(group => group.First())
            .OrderBy(email => email.ReceivedAt)
            .ToList();
    }
}
