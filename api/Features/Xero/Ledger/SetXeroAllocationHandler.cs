using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

/// <summary>
/// Applies one allocation action to a batch of ledger lines. Returns how many
/// lines were updated. AllocatedBy arrives stamped by the endpoint from the
/// signed-in user.
/// </summary>
public sealed class SetXeroAllocationHandler : ICommandHandler<SetXeroAllocation, int>
{
    private readonly JpmsContext context;

    public SetXeroAllocationHandler(JpmsContext context) { this.context = context; }

    public async Task<int> HandleAsync(SetXeroAllocation command, CancellationToken cancellationToken)
    {
        if (command.Action == XeroAllocationAction.Allocate)
        {
            var projectExists = await context.Projects
                .AnyAsync(project => project.ProjectId == command.ProjectId, cancellationToken);
            if (!projectExists)
                throw new InvalidOperationException("Choose a project before allocating.");

            var costCenterActive = await context.CostCenters
                .AnyAsync(centre => centre.Code == command.CostCenterCode && centre.IsActive, cancellationToken);
            if (!costCenterActive)
                throw new InvalidOperationException("Choose an active cost centre before allocating.");
        }

        if (command.Action == XeroAllocationAction.AllocateToBucket
            && !XeroBuckets.All.Contains(command.Bucket ?? "", StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Choose a bucket (Parking, Fuel, Software subscriptions or Other).");

        var ids = command.XeroLedgerLineIds.Distinct().ToList();
        var lines = await context.XeroLedgerLines
            .Where(line => ids.Contains(line.XeroLedgerLineId))
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var line in lines)
        {
            switch (command.Action)
            {
                case XeroAllocationAction.Allocate:
                    line.AllocationStatus = (int)XeroAllocationStatus.Allocated;
                    line.ProjectId = command.ProjectId;
                    line.CostCenterCode = command.CostCenterCode;
                    line.Bucket = null;
                    line.AllocatedBy = command.AllocatedBy;
                    line.AllocatedAtUtc = now;
                    line.Note = command.Note;
                    break;
                case XeroAllocationAction.AllocateToBucket:
                    line.AllocationStatus = (int)XeroAllocationStatus.Bucketed;
                    line.ProjectId = null;
                    line.CostCenterCode = null;
                    line.Bucket = command.Bucket;
                    line.AllocatedBy = command.AllocatedBy;
                    line.AllocatedAtUtc = now;
                    line.Note = command.Note;
                    break;
                case XeroAllocationAction.Ignore:
                    line.AllocationStatus = (int)XeroAllocationStatus.Ignored;
                    line.ProjectId = null;
                    line.CostCenterCode = null;
                    line.Bucket = null;
                    line.AllocatedBy = command.AllocatedBy;
                    line.AllocatedAtUtc = now;
                    line.Note = command.Note;
                    break;
                case XeroAllocationAction.Reset:
                    line.AllocationStatus = (int)XeroAllocationStatus.Unallocated;
                    line.ProjectId = null;
                    line.CostCenterCode = null;
                    line.Bucket = null;
                    line.AllocatedBy = null;
                    line.AllocatedAtUtc = null;
                    line.Note = null;
                    break;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return lines.Count;
    }
}
