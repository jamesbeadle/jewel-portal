using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Queries;

public sealed class GetVariationOrderByVoqHandler : IQueryHandler<GetVariationOrderByVoq, VariationOrder?>
{
    private readonly JpmsContext context;
    public GetVariationOrderByVoqHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrder?> HandleAsync(GetVariationOrderByVoq query, CancellationToken cancellationToken)
    {
        var entity = await context.VariationOrders
            .Where(vo => vo.VariationOrderQuoteId == query.VariationOrderQuoteId)
            .OrderByDescending(vo => vo.ApprovedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return entity?.ToModel();
    }
}
