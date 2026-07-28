using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial;

/// <summary>
/// The invoice drives the work order's cost centre. Whenever an allocated Xero purchase
/// line is linked to work orders — and whenever a linked line's centre later moves in
/// allocation — every line of each linked order is recoded wholesale to the invoice's
/// centre, so the order's committed value and the invoice's actual cost always sit in
/// the same centre. A pure recode in the RecodeWorkOrderLine sense: values, quantities
/// and PaidToDate stay put; only where the committed value sits changes. The centre code
/// comes off an allocated line, which allocation has already validated against the
/// master list. Changes are tracked only — the caller owns SaveChanges.
/// </summary>
public static class WorkOrderInvoiceRecoding
{
    public static async Task RecodeOrdersToCentreAsync(
        JpmsContext context,
        IReadOnlyCollection<string> workOrderIds,
        string costCode,
        CancellationToken cancellationToken)
    {
        if (workOrderIds.Count == 0) return;

        var orderLines = await context.WorkOrderLines
            .Where(line => workOrderIds.Contains(line.WorkOrderId))
            .ToListAsync(cancellationToken);

        foreach (var orderLine in orderLines)
        {
            if (!string.Equals(orderLine.CostCode, costCode, StringComparison.OrdinalIgnoreCase))
                orderLine.CostCode = costCode;
        }
    }
}
