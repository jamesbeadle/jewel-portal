using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Re-states a pre-approval variation's estimate (see SetVariationOrderEstimate). The estimate is
/// register data with no commercial writes behind it, so the write is the one field — but only
/// while nothing else owns the figure: a staged build-up's total is the estimate, an approved
/// order's figure is the contract Value with valuation/CVR/budget writes behind it, and a
/// rejected order's last quoted figure is part of its record.
/// </summary>
public sealed class SetVariationOrderEstimateHandler : ICommandHandler<SetVariationOrderEstimate, VariationOrder>
{
    private readonly JpmsContext context;
    public SetVariationOrderEstimateHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrder> HandleAsync(SetVariationOrderEstimate command, CancellationToken cancellationToken)
    {
        var order = await context.VariationOrders.FindAsync(new object[] { command.VariationOrderId }, cancellationToken);
        if (order is null) throw new InvalidOperationException($"Variation order {command.VariationOrderId} not found.");

        var status = (VariationOrderStatus)order.Status;
        if (!status.IsPreApproval())
            throw new InvalidOperationException(status == VariationOrderStatus.Approved
                ? "This variation is approved — revise its value instead; the estimate belongs to the quoting stage."
                : "This variation has been rejected — its last quoted figure is part of the record.");
        if (!string.IsNullOrWhiteSpace(order.DraftLinesJson))
            throw new InvalidOperationException(
                "A staged build-up's total is the estimate — clear the staged lines first if the figure must change.");
        if (command.EstimatedValue is < 0m)
            throw new InvalidOperationException("An estimate cannot be negative.");

        order.EstimatedValue = command.EstimatedValue;

        await context.SaveChangesAsync(cancellationToken);
        return order.ToModel();
    }
}
