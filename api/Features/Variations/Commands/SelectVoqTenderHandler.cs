using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Records the agreed subcontractor and value on a quoting variation order. Quoting-stage data
/// only — the status does not change. SelectedSubcontractorId is what IssueWorkOrderForVariation-
/// Order later instructs; SelectedBidPackageId is legacy (bid packages were separated from the VO
/// quoting process 2026-08-12) and is no longer written.
/// </summary>
public sealed class SelectVoqTenderHandler : ICommandHandler<SelectVoqTender, VariationOrder>
{
    private readonly JpmsContext context;
    public SelectVoqTenderHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrder> HandleAsync(SelectVoqTender command, CancellationToken cancellationToken)
    {
        var order = await context.VariationOrders.FindAsync(new object[] { command.VariationOrderId }, cancellationToken);
        if (order is null) throw new InvalidOperationException($"Variation order {command.VariationOrderId} not found.");

        order.SelectedSubcontractorId = command.SubcontractorId;
        order.EstimatedValue = command.EstimatedValue;
        // The recorded tender is quoting-stage data — the order stays Quoting until it is issued.

        await context.SaveChangesAsync(cancellationToken);
        return order.ToModel();
    }
}
