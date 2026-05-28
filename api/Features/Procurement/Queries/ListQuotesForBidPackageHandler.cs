using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

public sealed class ListQuotesForBidPackageHandler
    : IQueryHandler<ListQuotesForBidPackage, IReadOnlyList<Quote>>
{
    private readonly JpmsContext context;

    public ListQuotesForBidPackageHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Quote>> HandleAsync(ListQuotesForBidPackage query, CancellationToken cancellationToken)
    {
        var entities = await context.Quotes.Where(quote => quote.BidPackageId == query.BidPackageId).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
