using Jewel.JPMS.Contracts.Leads;

namespace Jewel.JPMS.Api.Features.Leads.Queries;

public sealed class ListInformationChaseItemsForLeadHandler
    : IQueryHandler<ListInformationChaseItemsForLead, IReadOnlyList<InfoChaseItem>>
{
    private readonly JpmsContext context;

    public ListInformationChaseItemsForLeadHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<InfoChaseItem>> HandleAsync(
        ListInformationChaseItemsForLead query, CancellationToken cancellationToken)
    {
        var entities = await context.InfoChaseItems.AsNoTracking().Where(item => item.LeadId == query.LeadId).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
