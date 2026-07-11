using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
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

        // Each line's work-order slices, so the modal can show and edit the split.
        var linksByLine = (await context.XeroLineWorkOrderLinks
                .Where(link => link.ProjectId == query.ProjectId)
                .ToListAsync(cancellationToken))
            .GroupBy(link => link.XeroLedgerLineId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key,
                group => (IReadOnlyList<XeroWorkOrderLinkSlice>)group
                    .Select(link => new XeroWorkOrderLinkSlice(link.WorkOrderId, link.Amount))
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);

        var whole = lines
            .Where(line => string.Equals(line.CostCenterCode, query.CostCode, StringComparison.OrdinalIgnoreCase))
            .Select(line => new CostCentreActualCostLine(
                line.XeroLedgerLineId,
                line.Date,
                line.ContactName ?? "",
                line.InvoiceNumber ?? "",
                line.Description ?? "",
                line.Type == "ACCPAYCREDIT" ? -line.Net : line.Net,
                IsSplit: false,
                linksByLine.TryGetValue(line.XeroLedgerLineId, out var links) ? links : Array.Empty<XeroWorkOrderLinkSlice>()));

        var shares = splitShares
            .Where(joined => string.Equals(joined.Split.CostCenterCode, query.CostCode, StringComparison.OrdinalIgnoreCase))
            .Select(joined => new CostCentreActualCostLine(
                joined.Line.XeroLedgerLineId,
                joined.Line.Date,
                joined.Line.ContactName ?? "",
                joined.Line.InvoiceNumber ?? "",
                joined.Line.Description ?? "",
                joined.Line.Type == "ACCPAYCREDIT" ? -joined.Split.Net : joined.Split.Net,
                IsSplit: true,
                Array.Empty<XeroWorkOrderLinkSlice>())); // centre-split lines can't carry links

        // The ± adjustment rows behind the summary's work-order re-attribution: each linked
        // slice leaves the invoice's Xero centre (a negative row there) and lands on the
        // order's centres pro-rata (positive rows), using the exact same apportionment the
        // financial summary applies — so this centre's rows total to its column figure.
        var codeTotalsByOrder = await WorkOrderCostApportionment.CodeTotalsByOrderAsync(context, query.ProjectId, cancellationToken);
        var orderNumbers = (await context.WorkOrders
                .Where(order => order.ProjectId == query.ProjectId)
                .Select(order => new { order.WorkOrderId, order.Number })
                .ToListAsync(cancellationToken))
            .ToDictionary(order => order.WorkOrderId, order => order.Number, StringComparer.OrdinalIgnoreCase);

        var attributions = new List<CostCentreActualCostLine>();
        var linesById = lines.ToDictionary(line => line.XeroLedgerLineId, StringComparer.OrdinalIgnoreCase);
        foreach (var lineLinks in linksByLine)
        {
            if (!linesById.TryGetValue(lineLinks.Key, out var sourceLine) || sourceLine.CostCenterCode is null) continue;
            foreach (var slice in lineLinks.Value)
            {
                if (!codeTotalsByOrder.TryGetValue(slice.WorkOrderId, out var codeTotals)) continue; // stays on the invoice's centre
                var reference = orderNumbers.TryGetValue(slice.WorkOrderId, out var number) ? $"WO-{number:0000}" : "WO";

                if (string.Equals(sourceLine.CostCenterCode, query.CostCode, StringComparison.OrdinalIgnoreCase))
                    attributions.Add(AttributionRow(sourceLine, -slice.Amount, reference, sourceLine.CostCenterCode));

                foreach (var (costCode, share) in WorkOrderCostApportionment.Apportion(slice.Amount, codeTotals))
                    if (share != 0m && string.Equals(costCode, query.CostCode, StringComparison.OrdinalIgnoreCase))
                        attributions.Add(AttributionRow(sourceLine, share, reference, sourceLine.CostCenterCode));
            }
        }

        return whole.Concat(shares)
            .OrderByDescending(line => line.Date ?? DateTime.MinValue)
            .Concat(attributions.OrderByDescending(line => line.Date ?? DateTime.MinValue))
            .ToList();
    }

    private static CostCentreActualCostLine AttributionRow(
        XeroLedgerLineEntity sourceLine, decimal amount, string workOrderReference, string sourceCostCode) =>
        new($"{sourceLine.XeroLedgerLineId}:{workOrderReference}:{Math.Sign(amount)}",
            sourceLine.Date,
            sourceLine.ContactName ?? "",
            sourceLine.InvoiceNumber ?? "",
            sourceLine.Description ?? "",
            amount,
            IsSplit: false,
            Array.Empty<XeroWorkOrderLinkSlice>(),
            ViaWorkOrderRef: workOrderReference,
            SourceCostCode: sourceCostCode);
}
