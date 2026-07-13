using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Accepts a subcontractor's variation request by creating its VOQ directly in Selected state:
/// there is no tender round because the price arrived with the request — the sub's proposed value
/// becomes EstimatedValue and the sub SelectedSubcontractorId. Approval then runs the unchanged
/// ApproveVariationOrderQuote pipeline (VO + valuation + CVR + committed budget). RequestId stays
/// empty: this VOQ originates from a portal request, not an RFI.
/// </summary>
public sealed class AcceptVariationRequestHandler : ICommandHandler<AcceptVariationRequest, VariationOrderQuote>
{
    private readonly JpmsContext context;

    public AcceptVariationRequestHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrderQuote> HandleAsync(AcceptVariationRequest command, CancellationToken cancellationToken)
    {
        var variationRequest = await context.SubcontractorVariationRequests
            .FirstOrDefaultAsync(row => row.VariationRequestId == command.VariationRequestId, cancellationToken);
        if (variationRequest is null)
            throw new InvalidOperationException($"Variation request {command.VariationRequestId} not found.");
        if (variationRequest.Status is not ((int)VariationRequestStatus.Submitted or (int)VariationRequestStatus.UnderReview))
            throw new InvalidOperationException("Only an open variation request can be accepted.");

        var now = DateTimeOffset.UtcNow;
        var nextNumber = (await context.VariationOrderQuotes.MaxAsync(voq => (int?)voq.Number, cancellationToken) ?? 0) + 1;

        var voq = new VariationOrderQuoteEntity
        {
            VariationOrderQuoteId = VariationsIdentifierFactory.NextVoqId(),
            ProjectId = variationRequest.ProjectId,
            RequestId = "",
            Number = nextNumber,
            Reference = VariationsIdentifierFactory.Reference(nextNumber),
            Title = variationRequest.Title,
            Description = variationRequest.Description,
            Status = (int)VariationOrderQuoteStatus.Selected,
            SelectedSubcontractorId = variationRequest.SubcontractorId,
            EstimatedValue = variationRequest.ProposedValue,
            CreatedAt = now,
            CreatedByEmail = command.AcceptedByEmail
        };
        context.VariationOrderQuotes.Add(voq);

        variationRequest.Status = (int)VariationRequestStatus.Accepted;
        variationRequest.ReviewedAt = now;
        variationRequest.ReviewedByEmail = command.AcceptedByEmail;
        variationRequest.VariationOrderQuoteId = voq.VariationOrderQuoteId;

        await context.SaveChangesAsync(cancellationToken);
        return voq.ToModel();
    }
}
