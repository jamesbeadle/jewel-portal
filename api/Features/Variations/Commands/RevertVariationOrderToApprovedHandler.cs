using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Un-issues a Variation Order: Issued back to Approved, clearing the issued date. Issuing writes
/// no commercial figures (the approval did), so this is purely a record correction. A cancelled VO
/// cannot be revived this way — cancellation reversed the approval's valuation/CVR/budget writes,
/// and re-applying them belongs to a fresh approval, not a status flip.
/// </summary>
public sealed class RevertVariationOrderToApprovedHandler : ICommandHandler<RevertVariationOrderToApproved, VariationOrder>
{
    private readonly JpmsContext context;
    public RevertVariationOrderToApprovedHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrder> HandleAsync(RevertVariationOrderToApproved command, CancellationToken cancellationToken)
    {
        var entity = await context.VariationOrders.FindAsync(new object[] { command.VariationOrderId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Variation order {command.VariationOrderId} not found.");
        if (entity.Status == (int)VariationOrderStatus.Approved) return entity.ToModel();
        if (entity.Status == (int)VariationOrderStatus.Cancelled)
            throw new InvalidOperationException("A cancelled variation order cannot be reverted to Approved — its commercial writes were reversed at cancellation.");

        entity.Status = (int)VariationOrderStatus.Approved;
        entity.IssuedAt = null;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
