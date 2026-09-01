using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

public sealed class GetBidPackageByIdHandler : IQueryHandler<GetBidPackageById, BidPackage?>
{
    private readonly JpmsContext context;

    public GetBidPackageByIdHandler(JpmsContext context) { this.context = context; }

    public async Task<BidPackage?> HandleAsync(GetBidPackageById query, CancellationToken cancellationToken)
    {
        var entity = await context.BidPackages.AsNoTracking()
            .FirstOrDefaultAsync(package => package.BidPackageId == query.BidPackageId, cancellationToken);
        return entity?.ToModel();
    }
}
