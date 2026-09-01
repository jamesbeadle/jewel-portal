using Jewel.JPMS.Contracts.Leads;

namespace Jewel.JPMS.Api.Features.Leads.Queries;

public sealed class ListSiteVisitsForLeadHandler
    : IQueryHandler<ListSiteVisitsForLead, IReadOnlyList<SiteVisit>>
{
    private readonly JpmsContext context;

    public ListSiteVisitsForLeadHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<SiteVisit>> HandleAsync(
        ListSiteVisitsForLead query, CancellationToken cancellationToken)
    {
        var entities = await context.SiteVisits.AsNoTracking().Where(visit => visit.LeadId == query.LeadId).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
