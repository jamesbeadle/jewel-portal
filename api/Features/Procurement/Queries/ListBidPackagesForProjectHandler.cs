using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

public sealed class ListBidPackagesForProjectHandler
    : IQueryHandler<ListBidPackagesForProject, IReadOnlyList<BidPackage>>
{
    private readonly JpmsContext context;

    public ListBidPackagesForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<BidPackage>> HandleAsync(ListBidPackagesForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.BidPackages.AsNoTracking()
            .Where(package => package.ProjectId == query.ProjectId)
            .OrderByDescending(package => package.CreatedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
