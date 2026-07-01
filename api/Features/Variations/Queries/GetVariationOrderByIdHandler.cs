using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Variations.Queries;

public sealed class GetVariationOrderByIdHandler : IQueryHandler<GetVariationOrderById, VariationOrder?>
{
    private readonly JpmsContext context;
    public GetVariationOrderByIdHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrder?> HandleAsync(GetVariationOrderById query, CancellationToken cancellationToken)
    {
        var entity = await context.VariationOrders.FindAsync(new object[] { query.VariationOrderId }, cancellationToken);
        return entity?.ToModel();
    }
}
