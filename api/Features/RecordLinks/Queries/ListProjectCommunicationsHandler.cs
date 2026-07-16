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

        var page = await graph.ListByTagsAsync(queryTags.ToList(), query.Cursor, query.Take, cancellationToken);

        var items = page.Items
            .Select(message => new ProjectCommunication(
                message,
                message.Categories
                    .Where(labelsByTag.ContainsKey)
                    .Select(category => labelsByTag[category])
                    .OrderBy(link => link.Type)
                    .ThenBy(link => link.Reference, StringComparer.OrdinalIgnoreCase)
                    .ToList()))
            .ToList();

        return new ProjectCommunicationsPage(items, page.NextCursor, page.Total);
    }
}
