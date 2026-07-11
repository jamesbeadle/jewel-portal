using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial;

/// <summary>
/// Spreads a work-order-linked invoice slice across the order's cost centres, pro-rata
/// by the order's line values — so linked actual cost of sales lands on the same
/// centres as the order's committed value, not wherever the invoice happened to be
/// allocated in Xero. Both the financial summary and the per-centre actual-cost detail
/// use this, and they must agree to the penny: the code list is ordered deterministically
/// and the last code absorbs the rounding remainder.
/// </summary>
internal static class WorkOrderCostApportionment
{
    /// <summary>Each order's coded line values, ordered deterministically. Orders whose
    /// lines carry no cost code (or net to zero) are absent — their linked slices stay
    /// on the invoice's own centre because there is nothing to apportion by.</summary>
    public static async Task<Dictionary<string, List<(string CostCode, decimal Total)>>> CodeTotalsByOrderAsync(
        JpmsContext context, string projectId, CancellationToken cancellationToken)
    {
        var rows = await context.WorkOrderLines
            .Join(context.WorkOrders,
                line => line.WorkOrderId,
                order => order.WorkOrderId,
                (line, order) => new { order.ProjectId, line.WorkOrderId, line.CostCode, line.LineTotal })
            .Where(joined => joined.ProjectId == projectId && joined.CostCode != "")
            .GroupBy(joined => new { joined.WorkOrderId, joined.CostCode })
            .Select(group => new { group.Key.WorkOrderId, group.Key.CostCode, Total = group.Sum(row => row.LineTotal) })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.WorkOrderId, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Sum(row => row.Total) != 0m)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(row => (row.CostCode, row.Total))
                    .OrderBy(entry => entry.CostCode, StringComparer.Ordinal)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Splits an amount across the order's codes in proportion to their line
    /// values, rounding each share to pennies with the final code taking the remainder
    /// so the shares always sum exactly to the amount.</summary>
    public static List<(string CostCode, decimal Share)> Apportion(
        decimal amount, IReadOnlyList<(string CostCode, decimal Total)> codeTotals)
    {
        var totalValue = codeTotals.Sum(entry => entry.Total);
        var shares = new List<(string CostCode, decimal Share)>(codeTotals.Count);
        decimal assigned = 0m;
        for (var index = 0; index < codeTotals.Count; index++)
        {
            var share = index == codeTotals.Count - 1
                ? amount - assigned
                : Math.Round(amount * codeTotals[index].Total / totalValue, 2);
            assigned += share;
            shares.Add((codeTotals[index].CostCode, share));
        }
        return shares;
    }
}
