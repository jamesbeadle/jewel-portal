using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake.Actions;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// Promotes a request to an RFI and issues its document to the request's linked party — an
/// architect's contact email, or a client's primary contact email when Jewel works with the client
/// directly. Resolution: the request's party first, then the project's party, then the project's
/// Architect contact; if none is found the send falls back to the project's flagged contacts
/// (recipientOverride null), matching how a raised request is issued. Promotion never fails for
/// want of a recipient.
/// </summary>
public sealed class PromoteRequestToRfiHandler : ICommandHandler<PromoteRequestToRfi, Request>
{
    private readonly JpmsContext context;
    private readonly IMailboxActionScheduler mailbox;

    public PromoteRequestToRfiHandler(JpmsContext context, IMailboxActionScheduler mailbox)
    {
        this.context = context;
        this.mailbox = mailbox;
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

        var recipientEmail = await ResolveRecipientEmailAsync(entity, cancellationToken);
        await mailbox.ScheduleRequestDocumentSendAsync(entity.RequestId, recipientEmail, cancellationToken);

        // Return with the itemised queries so the detail view keeps them across the promotion.
        var items = await context.RequestItems
            .Where(item => item.RequestId == entity.RequestId)
            .ToListAsync(cancellationToken);
        return entity.ToModel(items);
    }

    private async Task<string?> ResolveRecipientEmailAsync(RequestEntity entity, CancellationToken cancellationToken)
    {
        // The request's own party first: architect contact email, or client primary contact email.
        var partyEmail = await ResolvePartyEmailAsync(entity.PartyKind, entity.PartyId, cancellationToken);
        if (partyEmail is not null) return partyEmail;

        // Then the project's party — a request with no party link of its own follows its project.
        var project = await context.Projects.FindAsync(new object[] { entity.ProjectId }, cancellationToken);
        if (project is not null)
        {
            var projectPartyEmail = await ResolvePartyEmailAsync(project.PartyKind, project.PartyId, cancellationToken);
            if (projectPartyEmail is not null) return projectPartyEmail;
        }

        // Legacy fallback: the project's Architect contact flagged to receive requests.
        var architectContact = await context.ProjectContacts
            .Where(contact => contact.ProjectId == entity.ProjectId
                && contact.Role == (int)ProjectContactRole.Architect
                && contact.ReceivesRequests)
            .Select(contact => contact.Email)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(architectContact) ? null : architectContact;
    }

    private async Task<string?> ResolvePartyEmailAsync(int partyKind, string? partyId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(partyId)) return null;

        if (partyKind == (int)PartyKind.Architect)
        {
            var architect = await context.Architects.FindAsync(new object[] { partyId }, cancellationToken);
            return string.IsNullOrWhiteSpace(architect?.ContactEmail) ? null : architect!.ContactEmail;
        }

        var client = await context.Clients.FindAsync(new object[] { partyId }, cancellationToken);
        return string.IsNullOrWhiteSpace(client?.PrimaryContactEmail) ? null : client!.PrimaryContactEmail;
    }
}
