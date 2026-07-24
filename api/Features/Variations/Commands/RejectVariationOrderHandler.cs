using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Rejects a variation order. From Quoting or Issued this is a plain status move — the client
/// declined and nothing commercial was ever written. From APPROVED it reverses the commercial
/// writes the approval made, so the valuation report only ever shows live, agreed variations:
///   1. removes the Variation line from the Valuation Report (and any claim entries against it),
///   2. records an offsetting QS accrual (omit) on the CVR,
///   3. releases the committed value from the cost-centre budget.
/// The V-ref, once minted, stays on the record as an audit fact — its number is not re-used.
/// </summary>
public sealed class RejectVariationOrderHandler : ICommandHandler<RejectVariationOrder, VariationOrder>
{
    private readonly JpmsContext context;
    public RejectVariationOrderHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrder> HandleAsync(RejectVariationOrder command, CancellationToken cancellationToken)
    {
        var entity = await context.VariationOrders.FindAsync(new object[] { command.VariationOrderId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Variation order {command.VariationOrderId} not found.");
        if (entity.Status == (int)VariationOrderStatus.Rejected) return entity.ToModel();

        var now = DateTimeOffset.UtcNow;
        var wasApproved = entity.Status == (int)VariationOrderStatus.Approved;

        if (wasApproved)
        {
            // 1) Take the Variation line off the Valuation Report, dropping any claim entries so
            //    claims stay reconcilable.
            var lines = await context.ValuationLineItems
                .Where(line => line.ProjectId == entity.ProjectId
                               && line.ElementType == (int)ValuationElementType.Variation
                               && line.VariationRef == entity.VariationRef)
                .ToListAsync(cancellationToken);
            // A build-up approval committed each cost centre its own share; read those off the line
            // amounts now, before the lines are removed, so step 3 can release the same per centre.
            var releaseByCentre = lines
                .GroupBy(line => line.CostCode)
                .ToDictionary(group => group.Key, group => group.Sum(line => line.LineAmount));
            if (lines.Count > 0)
            {
                var lineIds = lines.Select(line => line.ValuationLineItemId).ToList();
                var claimLines = await context.ClaimLines
                    .Where(line => lineIds.Contains(line.ValuationLineItemId))
                    .ToListAsync(cancellationToken);
                context.ClaimLines.RemoveRange(claimLines);
                context.ValuationLineItems.RemoveRange(lines);
            }

            // 2) Offset the CVR accrual recorded at approval.
            context.QsAccruals.Add(new QsAccrualEntity
            {
                QsAccrualId = VariationsIdentifierFactory.NextQsAccrualId(),
                ProjectId = entity.ProjectId,
                Category = "Variation",
                Description = $"{entity.VariationRef} — {entity.Title} (rejected)",
                AddAmount = 0m,
                OmitAmount = entity.Value,
                LiabilityAmount = 0m,
                SignedOffByEmail = entity.ApprovedByEmail ?? "",
                SignedOffAt = now
            });

            // 3) Release the committed budget. A build-up committed each centre its own share, so
            //    release the same per centre; with no report lines (legacy/seeded) fall back to the
            //    whole value against the primary code.
            if (releaseByCentre.Count > 0)
            {
                foreach (var centre in releaseByCentre)
                {
                    var centreBudget = await context.CostCodeBudgets.FirstOrDefaultAsync(
                        b => b.ProjectId == entity.ProjectId && b.CostCode == centre.Key, cancellationToken);
                    if (centreBudget is not null) centreBudget.CommittedAmount -= centre.Value;
                }
            }
            else
            {
                var budget = await context.CostCodeBudgets.FirstOrDefaultAsync(
                    b => b.ProjectId == entity.ProjectId && b.CostCode == entity.CostCode, cancellationToken);
                if (budget is not null) budget.CommittedAmount -= entity.Value;
            }
        }

        entity.Status = (int)VariationOrderStatus.Rejected;
        entity.RejectedAt = now;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
