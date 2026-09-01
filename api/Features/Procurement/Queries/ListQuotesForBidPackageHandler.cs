using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

public sealed class ListQuotesForBidPackageHandler
    : IQueryHandler<ListQuotesForBidPackage, IReadOnlyList<Quote>>
{
    private readonly JpmsContext context;

    public ListQuotesForBidPackageHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Quote>> HandleAsync(ListQuotesForBidPackage query, CancellationToken cancellationToken)
    {
        var entities = await context.Quotes.AsNoTracking().Where(quote => quote.BidPackageId == query.BidPackageId).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
