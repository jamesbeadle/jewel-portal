using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>
/// Locks a package: computes its live figures one last time and freezes them onto the
/// package, realising profit / loss against ACTUAL invoiced cost (sales value − invoiced
/// to date) rather than committed orders — invoicing past the orders must never flatter
/// the banked result. Unlocking clears the snapshot; figures go live again.
/// </summary>
public sealed class SetReconciliationPackageLockHandler : ICommandHandler<SetReconciliationPackageLock, ReconciliationPackage>
{
    private readonly JpmsContext context;

    public SetReconciliationPackageLockHandler(JpmsContext context) { this.context = context; }

    public async Task<ReconciliationPackage> HandleAsync(SetReconciliationPackageLock command, CancellationToken cancellationToken)
    {
        var package = await context.ReconciliationPackages.FirstOrDefaultAsync(
                candidate => candidate.ReconciliationPackageId == command.ReconciliationPackageId
                             && candidate.ProjectId == command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException("This package no longer exists — refresh and try again.");

        if (command.IsLocked && !package.IsLocked)
        {
            var rows = await PackageReconciliationCalculator.ComputeAsync(context, command.ProjectId, cancellationToken);
            var row = rows.First(candidate => string.Equals(
                candidate.ReconciliationPackageId, package.ReconciliationPackageId, StringComparison.OrdinalIgnoreCase));

            package.IsLocked = true;
            package.LockedAt = DateTimeOffset.UtcNow;
            package.LockedSalesValue = row.SalesValue;
            package.LockedClaimedToDate = row.ClaimedToDate;
            package.LockedTargetCost = row.TargetCost;
            package.LockedWoCommitted = row.WoCommitted;
            package.LockedInvoicedCost = row.InvoicedToDate;
            package.LockedProfitLoss = row.SalesValue - row.InvoicedToDate;
        }
        else if (!command.IsLocked && package.IsLocked)
        {
            package.IsLocked = false;
            package.LockedAt = null;
            package.LockedSalesValue = 0m;
            package.LockedClaimedToDate = 0m;
            package.LockedTargetCost = 0m;
            package.LockedWoCommitted = 0m;
            package.LockedInvoicedCost = 0m;
            package.LockedProfitLoss = 0m;
        }

        await context.SaveChangesAsync(cancellationToken);

        var orders = await context.ReconciliationPackageOrders
            .Where(member => member.ReconciliationPackageId == package.ReconciliationPackageId)
            .Select(member => member.WorkOrderId)
            .ToListAsync(cancellationToken);
        var slices = await context.ReconciliationPackageSalesLines
            .Where(slice => slice.ReconciliationPackageId == package.ReconciliationPackageId)
            .Select(slice => new PackageSalesSlice(slice.ValuationLineItemId, slice.Amount))
            .ToListAsync(cancellationToken);
        var costSlices = await context.ReconciliationPackageCostLines
            .Where(slice => slice.ReconciliationPackageId == package.ReconciliationPackageId)
            .Select(slice => new PackageCostSlice(slice.XeroLedgerLineId, slice.Amount))
            .ToListAsync(cancellationToken);
        return new ReconciliationPackage(
            package.ReconciliationPackageId, package.ProjectId, package.Name,
            orders, slices, package.IsLocked, package.LockedAt, costSlices);
    }
}
