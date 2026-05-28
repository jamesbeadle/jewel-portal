using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Leads.Queries;

public sealed class ListInformationChaseItemsForLeadHandler
    : IQueryHandler<ListInformationChaseItemsForLead, IReadOnlyList<InfoChaseItem>>
{
    private readonly JpmsContext context;

    public ListInformationChaseItemsForLeadHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<InfoChaseItem>> HandleAsync(
        ListInformationChaseItemsForLead query, CancellationToken cancellationToken)
    {
        var entities = await context.InfoChaseItems.Where(item => item.LeadId == query.LeadId).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
