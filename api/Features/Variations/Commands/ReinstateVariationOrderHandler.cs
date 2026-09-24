using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Reinstates a rejected variation order: back to Issued when it had been issued, else Quoting,
/// with the rejection date cleared.
///
/// A variation rejected from Approved had its valuation lines removed and its budget released by
/// the rejection, which also wrote an omit offsetting the approval's CVR accrual. It comes back
/// UNAPPROVED — the reinstated record reads as if the approval never happened, exactly as a return
/// to quoting leaves it: the accruals under its V-ref (the approval, any revision, the rejection's
/// omit) leave the CVR together, and the V-ref is cleared so a re-approval re-mints it. Leaving the
/// pair behind would let a later return to quoting delete the approval half and strand the omit.
/// </summary>
public sealed class ReinstateVariationOrderHandler : ICommandHandler<ReinstateVariationOrder, VariationOrder>
{
    private const string VariationAccrualCategory = "Variation";

    private readonly JpmsContext context;
    public ReinstateVariationOrderHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrder> HandleAsync(ReinstateVariationOrder command, CancellationToken cancellationToken)
    {
        var order = await context.VariationOrders.FindAsync(new object[] { command.VariationOrderId }, cancellationToken);
        if (order is null) throw new InvalidOperationException($"Variation order {command.VariationOrderId} not found.");
        if (order.Status != (int)VariationOrderStatus.Rejected)
            throw new InvalidOperationException("Only a rejected variation order can be reinstated.");

        var wasApproved = !string.IsNullOrWhiteSpace(order.VariationRef);
        if (wasApproved) await ClearApprovalAsync(order, cancellationToken);

        var wasIssued = order.IssuedAt is not null;
        order.Status = wasIssued ? (int)VariationOrderStatus.Issued : (int)VariationOrderStatus.Quoting;
        order.RejectedAt = null;

        await context.SaveChangesAsync(cancellationToken);
        return order.ToModel();
    }

    /// <summary>The " — " separator keeps V7's accruals clear of V70's, the same match the
    /// return to quoting makes.</summary>
    private async Task ClearApprovalAsync(VariationOrderEntity order, CancellationToken cancellationToken)
    {
        var accrualPrefix = order.VariationRef + " — ";
        var approvalAccruals = await context.QsAccruals
            .Where(accrual => accrual.ProjectId == order.ProjectId
                              && accrual.Category == VariationAccrualCategory
                              && accrual.Description.StartsWith(accrualPrefix))
            .ToListAsync(cancellationToken);
        context.QsAccruals.RemoveRange(approvalAccruals);

        order.VariationRef = null;
        order.Value = 0m;
        order.CostCode = null;
        order.ApprovedAt = null;
        order.ApprovedByEmail = null;
    }
}
