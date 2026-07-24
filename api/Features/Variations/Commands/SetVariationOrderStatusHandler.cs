using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Moves a variation order between the side-effect-free stages — Quoting, Issued and Awaiting AI. Entering
/// Issued stamps IssuedAt ("sent to the client"); moving back to Quoting clears it. The two
/// commercial transitions keep their own commands: Approved is only reached through
/// ApproveVariationOrder (which writes the figures), Rejected through RejectVariationOrder (which
/// reverses them from an approved order), and an approved order is un-approved only through
/// ReturnVariationOrderToQuoting. So this refuses Approved and Rejected targets, and refuses to
/// move an already-approved order (its writes must be unwound deliberately).
/// </summary>
public sealed class SetVariationOrderStatusHandler : ICommandHandler<SetVariationOrderStatus, VariationOrder>
{
    private readonly JpmsContext context;
    public SetVariationOrderStatusHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrder> HandleAsync(SetVariationOrderStatus command, CancellationToken cancellationToken)
    {
        var order = await context.VariationOrders.FindAsync(new object[] { command.VariationOrderId }, cancellationToken);
        if (order is null) throw new InvalidOperationException($"Variation order {command.VariationOrderId} not found.");

        if (order.Status == (int)command.Status) return order.ToModel();

        if (command.Status == VariationOrderStatus.Approved)
            throw new InvalidOperationException("Approving a variation order writes the contract figures — use the approve flow.");
        if (command.Status == VariationOrderStatus.Rejected)
            throw new InvalidOperationException("Rejecting a variation order has its own flow — use reject.");

        if (order.Status == (int)VariationOrderStatus.Approved)
            throw new InvalidOperationException("An approved variation order can only be un-approved by returning it to quoting, which reverses the approval's commercial writes.");
        if (order.Status == (int)VariationOrderStatus.Rejected)
            throw new InvalidOperationException("A rejected variation order cannot be moved back to quoting or issued directly — re-approve or leave it as the audit record.");

        // Move between the side-effect-free stages (Quoting, Issued, Awaiting AI). Entering Issued
        // stamps the client-issue date; returning to Quoting clears it; entering Awaiting AI leaves
        // it untouched (a post-issue waiting stage, so the issued date still stands).
        order.Status = (int)command.Status;
        if (command.Status == VariationOrderStatus.Issued)
            order.IssuedAt = DateTimeOffset.UtcNow;
        else if (command.Status == VariationOrderStatus.Quoting)
            order.IssuedAt = null;

        await context.SaveChangesAsync(cancellationToken);
        return order.ToModel();
    }
}
