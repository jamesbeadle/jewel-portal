using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial;

/// <summary>
/// Computes every package's report row from source: sales value from the member sales
/// slices, claimed pro-rata from the latest claim, target cost via the assumed markup,
/// WO committed from the member orders' lines, invoiced from the invoice→order link
/// slices. Locked packages return the snapshot frozen at lock instead — a closed
/// package's banked figures never move. The report query and the lock command share
/// this so they can never disagree.
/// </summary>
internal static class PackageReconciliationCalculator
{
    public static async Task<List<PackageReconciliationRow>> ComputeAsync(
        JpmsContext context, string projectId, CancellationToken cancellationToken)
    {
        var packages = await context.ReconciliationPackages
            .Where(package => package.ProjectId == projectId)
            .OrderBy(package => package.Name)
            .ToListAsync(cancellationToken);
        if (packages.Count == 0) return new List<PackageReconciliationRow>();

        var orders = await context.ReconciliationPackageOrders
            .Where(member => member.ProjectId == projectId)
            .ToListAsync(cancellationToken);
        var slices = await context.ReconciliationPackageSalesLines
            .Where(slice => slice.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        // Cost side: committed per order (line totals) and actually invoiced per order
        // (the invoice→order link slices; credit notes negative).
        var orderIds = orders.Select(member => member.WorkOrderId).Distinct().ToList();
        var committedByOrder = (await context.WorkOrderLines
                .Where(line => orderIds.Contains(line.WorkOrderId))
                .GroupBy(line => line.WorkOrderId)
                .Select(group => new { WorkOrderId = group.Key, Total = group.Sum(line => line.LineTotal) })
                .ToListAsync(cancellationToken))
            .ToDictionary(entry => entry.WorkOrderId, entry => entry.Total, StringComparer.OrdinalIgnoreCase);
        var invoicedByOrder = (await context.XeroLineWorkOrderLinks
                .Where(link => orderIds.Contains(link.WorkOrderId))
                .GroupBy(link => link.WorkOrderId)
                .Select(group => new { WorkOrderId = group.Key, Total = group.Sum(link => link.Amount) })
                .ToListAsync(cancellationToken))
            .ToDictionary(entry => entry.WorkOrderId, entry => entry.Total, StringComparer.OrdinalIgnoreCase);

        // Sales side: each member line's full value, and its claimed-to-date from the
        // latest claim (any status — mirrors the financial summary). A partial slice
        // takes its pro-rata share of the line's claimed value.
        var lineIds = slices.Select(slice => slice.ValuationLineItemId).Distinct().ToList();
        var lineAmounts = (await context.ValuationLineItems
                .Where(line => lineIds.Contains(line.ValuationLineItemId))
                .Select(line => new { line.ValuationLineItemId, line.LineAmount })
                .ToListAsync(cancellationToken))
            .ToDictionary(entry => entry.ValuationLineItemId, entry => entry.LineAmount, StringComparer.OrdinalIgnoreCase);

        var latestClaimId = await context.ValuationClaims
            .Where(claim => claim.ProjectId == projectId)
            .OrderByDescending(claim => claim.ClaimNumber)
            .Select(claim => (string?)claim.ValuationClaimId)
            .FirstOrDefaultAsync(cancellationToken);
        var claimedByLine = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        if (latestClaimId is not null)
        {
            var claimed = await context.ClaimLines
                .Where(claimLine => claimLine.ValuationClaimId == latestClaimId
                                    && lineIds.Contains(claimLine.ValuationLineItemId))
                .Select(claimLine => new { claimLine.ValuationLineItemId, claimLine.CumulativeClaimed })
                .ToListAsync(cancellationToken);
            foreach (var entry in claimed) claimedByLine[entry.ValuationLineItemId] = entry.CumulativeClaimed;
        }

        var ordersByPackage = orders.ToLookup(member => member.ReconciliationPackageId, StringComparer.OrdinalIgnoreCase);
        var slicesByPackage = slices.ToLookup(slice => slice.ReconciliationPackageId, StringComparer.OrdinalIgnoreCase);

        return packages.Select(package =>
        {
            var memberOrders = ordersByPackage[package.ReconciliationPackageId].ToList();
            var memberSlices = slicesByPackage[package.ReconciliationPackageId].ToList();

            if (package.IsLocked)
            {
                // Banked at lock — the report never recomputes a closed package.
                return new PackageReconciliationRow(
                    package.ReconciliationPackageId, package.Name, true, package.LockedAt,
                    memberOrders.Count, memberSlices.Count,
                    package.LockedSalesValue, package.LockedClaimedToDate, package.LockedTargetCost,
                    package.LockedWoCommitted, package.LockedInvoicedCost,
                    Drawdown: 0m, Margin: 0m, ProfitLoss: package.LockedProfitLoss);
            }

            var salesValue = memberSlices.Sum(slice => slice.Amount);
            var claimed = memberSlices.Sum(slice =>
            {
                if (!lineAmounts.TryGetValue(slice.ValuationLineItemId, out var lineAmount) || lineAmount == 0m) return 0m;
                var lineClaimed = claimedByLine.TryGetValue(slice.ValuationLineItemId, out var value) ? value : 0m;
                return lineClaimed * slice.Amount / lineAmount;
            });
            var targetCost = Math.Round(salesValue * FinancialSummaryAssumptions.CostFactor, 2);
            var woCommitted = memberOrders.Sum(member =>
                committedByOrder.TryGetValue(member.WorkOrderId, out var committed) ? committed : 0m);
            var invoiced = memberOrders.Sum(member =>
                invoicedByOrder.TryGetValue(member.WorkOrderId, out var total) ? total : 0m);

            return new PackageReconciliationRow(
                package.ReconciliationPackageId, package.Name, false, null,
                memberOrders.Count, memberSlices.Count,
                salesValue, Math.Round(claimed, 2), targetCost, woCommitted, invoiced,
                // Budget left to commit; and the live forecast buying gain — invoicing
                // past the committed orders tightens it (never flatters).
                Drawdown: targetCost - woCommitted,
                Margin: targetCost - Math.Max(woCommitted, invoiced),
                ProfitLoss: 0m);
        }).ToList();
    }
}
