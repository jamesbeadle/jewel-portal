using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

/// <summary>
/// Which of an order's cost centres have no priced line on the project's valuation report —
/// no contract or approved-variation sale to set the committed cost against. Declined and TBC
/// lines don't count as cover: they are recorded but never priced into any total, so they are
/// not money the order could be claimed against.
/// </summary>
public static class UncoveredCostCentres
{
    public static async Task<IReadOnlyList<string>> FindAsync(
        JpmsContext context,
        string projectId,
        IEnumerable<string> orderCostCodes,
        CancellationToken cancellationToken)
    {
        var pricedCodes = await context.ValuationLineItems
            .Where(line => line.ProjectId == projectId)
            .Where(line => line.LineType != (int)ValuationLineType.Declined)
            .Where(line => line.LineType != (int)ValuationLineType.Tbc)
            .Select(line => line.CostCode)
            .ToListAsync(cancellationToken);
        var pricedSet = new HashSet<string>(pricedCodes, StringComparer.OrdinalIgnoreCase);
        return orderCostCodes
            .Where(code => !pricedSet.Contains(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
