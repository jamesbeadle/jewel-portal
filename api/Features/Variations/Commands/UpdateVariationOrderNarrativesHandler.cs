using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Re-states a variation order's narrative sections — commercial basis, programme impact and
/// exclusions — at any stage. Wording only, the same rule as a retitle: figures live with their
/// own commands, and the official document renders fresh from the record on every download and
/// send, so the corrected prose reaches the very next copy. Blank submissions clear a section
/// (the document simply omits it); everything is trimmed and clamped through the one shared
/// narrative rule so creation and editing can never disagree.
/// </summary>
public sealed class UpdateVariationOrderNarrativesHandler : ICommandHandler<UpdateVariationOrderNarratives, VariationOrder>
{
    private readonly JpmsContext context;
    public UpdateVariationOrderNarrativesHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrder> HandleAsync(UpdateVariationOrderNarratives command, CancellationToken cancellationToken)
    {
        var order = await context.VariationOrders.FindAsync(new object[] { command.VariationOrderId }, cancellationToken);
        if (order is null) throw new InvalidOperationException($"Variation order {command.VariationOrderId} not found.");

        order.CommercialBasis = VariationNarratives.Clean(command.CommercialBasis);
        order.ProgrammeImpact = VariationNarratives.Clean(command.ProgrammeImpact);
        order.Exclusions = VariationNarratives.Clean(command.Exclusions);

        await context.SaveChangesAsync(cancellationToken);
        return order.ToModel();
    }
}
