using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class IssueVariationOrderHandler : ICommandHandler<IssueVariationOrder, VariationOrder>
{
    private readonly JpmsContext context;
    public IssueVariationOrderHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrder> HandleAsync(IssueVariationOrder command, CancellationToken cancellationToken)
    {
        var entity = await context.VariationOrders.FindAsync(new object[] { command.VariationOrderId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Variation order {command.VariationOrderId} not found.");
        if (entity.Status == (int)VariationOrderStatus.Cancelled)
            throw new InvalidOperationException("A cancelled variation order cannot be issued.");

        entity.Status = (int)VariationOrderStatus.Issued;
        entity.IssuedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
