using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class AwardBidPackageHandler
    : ICommandHandler<AwardBidPackage, WorkOrder>
{
    private readonly JpmsContext context;

    public AwardBidPackageHandler(JpmsContext context) { this.context = context; }

    public async Task<WorkOrder> HandleAsync(AwardBidPackage command, CancellationToken cancellationToken)
    {
        var package = await context.BidPackages.FindAsync(new object[] { command.BidPackageId }, cancellationToken);
        if (package is null) throw new InvalidOperationException($"Bid package {command.BidPackageId} not found.");

        var entity = new WorkOrderEntity
        {
            WorkOrderId = ProcurementIdentifierFactory.NextWorkOrderId(),
            ProjectId = command.ProjectId,
            BidPackageId = command.BidPackageId,
            SubcontractorId = command.SubcontractorId,
            Value = command.Value,
            Scope = command.Scope,
            AwardedAt = DateTimeOffset.UtcNow,
            AwardedByEmail = command.AwardedByEmail
        };
        context.WorkOrders.Add(entity);
        package.Status = (int)BidPackageStatus.Awarded;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
