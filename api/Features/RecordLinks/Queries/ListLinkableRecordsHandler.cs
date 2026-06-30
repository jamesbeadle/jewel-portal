using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.RecordLinks.Queries;

// All records of a given type on a project, projected for the triage "link to existing" picker. Routes
// straight to the type's provider — the handler itself stays type-agnostic.
public sealed class ListLinkableRecordsHandler : IQueryHandler<ListLinkableRecords, IReadOnlyList<LinkableRecord>>
{
    private readonly RecordProviderRegistry providers;

    public ListLinkableRecordsHandler(RecordProviderRegistry providers) { this.providers = providers; }

    public async Task<IReadOnlyList<LinkableRecord>> HandleAsync(ListLinkableRecords query, CancellationToken cancellationToken)
    {
        if (!providers.TryGet(query.Type, out var provider))
            return Array.Empty<LinkableRecord>();
        return await provider.ForProjectAsync(query.ProjectId, cancellationToken);
    }
}
