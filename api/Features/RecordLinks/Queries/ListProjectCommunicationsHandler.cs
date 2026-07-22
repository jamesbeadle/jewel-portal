using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.RecordLinks.Queries;

// A project's tagged emails across every linkable record type, read live from the mailbox in one
// server-side-filtered page. The handler resolves the project's records through the provider
// registry, ORs their tags into a single Graph read (ListByTagsAsync — the same primitive behind
// the triage Tagged tab), and labels each returned email with the record(s) its tags resolve to.
//
// Two tag sets are built from the same provider sweep:
//  - the QUERY set — the tags actually filtered on: one provider's when Type is given, all otherwise;
//  - the LABEL map — always every provider's, so an email matched via its cost-centre tag still shows
//    its RFI chip too (an email can feed several records at once).
//
// Scale note: the OR filter carries one clause per record tag, so the all-types read grows with the
// project's record count. Fine at current scale (the Tagged tab already reads this way); if a project
// ever accumulates enough records for Graph to reject the filter, chunk the query set and merge —
// don't silently truncate it.
public sealed class ListProjectCommunicationsHandler
    : IQueryHandler<ListProjectCommunications, ProjectCommunicationsPage>
{
    private readonly RecordProviderRegistry providers;
    private readonly IMailboxGraphClient graph;

    public ListProjectCommunicationsHandler(RecordProviderRegistry providers, IMailboxGraphClient graph)
    {
        this.providers = providers;
        this.graph = graph;
    }

    public async Task<ProjectCommunicationsPage> HandleAsync(ListProjectCommunications query, CancellationToken cancellationToken)
    {
        var labelsByTag = new Dictionary<string, ProjectCommunicationLink>(StringComparer.OrdinalIgnoreCase);
        var queryTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var provider in providers.All)
        {
            var records = await provider.ForProjectAsync(query.ProjectId, cancellationToken);
            var includeInQuery = query.Type is null || provider.Type == query.Type;

            foreach (var record in records)
            {
                var tag = TriageCategories.ForRecord(record.TagReference);
                // Reference namespaces are asserted not to collide across providers, but use the
                // indexer so a bad provider can't take the whole tab down.
                labelsByTag[tag] = new ProjectCommunicationLink(record.Type, record.Reference, record.Title, tag);
                if (includeInQuery)
                    queryTags.Add(tag);
            }
        }

        // No records of the requested type (or none at all) → nothing can be tagged; skip the
        // mailbox read entirely. Never fall through to ListByTagsAsync with an empty set — that
        // reads EVERY tagged email in the mailbox, other projects included.
        if (queryTags.Count == 0)
            return new ProjectCommunicationsPage(Array.Empty<ProjectCommunication>(), null, 0);

        // Exchange rejects an OR filter with too many category clauses as "too complex" — and the
        // Graph client surfaces that as an EMPTY page, so the all-types view (one clause per
        // record, cost centres included) silently rendered as "no tagged emails". Small tag sets
        // keep the precise single-filter read; large ones scan the marker-tagged stream (the same
        // read behind triage's Tagged tab — one simple clause) and intersect with the project's
        // tags here. The scan returns Total = 0, which the UI treats as "count unknown".
        var page = queryTags.Count <= MaxOrFilterTags
            ? await graph.ListByTagsAsync(queryTags.ToList(), query.Cursor, query.Take, newestFirst: false, cancellationToken)
            : await ScanTaggedForAsync(queryTags, query.Cursor, query.Take, cancellationToken);

        // Pathway filter (the Communications tab's Client/Subcontractor/Internal segmented control):
        // applied per page against each message's bucket category. Total becomes 0 ("count
        // unknown") because the full count would need the whole stream. A page can filter down to
        // fewer than Take items while NextCursor still advances — the UI keeps paging as usual.
        var messages = page.Items;
        var total = page.Total;
        if (!string.IsNullOrWhiteSpace(query.Bucket))
        {
            var bucketTag = TriageCategories.WorkflowPrefix + query.Bucket.Trim();
            messages = messages
                .Where(message => string.Equals(message.Bucket, bucketTag, StringComparison.OrdinalIgnoreCase))
                .ToList();
            total = 0;
        }

        var items = messages
            .Select(message => new ProjectCommunication(
                message,
                message.Categories
                    .Where(labelsByTag.ContainsKey)
                    .Select(category => labelsByTag[category])
                    .OrderBy(link => link.Type)
                    .ThenBy(link => link.Reference, StringComparer.OrdinalIgnoreCase)
                    .ToList()))
            .ToList();

        return new ProjectCommunicationsPage(items, page.NextCursor, total);
    }

    // Conservatively below Exchange's "restriction or sort order is too complex" threshold for
    // OR'd category clauses.
    private const int MaxOrFilterTags = 10;

    // How many marker-stream pages (of up to 100 messages) one request will walk before handing
    // back a cursor. Bounds the worst case; the UI's Load more continues from the cursor.
    private const int MaxScanPages = 20;

    /// <summary>
    /// Pages through every marker-tagged email (one simple filter Graph always accepts) and keeps
    /// the ones carrying any of this project's tags. Whole pages are consumed so the returned
    /// cursor is simply the underlying stream's cursor — a page can therefore run slightly over
    /// <paramref name="take"/>. Total is unknowable without a full scan, so 0 is returned and the
    /// UI hides its "Showing x of y" counter.
    /// </summary>
    private async Task<MailboxPage> ScanTaggedForAsync(
        IReadOnlySet<string> projectTags, string? cursor, int take, CancellationToken cancellationToken)
    {
        var collected = new List<MailboxMessage>();
        var next = cursor;
        for (var pages = 0; pages < MaxScanPages; pages++)
        {
            var page = await graph.ListTaggedAsync(next, 100, newestFirst: false, cancellationToken);
            collected.AddRange(page.Items.Where(message => message.Categories.Any(projectTags.Contains)));
            next = page.NextCursor;
            if (next is null || collected.Count >= take) break;
        }
        return new MailboxPage(collected, next, 0);
    }
}
