using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class GetProjectFinancialSummaryHandler : IQueryHandler<GetProjectFinancialSummary, IReadOnlyList<ProjectFinancialSummaryRow>>
{
    private readonly JpmsContext context;

    public GetProjectFinancialSummaryHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ProjectFinancialSummaryRow>> HandleAsync(GetProjectFinancialSummary query, CancellationToken cancellationToken)
    {
        // Budgeted sales: every counting valuation line (declined / TBC excluded —
        // mirrors ValuationLineItem.CountsTowardTotals). Omit lines carry negative
        // amounts and net off naturally; variation lines carry cost codes and count too.
        var sales = await context.ValuationLineItems
            .Where(line => line.ProjectId == query.ProjectId
                           && line.LineType != (int)ValuationLineType.Declined
                           && line.LineType != (int)ValuationLineType.Tbc
                           && line.CostCode != "")
            .GroupBy(line => line.CostCode)
            .Select(group => new { CostCode = group.Key, Amount = group.Sum(line => line.LineAmount) })
            .ToListAsync(cancellationToken);

        // Completion: the latest claim's cumulative claimed value per cost centre
        // (any status — reflects current site progress even while a claim is in
        // draft). Completion % = claimed / budgeted sales, i.e. amount-weighted
        // across the centre's lines; lines not yet claimed against count as 0%.
        var latestClaimId = await context.ValuationClaims
            .Where(claim => claim.ProjectId == query.ProjectId)
            .OrderByDescending(claim => claim.ClaimNumber)
            .Select(claim => (string?)claim.ValuationClaimId)
            .FirstOrDefaultAsync(cancellationToken);

        var claimedByCode = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        if (latestClaimId is not null)
        {
            var claimed = await context.ClaimLines
                .Where(claimLine => claimLine.ValuationClaimId == latestClaimId)
                .Join(context.ValuationLineItems,
                    claimLine => claimLine.ValuationLineItemId,
                    line => line.ValuationLineItemId,
                    (claimLine, line) => new { line.CostCode, line.LineType, claimLine.CumulativeClaimed })
                .Where(joined => joined.LineType != (int)ValuationLineType.Declined
                                 && joined.LineType != (int)ValuationLineType.Tbc
                                 && joined.CostCode != "")
                .GroupBy(joined => joined.CostCode)
                .Select(group => new { CostCode = group.Key, Amount = group.Sum(joined => joined.CumulativeClaimed) })
                .ToListAsync(cancellationToken);
            foreach (var entry in claimed) claimedByCode[entry.CostCode] = entry.Amount;
        }

        // Actuals: Xero purchase lines allocated to this project. Net is stored
        // positive; supplier credit notes (ACCPAYCREDIT) subtract. Whole-line
        // allocations carry their project + centre on the line; split lines carry
        // theirs in XeroCostSplits (each row a share of the line's net, with its
        // own project), so both populations sum into the same per-centre totals.
        var actuals = await context.XeroLedgerLines
            .Where(line => line.ProjectId == query.ProjectId
                           && line.AllocationStatus == (int)XeroAllocationStatus.Allocated
                           && line.CostCenterCode != null)
            .GroupBy(line => line.CostCenterCode!)
            .Select(group => new
            {
                CostCode = group.Key,
                Amount = group.Sum(line => line.Type == "ACCPAYCREDIT" ? -line.Net : line.Net)
            })
            .ToListAsync(cancellationToken);

        var splitActuals = await context.XeroCostSplits
            .Join(context.XeroLedgerLines,
                split => split.XeroLedgerLineId,
                line => line.XeroLedgerLineId,
                (split, line) => new { split.CostCenterCode, split.Net, split.ProjectId, line.AllocationStatus, line.Type })
            .Where(joined => joined.ProjectId == query.ProjectId
                             && joined.AllocationStatus == (int)XeroAllocationStatus.Allocated)
            .GroupBy(joined => joined.CostCenterCode)
            .Select(group => new
            {
                CostCode = group.Key,
                Amount = group.Sum(joined => joined.Type == "ACCPAYCREDIT" ? -joined.Net : joined.Net)
            })
            .ToListAsync(cancellationToken);

        // Work-order-linked spend per centre: each slice in XeroLineWorkOrderLinks pays an
        // order from a whole-line allocation (links never exist on centre-split lines).
        // Non-WO cost of sales per centre = total actual spend less these linked slices —
        // a partially split bill only counts its unallocated remainder.
        var linkedActuals = await context.XeroLineWorkOrderLinks
            .Where(link => link.ProjectId == query.ProjectId)
            .Join(context.XeroLedgerLines,
                link => link.XeroLedgerLineId,
                line => line.XeroLedgerLineId,
                (link, line) => new { line.CostCenterCode, link.Amount })
            .Where(joined => joined.CostCenterCode != null)
            .GroupBy(joined => joined.CostCenterCode!)
            .Select(group => new { CostCode = group.Key, Amount = group.Sum(joined => joined.Amount) })
            .ToListAsync(cancellationToken);

        // Cost-side state: completion % and the finalisation lock, set inline on the
        // Financials tab, one row per cost centre. Centres never edited default to 0% / unlocked.
        var costProgress = await context.CostCentreCostProgress
            .Where(progress => progress.ProjectId == query.ProjectId)
            .Select(progress => new { progress.CostCode, progress.CostCompletionPercent, progress.IsFinalised })
            .ToListAsync(cancellationToken);
        var costProgressByCode = new Dictionary<string, (decimal Percent, bool IsFinalised)>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in costProgress) costProgressByCode[entry.CostCode] = (entry.CostCompletionPercent, entry.IsFinalised);

        var salesByCode = sales.ToDictionary(s => s.CostCode, s => s.Amount, StringComparer.OrdinalIgnoreCase);
        var actualByCode = actuals.ToDictionary(a => a.CostCode, a => a.Amount, StringComparer.OrdinalIgnoreCase);
        foreach (var splitActual in splitActuals)
            actualByCode[splitActual.CostCode] = actualByCode.TryGetValue(splitActual.CostCode, out var existing)
                ? existing + splitActual.Amount
                : splitActual.Amount;

        // Non-WO = actual spend less the work-order-linked slices, per centre.
        var nonWoActualByCode = new Dictionary<string, decimal>(actualByCode, StringComparer.OrdinalIgnoreCase);
        foreach (var linked in linkedActuals)
            nonWoActualByCode[linked.CostCode] = nonWoActualByCode.TryGetValue(linked.CostCode, out var actualTotal)
                ? actualTotal - linked.Amount
                : -linked.Amount;

        return salesByCode.Keys.Union(actualByCode.Keys, StringComparer.OrdinalIgnoreCase)
            .Union(costProgressByCode.Keys, StringComparer.OrdinalIgnoreCase)
            .Select(code =>
            {
                var budgetedSales = salesByCode.TryGetValue(code, out var salesAmount) ? salesAmount : 0m;
                var claimedAmount = claimedByCode.TryGetValue(code, out var claimedValue) ? claimedValue : 0m;
                var actualCost = actualByCode.TryGetValue(code, out var actual) ? actual : 0m;

                var budgetedCost = Math.Round(budgetedSales * FinancialSummaryAssumptions.CostFactor, 2);
                var completionPercent = budgetedSales == 0m ? 0m : Math.Round(claimedAmount / budgetedSales * 100m, 1);
                var expectedActualCost = Math.Round(budgetedCost * completionPercent / 100m, 2);

                var costState = costProgressByCode.TryGetValue(code, out var state) ? state : (Percent: 0m, IsFinalised: false);
                var nonWoActualCost = nonWoActualByCode.TryGetValue(code, out var nonWo) ? nonWo : 0m;

                return new ProjectFinancialSummaryRow(
                    code,
                    budgetedSales,
                    budgetedCost,
                    completionPercent,
                    expectedActualCost,
                    actualCost,
                    expectedActualCost - actualCost,
                    claimedAmount,
                    costState.Percent,
                    nonWoActualCost,
                    costState.IsFinalised);
            })
            .ToList();
    }
}
