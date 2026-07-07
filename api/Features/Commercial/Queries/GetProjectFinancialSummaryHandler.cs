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
        // Budget: every counting valuation line (declined / TBC excluded — mirrors
        // ValuationLineItem.CountsTowardTotals). Omit lines carry negative amounts
        // and net off naturally; variation lines carry cost codes and count too.
        var budgets = await context.ValuationLineItems
            .Where(line => line.ProjectId == query.ProjectId
                           && line.LineType != (int)ValuationLineType.Declined
                           && line.LineType != (int)ValuationLineType.Tbc
                           && line.CostCode != "")
            .GroupBy(line => line.CostCode)
            .Select(group => new { CostCode = group.Key, Amount = group.Sum(line => line.LineAmount) })
            .ToListAsync(cancellationToken);

        // Actuals: Xero purchase lines allocated to this project. Net is stored
        // positive; supplier credit notes (ACCPAYCREDIT) subtract.
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

        var budgetByCode = budgets.ToDictionary(b => b.CostCode, b => b.Amount, StringComparer.OrdinalIgnoreCase);
        var actualByCode = actuals.ToDictionary(a => a.CostCode, a => a.Amount, StringComparer.OrdinalIgnoreCase);

        return budgetByCode.Keys.Union(actualByCode.Keys, StringComparer.OrdinalIgnoreCase)
            .Select(code => new ProjectFinancialSummaryRow(
                code,
                budgetByCode.TryGetValue(code, out var budget) ? budget : 0m,
                actualByCode.TryGetValue(code, out var actual) ? actual : 0m))
            .ToList();
    }
}
