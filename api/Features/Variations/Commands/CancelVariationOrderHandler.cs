using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class CancelVariationOrderHandler : ICommandHandler<CancelVariationOrder, VariationOrder>
{
    private readonly JpmsContext context;
    public CancelVariationOrderHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrder> HandleAsync(CancelVariationOrder command, CancellationToken cancellationToken)
    {
        var entity = await context.VariationOrders.FindAsync(new object[] { command.VariationOrderId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Variation order {command.VariationOrderId} not found.");

        entity.Status = (int)VariationOrderStatus.Cancelled;
        entity.CancelledAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
