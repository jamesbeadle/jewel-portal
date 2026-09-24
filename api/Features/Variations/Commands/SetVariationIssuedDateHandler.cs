using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>Sets the Issued stamp to the day the client was sent the variation. The stamp is kept
/// at midnight UTC of that day, so every reading of it (the register, the Contractor's Report's
/// Position line) gives the day entered.</summary>
public sealed class SetVariationIssuedDateHandler : ICommandHandler<SetVariationIssuedDate, VariationOrder>
{
    private readonly JpmsContext context;
    public SetVariationIssuedDateHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrder> HandleAsync(SetVariationIssuedDate command, CancellationToken cancellationToken)
    {
        var order = await context.VariationOrders.FindAsync(new object[] { command.VariationOrderId }, cancellationToken)
            ?? throw new InvalidOperationException($"Variation order {command.VariationOrderId} not found.");
        if (order.IssuedAt is null)
            throw new InvalidOperationException("This variation has not been issued — move it to Issued first.");

        order.IssuedAt = new DateTimeOffset(command.IssuedOn.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        await context.SaveChangesAsync(cancellationToken);
        return order.ToModel();
    }
}
