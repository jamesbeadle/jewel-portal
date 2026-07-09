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

        var salesByCode = sales.ToDictionary(s => s.CostCode, s => s.Amount, StringComparer.OrdinalIgnoreCase);
        var actualByCode = actuals.ToDictionary(a => a.CostCode, a => a.Amount, StringComparer.OrdinalIgnoreCase);
        foreach (var splitActual in splitActuals)
            actualByCode[splitActual.CostCode] = actualByCode.TryGetValue(splitActual.CostCode, out var existing)
                ? existing + splitActual.Amount
                : splitActual.Amount;

        return salesByCode.Keys.Union(actualByCode.Keys, StringComparer.OrdinalIgnoreCase)
            .Select(code =>
            {
                var budgetedSales = salesByCode.TryGetValue(code, out var salesAmount) ? salesAmount : 0m;
                var claimedAmount = claimedByCode.TryGetValue(code, out var claimedValue) ? claimedValue : 0m;
                var actualCost = actualByCode.TryGetValue(code, out var actual) ? actual : 0m;

                var budgetedCost = Math.Round(budgetedSales * FinancialSummaryAssumptions.CostFactor, 2);
                var completionPercent = budgetedSales == 0m ? 0m : Math.Round(claimedAmount / budgetedSales * 100m, 1);
                var expectedActualCost = Math.Round(budgetedCost * completionPercent / 100m, 2);

                return new ProjectFinancialSummaryRow(
                    code,
                    budgetedSales,
                    budgetedCost,
                    completionPercent,
                    expectedActualCost,
                    actualCost,
                    expectedActualCost - actualCost);
            })
            .ToList();
    }
}
