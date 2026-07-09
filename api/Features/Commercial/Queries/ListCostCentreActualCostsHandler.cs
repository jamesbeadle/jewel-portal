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
        // Xero purchase lines for this project — whole-line allocations plus split
        // shares from XeroCostSplits. Cost-code match is case-insensitive in memory
        // to mirror the summary's OrdinalIgnoreCase grouping.
        var lines = await context.XeroLedgerLines
            .Where(line => line.ProjectId == query.ProjectId
                           && line.AllocationStatus == (int)XeroAllocationStatus.Allocated
                           && line.CostCenterCode != null)
            .ToListAsync(cancellationToken);

        var splitShares = await context.XeroCostSplits
            .Join(context.XeroLedgerLines,
                split => split.XeroLedgerLineId,
                line => line.XeroLedgerLineId,
                (split, line) => new { Split = split, Line = line })
            .Where(joined => joined.Split.ProjectId == query.ProjectId
                             && joined.Line.AllocationStatus == (int)XeroAllocationStatus.Allocated)
            .ToListAsync(cancellationToken);

        var whole = lines
            .Where(line => string.Equals(line.CostCenterCode, query.CostCode, StringComparison.OrdinalIgnoreCase))
            .Select(line => new CostCentreActualCostLine(
                line.XeroLedgerLineId,
                line.Date,
                line.ContactName ?? "",
                line.InvoiceNumber ?? "",
                line.Description ?? "",
                line.Type == "ACCPAYCREDIT" ? -line.Net : line.Net));

        var shares = splitShares
            .Where(joined => string.Equals(joined.Split.CostCenterCode, query.CostCode, StringComparison.OrdinalIgnoreCase))
            .Select(joined => new CostCentreActualCostLine(
                joined.Line.XeroLedgerLineId,
                joined.Line.Date,
                joined.Line.ContactName ?? "",
                joined.Line.InvoiceNumber ?? "",
                joined.Line.Description ?? "",
                joined.Line.Type == "ACCPAYCREDIT" ? -joined.Split.Net : joined.Split.Net,
                IsSplit: true));

        return whole.Concat(shares)
            .OrderByDescending(line => line.Date ?? DateTime.MinValue)
            .ToList();
    }
}
