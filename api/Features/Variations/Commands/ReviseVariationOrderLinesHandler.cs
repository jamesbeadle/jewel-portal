using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Commercial;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Variations;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Re-states an approved variation's priced lines in one transaction: re-prices the Variation lines
/// on the Valuation Report, moves the CVR by a delta accrual, and adjusts each cost centre's
/// committed budget by its own change. See ReviseVariationOrderLines for the guard rules.
///
/// Lines are matched to the report by id (VariationLineRevision), never by position, so a line the
/// user re-prices keeps its ValuationLineItemId and every claim entry standing against it stays
/// attached. That is what lets a variation be edited after it has been claimed: settled claims
/// (Preapproved / Confirmed) keep the money they were certified at, the snapshots frozen from them
/// were value copies that were never going to move, and only the claim in progress is re-based onto
/// the new figures (DraftClaimRebase).
///
/// A line can only be dropped altogether while nothing settled has been claimed against it: deleting
/// its claim entries would rewrite a valuation the client has already been sent. Re-price it to
/// nothing instead — a negative rate omits the work and leaves the claim a row to reconcile against.
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

        var existing = await context.ValuationLineItems
            .Where(line => line.ProjectId == order.ProjectId
                           && line.ElementType == (int)ValuationElementType.Variation
                           && line.VariationRef == variationRef)
            .OrderBy(line => line.DisplayOrder).ThenBy(line => line.ValuationLineItemId)
            .ToListAsync(cancellationToken);
        var rowsById = existing.ToDictionary(line => line.ValuationLineItemId);

        var existingIds = existing.Select(line => line.ValuationLineItemId).ToList();

        // A claim whose totals are already locked must not have these lines move underneath it.
        await DraftClaimRebase.GuardNoClaimInFlightAsync(context, order.ProjectId, existingIds, cancellationToken);

        // Every claim entry standing against those lines, carrying the status of the claim it
        // belongs to: settled entries are history, and history is what the guard below protects.
        var claimEntries = await (
                from claimLine in context.ClaimLines
                join claim in context.ValuationClaims on claimLine.ValuationClaimId equals claim.ValuationClaimId
                where existingIds.Contains(claimLine.ValuationLineItemId)
                select new { Entry = claimLine, claim.Status })
            .ToListAsync(cancellationToken);

        var oldTotal = order.Value;
        // Per-centre committed amounts the approval (or a prior revision) wrote — read from the
        // lines as they stand now, before any of them is re-priced (the Sum runs here, and that
        // eagerness is load-bearing), falling back to the whole value against the primary code for
        // a seeded no-line approval.
        var oldPerCentre = existing.Count > 0
            ? existing.GroupBy(line => line.CostCode).ToDictionary(g => g.Key, g => g.Sum(line => line.LineAmount))
            : (string.IsNullOrWhiteSpace(order.CostCode)
                ? new Dictionary<string, decimal>()
                : new Dictionary<string, decimal> { [order.CostCode!] = order.Value });

        var revision = VariationLineRevision.Plan(existingIds, lines);

        // Value certified on a settled valuation can't be dropped off the report with its line.
        var settledLineIds = claimEntries
            .Where(row => row.Status != (int)ValuationClaimStatus.Draft
                          && (row.Entry.CumulativeClaimed != 0m || row.Entry.PercentComplete != 0m))
            .Select(row => row.Entry.ValuationLineItemId)
            .ToHashSet();
        var blocked = revision.Dropped.FirstOrDefault(settledLineIds.Contains);
        if (blocked is not null)
            throw new InvalidOperationException(
                $"{variationRef}'s {rowsById[blocked].CostCode} line has value claimed on a settled valuation, so it can't be removed. Re-price it instead — a negative rate omits the work without breaking the claim.");

        // Re-price what is already there. Lines whose money actually moved are collected so the
        // claim in progress can follow them.
        var moved = new List<ValuationLineItemEntity>();
        foreach (var (lineItemId, input) in revision.Repriced)
        {
            var row = rowsById[lineItemId];
            var lineType = LineTypeFor(input.Quantity, input.Rate);
            var amount = ValuationCalculations.LineAmount(lineType, input.Quantity, input.Rate);
            var amountMoved = row.LineAmount != amount;

            row.VariationTitle = order.Title;
            row.LineType = (int)lineType;
            row.CostCode = input.CostCode.Trim();
            row.Description = string.IsNullOrWhiteSpace(input.Description) ? order.Title : input.Description.Trim();
            row.Unit = "item";
            row.Quantity = input.Quantity;
            row.Rate = input.Rate;
            row.LineAmount = amount;
            row.Comments = $"Variation order {variationRef} (from {order.Reference})";

            if (amountMoved) moved.Add(row);
        }

        // Then append what the revision added.
        var nextDisplayOrder = (await context.ValuationLineItems
            .Where(line => line.ProjectId == order.ProjectId)
            .MaxAsync(line => (int?)line.DisplayOrder, cancellationToken) ?? 0) + 1;
        foreach (var input in revision.Added)
        {
            var lineType = LineTypeFor(input.Quantity, input.Rate);
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
                CostCode = input.CostCode.Trim(),
                Description = string.IsNullOrWhiteSpace(input.Description) ? order.Title : input.Description.Trim(),
                Unit = "item",
                Quantity = input.Quantity,
                Rate = input.Rate,
                LineAmount = ValuationCalculations.LineAmount(lineType, input.Quantity, input.Rate),
                Comments = $"Variation order {variationRef} (from {order.Reference})",
                DisplayOrder = nextDisplayOrder++
            });
        }

        if (revision.Dropped.Count > 0)
        {
            // Only the claim still being built loses its entries with the line. A settled entry
            // here can only be a £0 bookkeeping row (the guard refused anything else) and it stays
            // put: a closed period is not ours to edit, even by a row that carries no money.
            var droppedIds = revision.Dropped.ToHashSet();
            context.ClaimLines.RemoveRange(claimEntries
                .Where(row => row.Status == (int)ValuationClaimStatus.Draft
                              && droppedIds.Contains(row.Entry.ValuationLineItemId))
                .Select(row => row.Entry));
            context.ValuationLineItems.RemoveRange(revision.Dropped.Select(id => rowsById[id]));
        }

        await DraftClaimRebase.ApplyAsync(context, moved, cancellationToken);

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
