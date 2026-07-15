using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// Promotes a request to an RFI: mints the RFI reference and unlocks the official document.
/// Promotion deliberately does NOT stage an email — the draft is created only when a person
/// explicitly asks for one ("Prepare email draft" for a fresh email, or a reply draft into a
/// tagged email chain), so promoting is a pure register action with no mailbox side effects.
/// </summary>
public sealed class PromoteRequestToRfiHandler : ICommandHandler<PromoteRequestToRfi, Request>
{
    private readonly JpmsContext context;

    public PromoteRequestToRfiHandler(JpmsContext context)
    {
        this.context = context;
    }

    public async Task<Request> HandleAsync(PromoteRequestToRfi command, CancellationToken cancellationToken)
    {
        var entity = await context.Requests.FindAsync(new object[] { command.RequestId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Request {command.RequestId} not found.");

        // Mint the next RFI reference for this project on first promotion. A General container carries a
        // REQ-#### reference; becoming an official RFI gives it a place in the project's RFI sequence
        // (RFI-001, RFI-002…). References already in the RFI series (e.g. a back-filled RFI) are left as-is.
        // A concurrent promotion can race to the same number; the per-project unique index rejects the
        // save, so re-mint from the fresh register and retry a couple of times before giving up.
        var mintReference = !entity.Reference.StartsWith("RFI-", StringComparison.OrdinalIgnoreCase);
        entity.Kind = (int)RequestType.Rfi;
        if (entity.Status == (int)RequestStatus.Closed) entity.Status = (int)RequestStatus.Open;

        for (var attempt = 1; ; attempt++)
        {
            if (mintReference)
            {
                var projectReferences = await context.Requests
                    .Where(r => r.ProjectId == entity.ProjectId && r.RequestId != entity.RequestId)
                    .Select(r => r.Reference)
                    .ToListAsync(cancellationToken);
                entity.Reference = RequestReference.SuggestNext(RequestType.Rfi, projectReferences);
            }

            try
            {
                await context.SaveChangesAsync(cancellationToken);
                break;
            }
            catch (DbUpdateException ex) when (mintReference && attempt < 3 && RequestReferenceConflict.IsReferenceClash(ex))
            {
                // Lost the race for that number — loop and take the next one.
            }
        }

        // Return with the itemised queries so the detail view keeps them across the promotion.
        var items = await context.RequestItems
            .Where(item => item.RequestId == entity.RequestId)
            .ToListAsync(cancellationToken);
        return entity.ToModel(items);
    }
}
