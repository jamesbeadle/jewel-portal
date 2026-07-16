using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class RemoveReconciliationPackageHandler : ICommandHandler<RemoveReconciliationPackage, Acknowledgement>
{
    private readonly JpmsContext context;

    public RemoveReconciliationPackageHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(RemoveReconciliationPackage command, CancellationToken cancellationToken)
    {
        var package = await context.ReconciliationPackages.FirstOrDefaultAsync(
            candidate => candidate.ReconciliationPackageId == command.ReconciliationPackageId
                         && candidate.ProjectId == command.ProjectId, cancellationToken);
        if (package is null) return new Acknowledgement(command.ReconciliationPackageId); // already gone — idempotent

        if (package.IsLocked)
            throw new InvalidOperationException(
                "This package is locked — its realised profit / loss is banked. Unlock it first if you really want to dissolve it.");

        var orders = await context.ReconciliationPackageOrders
            .Where(member => member.ReconciliationPackageId == package.ReconciliationPackageId)
            .ToListAsync(cancellationToken);
        var slices = await context.ReconciliationPackageSalesLines
            .Where(slice => slice.ReconciliationPackageId == package.ReconciliationPackageId)
            .ToListAsync(cancellationToken);
        var costSlices = await context.ReconciliationPackageCostLines
            .Where(slice => slice.ReconciliationPackageId == package.ReconciliationPackageId)
            .ToListAsync(cancellationToken);

        context.ReconciliationPackageOrders.RemoveRange(orders);
        context.ReconciliationPackageSalesLines.RemoveRange(slices);
        context.ReconciliationPackageCostLines.RemoveRange(costSlices);
        context.ReconciliationPackages.Remove(package);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(package.ReconciliationPackageId);
    }
}
