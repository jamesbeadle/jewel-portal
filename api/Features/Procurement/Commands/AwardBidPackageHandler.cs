using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Creates the work order (the purchase-order record) for the winning subcontractor, marks their
// recipient row Won (a previously-marked winner drops back to Responded on re-award), and moves
// the package to Awarded.
public sealed class AwardBidPackageHandler
    : ICommandHandler<AwardBidPackage, WorkOrder>
{
    private readonly JpmsContext context;

    public AwardBidPackageHandler(JpmsContext context) { this.context = context; }

    public async Task<WorkOrder> HandleAsync(AwardBidPackage command, CancellationToken cancellationToken)
    {
        var package = await context.BidPackages.FindAsync(new object[] { command.BidPackageId }, cancellationToken);
        if (package is null) throw new InvalidOperationException($"Bid package {command.BidPackageId} not found.");

        // Numbers are sequential per project (mirroring Buildertrend's per-job PO numbering, which
        // seeded orders keep so paperwork cross-references hold), shared across all orders however
        // they were raised (award, direct, seed) — so max+1 within the project, not a count.
        var nextNumber = (await context.WorkOrders
            .Where(order => order.ProjectId == command.ProjectId)
            .MaxAsync(order => (int?)order.Number, cancellationToken) ?? 0) + 1;

        var now = DateTimeOffset.UtcNow;
        var entity = new WorkOrderEntity
        {
            WorkOrderId = ProcurementIdentifierFactory.NextWorkOrderId(),
            ProjectId = command.ProjectId,
            BidPackageId = command.BidPackageId,
            SubcontractorId = command.SubcontractorId,
            Value = command.Value,
            Scope = command.Scope,
            AwardedAt = now,
            AwardedByEmail = command.AwardedByEmail,
            Number = nextNumber,
            Title = package.Title,
            Status = (int)WorkOrderStatus.Released,
            CreatedAt = now
        };
        context.WorkOrders.Add(entity);
        package.Status = (int)BidPackageStatus.Awarded;

        var recipients = await context.BidPackageRecipients
            .Where(r => r.BidPackageId == command.BidPackageId)
            .ToListAsync(cancellationToken);
        foreach (var recipient in recipients)
        {
            if (string.Equals(recipient.SubcontractorId, command.SubcontractorId, StringComparison.OrdinalIgnoreCase))
                recipient.Status = (int)BidPackageRecipientStatus.Won;
            else if (recipient.Status == (int)BidPackageRecipientStatus.Won)
                recipient.Status = (int)BidPackageRecipientStatus.Responded;
        }

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
