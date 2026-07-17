using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Repairs a VOQ's link to the request (RFI) it was raised from — for records that predate the
/// link (e.g. seeded variations). The request must exist, belong to the VOQ's project, and not
/// already carry a different VOQ (a request has at most one).
/// </summary>
public sealed class LinkVoqToRequestHandler : ICommandHandler<LinkVoqToRequest, VariationOrderQuote>
{
    private readonly JpmsContext context;
    public LinkVoqToRequestHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrderQuote> HandleAsync(LinkVoqToRequest command, CancellationToken cancellationToken)
    {
        var voq = await context.VariationOrderQuotes.FindAsync(new object[] { command.VariationOrderQuoteId }, cancellationToken);
        if (voq is null) throw new InvalidOperationException($"VOQ {command.VariationOrderQuoteId} not found.");

        var request = await context.Requests.FindAsync(new object[] { command.RequestId }, cancellationToken);
        if (request is null) throw new InvalidOperationException($"Request {command.RequestId} not found.");
        if (!string.Equals(request.ProjectId, voq.ProjectId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("That request belongs to a different project.");

        var alreadyTaken = await context.VariationOrderQuotes.AnyAsync(
            other => other.RequestId == command.RequestId
                && other.VariationOrderQuoteId != command.VariationOrderQuoteId,
            cancellationToken);
        if (alreadyTaken) throw new InvalidOperationException("That request already has a VOQ linked to it.");

        voq.RequestId = command.RequestId;
        // A VOQ only exists past the RFQ stage, so the linked request has implicitly climbed that
        // ladder — set the flag so flag-driven UI (the RFQ/VOQ sections, the lineage strip) shows
        // the link without a separate "enable RFQ" step.
        request.HasRfq = true;

        await context.SaveChangesAsync(cancellationToken);
        return voq.ToModel();
    }
}
