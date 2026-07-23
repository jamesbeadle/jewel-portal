using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Accepts a subcontractor's variation request by creating its variation order directly in
/// Quoting with the tender already recorded: there is no tender round because the price arrived
/// with the request — the sub's proposed value becomes EstimatedValue and the sub the
/// SelectedSubcontractorId. The order then runs the normal lifecycle (Issued → Approved via
/// ApproveVariationOrder, valuation + CVR + committed budget). RequestId stays empty: this order
/// originates from a portal request, not an RFI.
/// </summary>
public sealed class AcceptVariationRequestHandler : ICommandHandler<AcceptVariationRequest, VariationOrder>
{
    private readonly JpmsContext context;

    public AcceptVariationRequestHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrder> HandleAsync(AcceptVariationRequest command, CancellationToken cancellationToken)
    {
        var variationRequest = await context.SubcontractorVariationRequests
            .FirstOrDefaultAsync(row => row.VariationRequestId == command.VariationRequestId, cancellationToken);
        if (variationRequest is null)
            throw new InvalidOperationException($"Variation request {command.VariationRequestId} not found.");
        if (variationRequest.Status is not ((int)VariationRequestStatus.Submitted or (int)VariationRequestStatus.UnderReview))
            throw new InvalidOperationException("Only an open variation request can be accepted.");

        var now = DateTimeOffset.UtcNow;
        var nextNumber = (await context.VariationOrders
            .Where(o => o.ProjectId == variationRequest.ProjectId)
            .MaxAsync(o => (int?)o.Number, cancellationToken) ?? 0) + 1;

        var order = new VariationOrderEntity
        {
            VariationOrderId = VariationsIdentifierFactory.NextVariationOrderId(),
            ProjectId = variationRequest.ProjectId,
            RequestId = "",
            Number = nextNumber,
            Reference = VariationsIdentifierFactory.Reference(nextNumber),
            Title = variationRequest.Title,
            Description = variationRequest.Description,
            Status = (int)VariationOrderStatus.Quoting,
            SelectedSubcontractorId = variationRequest.SubcontractorId,
            EstimatedValue = variationRequest.ProposedValue,
            CreatedAt = now,
            CreatedByEmail = command.AcceptedByEmail
        };
        context.VariationOrders.Add(order);

        variationRequest.Status = (int)VariationRequestStatus.Accepted;
        variationRequest.ReviewedAt = now;
        variationRequest.ReviewedByEmail = command.AcceptedByEmail;
        variationRequest.VariationOrderId = order.VariationOrderId;

        await context.SaveChangesAsync(cancellationToken);
        return order.ToModel();
    }
}
