using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Un-approves a variation order back to Quoting: reverses whatever the approval actually wrote and
/// clears the minted V-ref so a future approval can re-mint it.
///
/// This exists for approvals that should never have happened — chiefly seeded history marked
/// Approved when the client never really approved it. It is NOT a substitute for RejectVariationOrder:
/// a rejected order is a real decision that stays on the register as an audit fact, while a returned
/// order says "this approval was a data error — the record is still being quoted".
///
/// Reversal is proportionate to what approval wrote, because seeded approvals were written by SQL,
/// not the approve handler:
///   - Valuation line: a single line for the V-ref reverts to a TBC placeholder (recorded, not
///     priced into totals — the seeded-register convention for unapproved variations). Zero-amount
///     claim rows against it are dropped; any claimed value blocks the return.
///   - CVR accrual: the approval's accrual (recognised by its "{ref} — {title}" signature) is
///     deleted if present. Seeded approvals never wrote one, so nothing phantom is offset.
///   - Budget: commitment is released only when the approval accrual proved the approval committed
///     it in the first place.
/// Refused when work orders instruct the variation, when its value has been revised (sort the CVR
/// deltas out first), when it is priced as split detail lines, or when value has been claimed.
/// </summary>
public sealed class ReturnVariationOrderToQuotingHandler : ICommandHandler<ReturnVariationOrderToQuoting, VariationOrder>
{
    private readonly JpmsContext context;
    public ReturnVariationOrderToQuotingHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrder> HandleAsync(ReturnVariationOrderToQuoting command, CancellationToken cancellationToken)
    {
        var order = await context.VariationOrders.FindAsync(new object[] { command.VariationOrderId }, cancellationToken);
        if (order is null) throw new InvalidOperationException($"Variation order {command.VariationOrderId} not found.");
        if (order.Status != (int)VariationOrderStatus.Approved)
            throw new InvalidOperationException("Only an approved variation order can be returned to quoting.");

        // A variation that has been instructed is real committed work — un-approving underneath the
        // subcontractor would falsify the record. The work orders go first, deliberately.
        var instructingOrders = await context.WorkOrders
            .Where(wo => wo.VariationOrderId == order.VariationOrderId)
            .Select(wo => wo.WorkOrderId)
            .ToListAsync(cancellationToken);
        if (instructingOrders.Count > 0)
            throw new InvalidOperationException("Work orders instruct this variation — cancel them before returning it to quoting.");

        // A revised variation has moved the CVR/budget by deltas this reversal doesn't model.
        var revised = await context.QsAccruals.AnyAsync(
            accrual => accrual.ProjectId == order.ProjectId
                       && accrual.Category == "Variation"
                       && accrual.Description.StartsWith(order.VariationRef + " — ")
                       && accrual.Description.Contains("(revised"),
            cancellationToken);
        if (revised)
            throw new InvalidOperationException("This variation's value has been revised since approval — its CVR history must be unwound manually.");

        // ---- Valuation Report line(s) for this V-ref ----
        var lines = await context.ValuationLineItems
            .Where(line => line.ProjectId == order.ProjectId
                           && line.ElementType == (int)ValuationElementType.Variation
                           && line.VariationRef == order.VariationRef)
            .ToListAsync(cancellationToken);
        if (lines.Count > 1)
            throw new InvalidOperationException("This variation is priced as split detail lines on the valuation report — revert it manually.");

        if (lines.Count == 1)
        {
            var line = lines[0];
            var lineClaims = await context.ClaimLines
                .Where(claim => claim.ValuationLineItemId == line.ValuationLineItemId)
                .ToListAsync(cancellationToken);
            if (lineClaims.Any(claim => claim.CumulativeClaimed != 0m || claim.PercentComplete != 0m))
                throw new InvalidOperationException("Value has been claimed against this variation — it cannot be returned to quoting.");
            // Zero rows are bookkeeping only; drop them so the placeholder starts clean.
            context.ClaimLines.RemoveRange(lineClaims);

            // Back to the register's "recorded, not priced" convention for unapproved work.
            line.LineType = (int)ValuationLineType.Tbc;
            line.Rate = 0m;
            line.LineAmount = 0m;
            line.Comments = $"Variation order {order.Reference} — returned to quoting";
        }

        // ---- CVR accrual + budget: reverse only what the approval provably wrote ----
        var approvalAccruals = await context.QsAccruals
            .Where(accrual => accrual.ProjectId == order.ProjectId
                              && accrual.Category == "Variation"
                              && accrual.Description == order.VariationRef + " — " + order.Title)
            .ToListAsync(cancellationToken);
        if (approvalAccruals.Count > 0)
        {
            context.QsAccruals.RemoveRange(approvalAccruals);

            var budget = await context.CostCodeBudgets.FirstOrDefaultAsync(
                b => b.ProjectId == order.ProjectId && b.CostCode == order.CostCode, cancellationToken);
            if (budget is not null) budget.CommittedAmount -= order.Value;
        }
        // else: a seeded approval — no accrual was ever written and no budget was committed, so
        // there is nothing to reverse beyond the valuation line above.

        // The approval "never happened": clear the contract-stage data so the V-ref is free to
        // re-mint and the order reads as a clean quoting record again.
        order.Status = (int)VariationOrderStatus.Quoting;
        order.VariationRef = null;
        order.Value = 0m;
        order.CostCode = null;
        order.ApprovedAt = null;
        order.ApprovedByEmail = null;

        await context.SaveChangesAsync(cancellationToken);
        return order.ToModel();
    }
}
