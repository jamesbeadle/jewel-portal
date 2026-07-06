using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Drawings;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

public sealed class ListBidPackageDrawingsHandler
    : IQueryHandler<ListBidPackageDrawings, IReadOnlyList<Drawing>>
{
    private readonly JpmsContext context;

    public ListBidPackageDrawingsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Drawing>> HandleAsync(ListBidPackageDrawings query, CancellationToken cancellationToken)
    {
        var drawings = await (
            from link in context.BidPackageDrawings
            where link.BidPackageId == query.BidPackageId
            join drawing in context.Drawings on link.DrawingId equals drawing.DrawingId
            orderby link.LinkedAt descending
            select drawing)
            .ToListAsync(cancellationToken);
        return drawings.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
