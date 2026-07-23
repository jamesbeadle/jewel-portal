using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Records the winning bid package + subcontractor (and agreed value) on a quoting variation
/// order. The bid package must belong to the order. Quoting-stage data only — the status does
/// not change.
/// </summary>
public sealed class SelectVoqTenderHandler : ICommandHandler<SelectVoqTender, VariationOrder>
{
    private readonly JpmsContext context;
    public SelectVoqTenderHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrder> HandleAsync(SelectVoqTender command, CancellationToken cancellationToken)
    {
        var order = await context.VariationOrders.FindAsync(new object[] { command.VariationOrderId }, cancellationToken);
        if (order is null) throw new InvalidOperationException($"Variation order {command.VariationOrderId} not found.");

        var belongs = await context.BidPackages.AnyAsync(
            package => package.BidPackageId == command.BidPackageId
                && package.VariationOrderId == command.VariationOrderId,
            cancellationToken);
        if (!belongs) throw new InvalidOperationException("That bid package does not belong to this variation order.");

        order.SelectedBidPackageId = command.BidPackageId;
        order.SelectedSubcontractorId = command.SubcontractorId;
        order.EstimatedValue = command.EstimatedValue;
        // The recorded tender is quoting-stage data — the order stays Quoting until it is issued.

        await context.SaveChangesAsync(cancellationToken);
        return order.ToModel();
    }
}
