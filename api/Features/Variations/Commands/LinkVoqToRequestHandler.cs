using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Repairs a variation order's link to the request (RFI) it was raised from — for records that
/// predate the link (e.g. seeded variations). The request must exist, belong to the order's
/// project, and not already carry a different order (a request has at most one).
/// </summary>
public sealed class LinkVoqToRequestHandler : ICommandHandler<LinkVoqToRequest, VariationOrder>
{
    private readonly JpmsContext context;
    public LinkVoqToRequestHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrder> HandleAsync(LinkVoqToRequest command, CancellationToken cancellationToken)
    {
        var order = await context.VariationOrders.FindAsync(new object[] { command.VariationOrderId }, cancellationToken);
        if (order is null) throw new InvalidOperationException($"Variation order {command.VariationOrderId} not found.");

        var request = await context.Requests.FindAsync(new object[] { command.RequestId }, cancellationToken);
        if (request is null) throw new InvalidOperationException($"Request {command.RequestId} not found.");
        if (!string.Equals(request.ProjectId, order.ProjectId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("That request belongs to a different project.");

        var alreadyTaken = await context.VariationOrders.AnyAsync(
            other => other.RequestId == command.RequestId
                && other.VariationOrderId != command.VariationOrderId,
            cancellationToken);
        if (alreadyTaken) throw new InvalidOperationException("That request already has a variation order linked to it.");

        order.RequestId = command.RequestId;
        // A variation order only exists past the RFQ stage, so the linked request has implicitly
        // climbed that ladder — set the flag so flag-driven UI (the RFQ/variation sections, the
        // lineage strip) shows the link without a separate "enable RFQ" step.
        request.HasRfq = true;

        await context.SaveChangesAsync(cancellationToken);
        return order.ToModel();
    }
}
