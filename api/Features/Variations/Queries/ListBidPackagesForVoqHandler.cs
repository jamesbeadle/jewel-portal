using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Queries;

public sealed class ListBidPackagesForVoqHandler : IQueryHandler<ListBidPackagesForVoq, IReadOnlyList<BidPackage>>
{
    private readonly JpmsContext context;
    public ListBidPackagesForVoqHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<BidPackage>> HandleAsync(ListBidPackagesForVoq query, CancellationToken cancellationToken)
    {
        var entities = await context.BidPackages
            .Where(package => package.VariationOrderId == query.VariationOrderId)
            .OrderByDescending(package => package.CreatedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
