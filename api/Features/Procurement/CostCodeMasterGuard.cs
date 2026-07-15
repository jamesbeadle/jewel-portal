using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement;

// Shared guard: every supplied cost code must be a code in the cost-code master list
// (CostCenterEntity.Code). Mirrors the manual work order rule so no flow — tendered line items
// included — can scatter committed value onto codes the Financials tab treats as legacy.
internal static class CostCodeMasterGuard
{
    public static async Task EnsureCostCodesInMasterAsync(
        this JpmsContext context, IEnumerable<string> costCodes, CancellationToken cancellationToken)
    {
        var supplied = costCodes.Select(code => (code ?? "").Trim()).ToList();
        if (supplied.Count == 0) return;
        if (supplied.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Each line item needs a cost code from the cost-code master.");

        var masterCodes = await context.CostCenters
            .Select(centre => centre.Code)
            .ToListAsync(cancellationToken);
        var masterSet = new HashSet<string>(masterCodes, StringComparer.OrdinalIgnoreCase);
        var unknown = supplied.FirstOrDefault(code => !masterSet.Contains(code));
        if (unknown is not null)
            throw new InvalidOperationException($"Cost centre {unknown} is not in the cost-code master.");
    }
}
