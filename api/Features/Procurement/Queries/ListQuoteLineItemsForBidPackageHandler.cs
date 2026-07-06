using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

// Every quote line across the package's quotes — the comparison view aligns them to the package's
// line items client-side (via BidPackageLineItemId).
public sealed class ListQuoteLineItemsForBidPackageHandler
    : IQueryHandler<ListQuoteLineItemsForBidPackage, IReadOnlyList<QuoteLineItem>>
{
    private readonly JpmsContext context;

    public ListQuoteLineItemsForBidPackageHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<QuoteLineItem>> HandleAsync(ListQuoteLineItemsForBidPackage query, CancellationToken cancellationToken)
    {
        var lines = await (
            from line in context.QuoteLineItems
            join quote in context.Quotes on line.QuoteId equals quote.QuoteId
            where quote.BidPackageId == query.BidPackageId
            select line)
            .ToListAsync(cancellationToken);

        return lines.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
