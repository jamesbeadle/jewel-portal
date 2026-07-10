using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListProjectCostOfSalesLinesHandler
    : IQueryHandler<ListProjectCostOfSalesLines, IReadOnlyList<ProjectCostOfSalesLine>>
{
    private readonly JpmsContext context;

    public ListProjectCostOfSalesLinesHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ProjectCostOfSalesLine>> HandleAsync(
        ListProjectCostOfSalesLines query, CancellationToken cancellationToken)
    {
        // Same population as the financial summary's actual-cost figure, project-wide:
        // whole-line allocations plus split shares from XeroCostSplits.
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

        var whole = lines.Select(line => new ProjectCostOfSalesLine(
            line.XeroLedgerLineId,
            line.Date,
            line.ContactName ?? "",
            line.InvoiceNumber ?? "",
            line.Description ?? "",
            line.CostCenterCode ?? "",
            line.Type == "ACCPAYCREDIT" ? -line.Net : line.Net,
            IsSplit: false,
            line.LinkedWorkOrderId));

        var shares = splitShares.Select(joined => new ProjectCostOfSalesLine(
            joined.Line.XeroLedgerLineId,
            joined.Line.Date,
            joined.Line.ContactName ?? "",
            joined.Line.InvoiceNumber ?? "",
            joined.Line.Description ?? "",
            joined.Split.CostCenterCode,
            joined.Line.Type == "ACCPAYCREDIT" ? -joined.Split.Net : joined.Split.Net,
            IsSplit: true,
            joined.Line.LinkedWorkOrderId));

        return whole.Concat(shares)
            .OrderByDescending(line => line.Date ?? DateTime.MinValue)
            .ToList();
    }
}
