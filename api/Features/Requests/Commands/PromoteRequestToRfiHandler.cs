using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// Promotes a request to an RFI: mints the RFI reference and unlocks the official document.
/// Promotion deliberately does NOT stage an email — the draft is created only when a person
/// explicitly asks for one ("Prepare email draft" for a fresh email, or a reply draft into a
/// tagged email chain), so promoting is a pure register action whose only mailbox side effect
/// is aliasing the workflow tag onto the newly minted reference (see below).
/// </summary>
public sealed class PromoteRequestToRfiHandler : ICommandHandler<PromoteRequestToRfi, Request>
{
    private readonly JpmsContext context;
    private readonly IMailboxGraphClient graph;
    private readonly ILogger<PromoteRequestToRfiHandler> logger;

    public PromoteRequestToRfiHandler(JpmsContext context, IMailboxGraphClient graph, ILogger<PromoteRequestToRfiHandler> logger)
    {
        this.context = context;
        this.graph = graph;
        this.logger = logger;
    }

    public async Task<Request> HandleAsync(PromoteRequestToRfi command, CancellationToken cancellationToken)
    {
        var entity = await context.Requests.FindAsync(new object[] { command.RequestId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Request {command.RequestId} not found.");

        // The mailbox tag is derived from the (project-qualified) reference, so capture the tag as it
        // stands BEFORE promotion mints the RFI reference — for a mailbox-raised request that's the
        // REQ-#### fallback stem (e.g. "JPMS/JBB-2026-001-REQ-0122") carried by its intake email.
        var projectRef = await RequestTags.ProjectRefAsync(context, entity.ProjectId, cancellationToken);
        var previousTag = TriageCategories.ForRecord(RequestTags.Stem(projectRef, entity.ProjectId, entity.TagReference));

        // Mint the next RFI reference for this project on first promotion. A General container carries a
        // REQ-#### reference; becoming an official RFI gives it a place in the project's RFI sequence
        // (RFI-001, RFI-002…). References already in the RFI series (e.g. a back-filled RFI) are left as-is.
        // A concurrent promotion can race to the same number; the per-project unique index rejects the
        // save, so re-mint from the fresh register and retry a couple of times before giving up.
        var mintReference = !entity.Reference.StartsWith("RFI-", StringComparison.OrdinalIgnoreCase);
        entity.Kind = (int)RequestType.Rfi;
        if (entity.Status == (int)RequestStatus.Closed) entity.Status = (int)RequestStatus.NeedsAction;

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

        // Minting RFI-NNN changed the reference, and every live mailbox read (the conversation view,
        // the "append to a tagged email chain" list, the thread sweep) derives its tag from it — so
        // ADD the new JPMS/<ref> tag to every email carrying the old one. The old REQ-#### tag is
        // deliberately KEPT alongside as a permanent alias: it is the request's immutable internal
        // number, so the correspondence stays traceable to the record however the human reference
        // evolves. Best-effort, like the reference-edit retag: the promotion is already saved, and a
        // transient Graph failure must not fail it (re-promoting or a reference edit can reconcile).
        var newTag = TriageCategories.ForRecord(RequestTags.Stem(projectRef, entity.ProjectId, entity.TagReference));
        if (!string.Equals(previousTag, newTag, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var aliased = await graph.AddAliasTagAsync(previousTag, newTag, cancellationToken);
                logger.LogInformation(
                    "Aliased {Count} email(s) from {OldTag} onto {NewTag} after RFI promotion (old tag kept).",
                    aliased, previousTag, newTag);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Tag alias from {OldTag} onto {NewTag} failed after RFI promotion; emails may not carry the new tag yet.",
                    previousTag, newTag);
            }
        }

        // Return with the itemised queries so the detail view keeps them across the promotion.
        var items = await context.RequestItems
            .Where(item => item.RequestId == entity.RequestId)
            .ToListAsync(cancellationToken);
        return entity.ToModel(items);
    }
}
