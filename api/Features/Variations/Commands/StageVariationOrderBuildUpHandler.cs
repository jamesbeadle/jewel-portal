using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Stages the agreed build-up on a pre-approval variation. Stores the normalised lines as JSON,
/// sets the estimate to their total (the same LineAmount maths approval uses, so the figure the
/// register shows now is the figure approval will write), and applies the narratives with the
/// keep-or-clear rule. Writes nothing to the Valuation Report, the CVR or any budget — those
/// are approval's, and approval consumes the staging.
/// </summary>
public sealed class StageVariationOrderBuildUpHandler : ICommandHandler<StageVariationOrderBuildUp, VariationOrder>
{
    private readonly JpmsContext context;
    public StageVariationOrderBuildUpHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrder> HandleAsync(StageVariationOrderBuildUp command, CancellationToken cancellationToken)
    {
        var order = await context.VariationOrders.FindAsync(new object[] { command.VariationOrderId }, cancellationToken);
        if (order is null) throw new InvalidOperationException($"Variation order {command.VariationOrderId} not found.");

        var status = (VariationOrderStatus)order.Status;
        if (status is VariationOrderStatus.Approved or VariationOrderStatus.Rejected)
        {
            throw new InvalidOperationException(
                $"{DisplayNumber(order)} is {status.DisplayName()} — a build-up is staged before approval. "
                + (status == VariationOrderStatus.Approved ? "Use Edit lines to change an approved variation's lines." : "A rejected variation is closed."));
        }

        var lines = command.Lines
            .Select(line => new VariationLineInput(line.CostCode.Trim(), (line.Description ?? "").Trim(), line.Quantity, line.Rate))
            .ToList();

        order.DraftLinesJson = VariationDraftLines.Serialise(lines);
        if (lines.Count > 0)
        {
            // The same maths approval uses (a negative line is an omit), so the staged figure is
            // exactly what approval would write.
            order.EstimatedValue = lines.Sum(line => ValuationCalculations.LineAmount(
                line.Quantity * line.Rate < 0m ? ValuationLineType.Omit : ValuationLineType.Priced, line.Quantity, line.Rate));
        }

        // Null keeps what stands; whitespace clears — the narratives rule everywhere on the record.
        if (command.CommercialBasis is not null) order.CommercialBasis = VariationNarratives.Clean(command.CommercialBasis);
        if (command.ProgrammeImpact is not null) order.ProgrammeImpact = VariationNarratives.Clean(command.ProgrammeImpact);
        if (command.Exclusions is not null) order.Exclusions = VariationNarratives.Clean(command.Exclusions);

        await context.SaveChangesAsync(cancellationToken);
        return order.ToModel();
    }

    private static string DisplayNumber(Data.Entities.VariationOrderEntity order) =>
        order.Number > 0 ? $"V{order.Number}" : order.Reference;
}
