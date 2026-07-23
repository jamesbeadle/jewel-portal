using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Queries;

public sealed class GetVoqByRequestHandler : IQueryHandler<GetVoqByRequest, VariationOrder?>
{
    private readonly JpmsContext context;
    public GetVoqByRequestHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrder?> HandleAsync(GetVoqByRequest query, CancellationToken cancellationToken)
    {
        var entity = await context.VariationOrders
            .Where(vo => vo.RequestId == query.RequestId)
            .OrderByDescending(vo => vo.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return entity?.ToModel();
    }
}
