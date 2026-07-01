using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake.Actions;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// Promotes a request to an RFI and issues its document to the architect. Recipient resolution:
/// the linked client account's architect email first, then the project's Architect contact; if
/// neither is found the send falls back to the project's flagged contacts (recipientOverride null),
/// matching how a raised request is issued. Promotion never fails for want of a recipient.
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
        if (!entity.Reference.StartsWith("RFI-", StringComparison.OrdinalIgnoreCase))
        {
            var projectReferences = await context.Requests
                .Where(r => r.ProjectId == entity.ProjectId)
                .Select(r => r.Reference)
                .ToListAsync(cancellationToken);
            var nextRfi = RequestReference.HighestNumber("RFI", projectReferences) + 1;
            entity.Reference = $"RFI-{nextRfi:000}";
        }

        entity.Kind = (int)RequestType.Rfi;
        if (entity.Status == (int)RequestStatus.Closed) entity.Status = (int)RequestStatus.Open;
        await context.SaveChangesAsync(cancellationToken);

        var architectEmail = await ResolveArchitectEmailAsync(entity.ClientId, entity.ProjectId, cancellationToken);
        await mailbox.ScheduleRequestDocumentSendAsync(entity.RequestId, architectEmail, cancellationToken);

        return entity.ToModel();
    }

    private async Task<string?> ResolveArchitectEmailAsync(string? clientId, string projectId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(clientId))
        {
            var client = await context.Clients.FindAsync(new object[] { clientId }, cancellationToken);
            if (!string.IsNullOrWhiteSpace(client?.ArchitectEmail)) return client!.ArchitectEmail;
        }

        var architectContact = await context.ProjectContacts
            .Where(contact => contact.ProjectId == projectId
                && contact.Role == (int)ProjectContactRole.Architect
                && contact.ReceivesRequests)
            .Select(contact => contact.Email)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(architectContact) ? null : architectContact;
    }
}
