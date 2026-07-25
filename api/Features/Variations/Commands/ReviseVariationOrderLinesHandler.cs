using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Re-states an approved variation's priced lines in one transaction: replaces the Variation lines
/// on the Valuation Report with the new set, moves the CVR by a delta accrual, and adjusts each cost
/// centre's committed budget by its own change. See ReviseVariationOrderLines for the guard rules.
/// </summary>
public sealed class ReviseVariationOrderLinesHandler : ICommandHandler<ReviseVariationOrderLines, VariationOrder>
{
    private readonly JpmsContext context;
    public ReviseVariationOrderLinesHandler(JpmsContext context) { this.context = context; }

    private static ValuationLineType LineTypeFor(decimal quantity, decimal rate) =>
        quantity * rate < 0m ? ValuationLineType.Omit : ValuationLineType.Priced;

    private static decimal AmountOf(VariationLineInput line) =>
        ValuationCalculations.LineAmount(LineTypeFor(line.Quantity, line.Rate), line.Quantity, line.Rate);

    public async Task<VariationOrder> HandleAsync(ReviseVariationOrderLines command, CancellationToken cancellationToken)
    {
        var order = await context.VariationOrders.FindAsync(new object[] { command.VariationOrderId }, cancellationToken);
        if (order is null) throw new InvalidOperationException($"Variation order {command.VariationOrderId} not found.");
        if (order.Status != (int)VariationOrderStatus.Approved)
            throw new InvalidOperationException("Only an approved variation order can have its lines revised — before approval, edit the build-up on the approve panel.");

        var lines = command.Lines ?? Array.Empty<VariationLineInput>();
        if (lines.Count == 0)
            throw new InvalidOperationException("At least one line is required.");
        if (lines.Any(line => string.IsNullOrWhiteSpace(line.CostCode)))
            throw new InvalidOperationException("Every variation line needs a cost centre.");

        var newTotal = lines.Sum(AmountOf);
        if (newTotal == 0m)
            throw new InvalidOperationException("The total can't be zero — enter the agreed values (negative rate for an omit).");

        var variationRef = order.VariationRef ?? throw new InvalidOperationException("This variation has no reference — it may not be approved.");
        var now = DateTimeOffset.UtcNow;

        // Existing report lines for this variation.
        var existing = await context.ValuationLineItems
            .Where(line => line.ProjectId == order.ProjectId
                           && line.ElementType == (int)ValuationElementType.Variation
                           && line.VariationRef == variationRef)
            .ToListAsync(cancellationToken);

        // Refuse if value has been claimed against any of these lines — replacing them would break
        // claim reconciliation. Zero (bookkeeping) claim rows are fine and get cleared with the lines.
        var existingIds = existing.Select(line => line.ValuationLineItemId).ToList();
        var claimLines = existingIds.Count == 0
            ? new List<ClaimLineEntity>()
            : await context.ClaimLines.Where(claim => existingIds.Contains(claim.ValuationLineItemId)).ToListAsync(cancellationToken);
        if (claimLines.Any(claim => claim.CumulativeClaimed != 0m || claim.PercentComplete != 0m))
            throw new InvalidOperationException("Value has been claimed against this variation — sort the claim before editing its lines.");

        var oldTotal = order.Value;
        // Per-centre committed amounts the approval (or a prior revision) wrote — read from the lines,
        // falling back to the whole value against the primary code for a seeded no-line approval.
        var oldPerCentre = existing.Count > 0
            ? existing.GroupBy(line => line.CostCode).ToDictionary(g => g.Key, g => g.Sum(line => line.LineAmount))
            : (string.IsNullOrWhiteSpace(order.CostCode)
                ? new Dictionary<string, decimal>()
                : new Dictionary<string, decimal> { [order.CostCode!] = order.Value });

        // Swap the lines out for the new set.
        context.ClaimLines.RemoveRange(claimLines);
        context.ValuationLineItems.RemoveRange(existing);

        var nextDisplayOrder = (await context.ValuationLineItems
            .Where(line => line.ProjectId == order.ProjectId)
            .MaxAsync(line => (int?)line.DisplayOrder, cancellationToken) ?? 0) + 1;
        foreach (var line in lines)
        {
            var lineType = LineTypeFor(line.Quantity, line.Rate);
            context.ValuationLineItems.Add(new ValuationLineItemEntity
            {
                ValuationLineItemId = VariationsIdentifierFactory.NextValuationLineItemId(),
                ProjectId = order.ProjectId,
                ElementType = (int)ValuationElementType.Variation,
                SectionCode = "",
                SectionName = "",
                VariationRef = variationRef,
                VariationTitle = order.Title,
                LineType = (int)lineType,
                CostCode = line.CostCode.Trim(),
                Description = string.IsNullOrWhiteSpace(line.Description) ? order.Title : line.Description.Trim(),
                Unit = "item",
                Quantity = line.Quantity,
                Rate = line.Rate,
                LineAmount = ValuationCalculations.LineAmount(lineType, line.Quantity, line.Rate),
                Comments = $"Variation order {variationRef} (from {order.Reference})",
                DisplayOrder = nextDisplayOrder++
            });
        }

        // Adjust each cost centre's committed budget by its own change (add for new centres, release
        // for centres that dropped out).
        var newPerCentre = lines
            .GroupBy(line => line.CostCode.Trim())
            .ToDictionary(g => g.Key, g => g.Sum(AmountOf));
        foreach (var centre in oldPerCentre.Keys.Union(newPerCentre.Keys))
        {
            var oldAmt = oldPerCentre.TryGetValue(centre, out var o) ? o : 0m;
            var newAmt = newPerCentre.TryGetValue(centre, out var n) ? n : 0m;
            var delta = newAmt - oldAmt;
            if (delta != 0m) await CommitToBudgetAsync(order.ProjectId, centre, delta, cancellationToken);
        }

        // Move the CVR by the total delta, keeping the revision on the accrual history.
        var totalDelta = newTotal - oldTotal;
        if (totalDelta != 0m)
        {
            context.QsAccruals.Add(new QsAccrualEntity
            {
                QsAccrualId = VariationsIdentifierFactory.NextQsAccrualId(),
                ProjectId = order.ProjectId,
                Category = "Variation",
                Description = $"{variationRef} — {order.Title} (revised {Money(oldTotal)} → {Money(newTotal)})",
                AddAmount = totalDelta > 0m ? totalDelta : 0m,
                OmitAmount = totalDelta < 0m ? -totalDelta : 0m,
                LiabilityAmount = 0m,
                SignedOffByEmail = command.RevisedByEmail,
                SignedOffAt = now
            });
        }

        order.Value = newTotal;
        order.CostCode = lines[0].CostCode.Trim();

        await context.SaveChangesAsync(cancellationToken);
        return order.ToModel();
    }

    private async Task CommitToBudgetAsync(string projectId, string costCode, decimal amount, CancellationToken cancellationToken)
    {
        var budget = await context.CostCodeBudgets.FirstOrDefaultAsync(
            b => b.ProjectId == projectId && b.CostCode == costCode, cancellationToken);
        if (budget is null)
        {
            budget = new CostCodeBudgetEntity
            {
                CostCodeBudgetId = VariationsIdentifierFactory.NextCostCodeBudgetId(),
                ProjectId = projectId,
                CostCode = costCode,
                AllocatedAmount = 0m,
                SpentAmount = 0m,
                CommittedAmount = 0m
            };
            context.CostCodeBudgets.Add(budget);
        }
        budget.CommittedAmount += amount;
    }

    private static string Money(decimal value) =>
        value.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
}
