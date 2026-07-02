using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Cancels a Variation Order and reverses the commercial writes made when it was approved, so the
/// valuation report only ever shows live, agreed variations:
///   1. removes the Variation line from the Valuation Report (and any claim entries against it),
///   2. records an offsetting QS accrual (omit) on the CVR,
///   3. releases the committed value from the cost-centre budget.
/// </summary>
public sealed class CancelVariationOrderHandler : ICommandHandler<CancelVariationOrder, VariationOrder>
{
    private readonly JpmsContext context;
    public CancelVariationOrderHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrder> HandleAsync(CancelVariationOrder command, CancellationToken cancellationToken)
    {
        var entity = await context.VariationOrders.FindAsync(new object[] { command.VariationOrderId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Variation order {command.VariationOrderId} not found.");
        if (entity.Status == (int)VariationOrderStatus.Cancelled) return entity.ToModel();

        var now = DateTimeOffset.UtcNow;

        // 1) Take the Variation line off the Valuation Report, dropping any claim entries so
        //    claims stay reconcilable.
        var lines = await context.ValuationLineItems
            .Where(line => line.ProjectId == entity.ProjectId
                           && line.ElementType == (int)ValuationElementType.Variation
                           && line.VariationRef == entity.VariationRef)
            .ToListAsync(cancellationToken);
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
            Description = $"{entity.VariationRef} — {entity.Title} (cancelled)",
            AddAmount = 0m,
            OmitAmount = entity.Value,
            LiabilityAmount = 0m,
            SignedOffByEmail = entity.ApprovedByEmail,
            SignedOffAt = now
        });

        // 3) Release the committed budget.
        var budget = await context.CostCodeBudgets.FirstOrDefaultAsync(
            b => b.ProjectId == entity.ProjectId && b.CostCode == entity.CostCode, cancellationToken);
        if (budget is not null) budget.CommittedAmount -= entity.Value;

        entity.Status = (int)VariationOrderStatus.Cancelled;
        entity.CancelledAt = now;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
