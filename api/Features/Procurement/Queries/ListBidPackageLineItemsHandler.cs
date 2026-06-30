using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

public sealed class ListBidPackageLineItemsHandler
    : IQueryHandler<ListBidPackageLineItems, IReadOnlyList<BidPackageLineItem>>
{
    private readonly JpmsContext context;

    public ListBidPackageLineItemsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<BidPackageLineItem>> HandleAsync(ListBidPackageLineItems query, CancellationToken cancellationToken)
    {
        var entities = await context.BidPackageLineItems
            .Where(item => item.BidPackageId == query.BidPackageId)
            .OrderBy(item => item.SortOrder)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
