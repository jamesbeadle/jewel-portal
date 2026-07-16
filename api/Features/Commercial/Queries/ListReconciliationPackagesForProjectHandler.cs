using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListReconciliationPackagesForProjectHandler
    : IQueryHandler<ListReconciliationPackagesForProject, IReadOnlyList<ReconciliationPackage>>
{
    private readonly JpmsContext context;

    public ListReconciliationPackagesForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ReconciliationPackage>> HandleAsync(
        ListReconciliationPackagesForProject query, CancellationToken cancellationToken)
    {
        var packages = await context.ReconciliationPackages
            .Where(package => package.ProjectId == query.ProjectId)
            .OrderBy(package => package.Name)
            .ToListAsync(cancellationToken);

        var orders = await context.ReconciliationPackageOrders
            .Where(member => member.ProjectId == query.ProjectId)
            .ToListAsync(cancellationToken);
        var slices = await context.ReconciliationPackageSalesLines
            .Where(slice => slice.ProjectId == query.ProjectId)
            .ToListAsync(cancellationToken);
        var costSlices = await context.ReconciliationPackageCostLines
            .Where(slice => slice.ProjectId == query.ProjectId)
            .ToListAsync(cancellationToken);

        var ordersByPackage = orders.ToLookup(member => member.ReconciliationPackageId, StringComparer.OrdinalIgnoreCase);
        var slicesByPackage = slices.ToLookup(slice => slice.ReconciliationPackageId, StringComparer.OrdinalIgnoreCase);
        var costSlicesByPackage = costSlices.ToLookup(slice => slice.ReconciliationPackageId, StringComparer.OrdinalIgnoreCase);

        return packages
            .Select(package => new ReconciliationPackage(
                package.ReconciliationPackageId,
                package.ProjectId,
                package.Name,
                ordersByPackage[package.ReconciliationPackageId].Select(member => member.WorkOrderId).ToList(),
                slicesByPackage[package.ReconciliationPackageId]
                    .Select(slice => new PackageSalesSlice(slice.ValuationLineItemId, slice.Amount))
                    .ToList(),
                package.IsLocked,
                package.LockedAt,
                costSlicesByPackage[package.ReconciliationPackageId]
                    .Select(slice => new PackageCostSlice(slice.XeroLedgerLineId, slice.Amount))
                    .ToList()))
            .ToList();
    }
}
