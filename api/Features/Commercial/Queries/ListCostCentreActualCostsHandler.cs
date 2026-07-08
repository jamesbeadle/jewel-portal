using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListCostCentreActualCostsHandler : IQueryHandler<ListCostCentreActualCosts, IReadOnlyList<CostCentreActualCostLine>>
{
    private readonly JpmsContext context;

    public ListCostCentreActualCostsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<CostCentreActualCostLine>> HandleAsync(ListCostCentreActualCosts query, CancellationToken cancellationToken)
    {
        // Same population as the financial summary's actual-cost figure: allocated
        // Xero purchase lines for this project. Cost-code match is case-insensitive
        // in memory to mirror the summary's OrdinalIgnoreCase grouping.
        var lines = await context.XeroLedgerLines
            .Where(line => line.ProjectId == query.ProjectId
                           && line.AllocationStatus == (int)XeroAllocationStatus.Allocated
                           && line.CostCenterCode != null)
            .ToListAsync(cancellationToken);

        return lines
            .Where(line => string.Equals(line.CostCenterCode, query.CostCode, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(line => line.Date ?? DateTime.MinValue)
            .Select(line => new CostCentreActualCostLine(
                line.XeroLedgerLineId,
                line.Date,
                line.ContactName ?? "",
                line.InvoiceNumber ?? "",
                line.Description ?? "",
                line.Type == "ACCPAYCREDIT" ? -line.Net : line.Net))
            .ToList();
    }
}
