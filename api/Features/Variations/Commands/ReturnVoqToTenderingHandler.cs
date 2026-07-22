using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Un-approves a VOQ: deletes the live Variation Order (freeing its V-ref so a future approval can
/// re-mint it), reverses whatever the approval actually wrote, and sets the VOQ back to Tendering.
///
/// This exists for approvals that should never have happened — chiefly seeded history marked
/// Approved when the client never really approved it. It is NOT a substitute for Cancel: a
/// cancelled VO is a real commercial event that stays on the register as an audit fact, while a
/// returned VOQ says "this approval was a data error — the record is still being tendered".
///
/// Reversal is proportionate to what approval wrote, because seeded VOs were written by SQL, not
/// the approve handler:
///   - Valuation line: a single line for the V-ref reverts to a TBC placeholder (recorded, not
///     priced into totals — the seeded-register convention for unapproved variations). Zero-amount
///     claim rows against it are dropped; any claimed value blocks the return.
///   - CVR accrual: the approval's accrual (recognised by its "{ref} — {title}" signature) is
///     deleted if present. Seeded approvals never wrote one, so nothing phantom is offset.
///   - Budget: commitment is released only when the approval accrual proved the approval committed
///     it in the first place.
/// Refused when work orders instruct the VO, when the VO's value has been revised (sort the CVR
/// deltas out first), when a split (multi-line) VO is involved, or when value has been claimed.
/// </summary>
public sealed class ReturnVoqToTenderingHandler : ICommandHandler<ReturnVoqToTendering, VariationOrderQuote>
{
    private readonly JpmsContext context;
    public ReturnVoqToTenderingHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrderQuote> HandleAsync(ReturnVoqToTendering command, CancellationToken cancellationToken)
    {
        var voq = await context.VariationOrderQuotes.FindAsync(new object[] { command.VariationOrderQuoteId }, cancellationToken);
        if (voq is null) throw new InvalidOperationException($"VOQ {command.VariationOrderQuoteId} not found.");
        if (voq.Status != (int)VariationOrderQuoteStatus.Approved)
            throw new InvalidOperationException("Only an approved VOQ can be returned to tendering.");

        var vo = await context.VariationOrders.FirstOrDefaultAsync(
            order => order.VariationOrderQuoteId == voq.VariationOrderQuoteId
                     && order.Status != (int)VariationOrderStatus.Cancelled,
            cancellationToken);

        if (vo is not null)
        {
            // A VO that has been instructed is real committed work — un-approving underneath the
            // subcontractor would falsify the record. The work orders go first, deliberately.
            var instructingOrders = await context.WorkOrders
                .Where(order => order.VariationOrderId == vo.VariationOrderId)
                .Select(order => order.WorkOrderId)
                .ToListAsync(cancellationToken);
            if (instructingOrders.Count > 0)
                throw new InvalidOperationException("Work orders instruct this variation — cancel them before returning it to tendering.");

            // A revised VO has moved the CVR/budget by deltas this reversal doesn't model.
            var revised = await context.QsAccruals.AnyAsync(
                accrual => accrual.ProjectId == vo.ProjectId
                           && accrual.Category == "Variation"
                           && accrual.Description.StartsWith(vo.VariationRef + " — ")
                           && accrual.Description.Contains("(revised"),
                cancellationToken);
            if (revised)
                throw new InvalidOperationException("This variation's value has been revised since approval — its CVR history must be unwound manually.");

            // ---- Valuation Report line(s) for this V-ref ----
            var lines = await context.ValuationLineItems
                .Where(line => line.ProjectId == vo.ProjectId
                               && line.ElementType == (int)ValuationElementType.Variation
                               && line.VariationRef == vo.VariationRef)
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
                    throw new InvalidOperationException("Value has been claimed against this variation — it cannot be returned to tendering.");
                // Zero rows are bookkeeping only; drop them so the placeholder starts clean.
                context.ClaimLines.RemoveRange(lineClaims);

                // Back to the register's "recorded, not priced" convention for unapproved work.
                line.LineType = (int)ValuationLineType.Tbc;
                line.Rate = 0m;
                line.LineAmount = 0m;
                line.Comments = $"Variation order quote {voq.Reference} — returned to tendering";
            }

            // ---- CVR accrual + budget: reverse only what the approval provably wrote ----
            var approvalAccruals = await context.QsAccruals
                .Where(accrual => accrual.ProjectId == vo.ProjectId
                                  && accrual.Category == "Variation"
                                  && accrual.Description == vo.VariationRef + " — " + vo.Title)
                .ToListAsync(cancellationToken);
            if (approvalAccruals.Count > 0)
            {
                context.QsAccruals.RemoveRange(approvalAccruals);

                var budget = await context.CostCodeBudgets.FirstOrDefaultAsync(
                    b => b.ProjectId == vo.ProjectId && b.CostCode == vo.CostCode, cancellationToken);
                if (budget is not null) budget.CommittedAmount -= vo.Value;
            }
            // else: a seeded approval — no accrual was ever written and no budget was committed,
            // so there is nothing to reverse beyond the valuation line above.

            // The approval "never happened": remove the VO so its V-ref is free to re-mint.
            context.VariationOrders.Remove(vo);
        }

        voq.Status = (int)VariationOrderQuoteStatus.Tendering;
        voq.ApprovedAt = null;
        voq.ApprovedByEmail = null;

        await context.SaveChangesAsync(cancellationToken);
        return voq.ToModel();
    }
}
