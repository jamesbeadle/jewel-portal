using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Requests.Documents;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// Creates an Outlook draft in the connected projects mailbox carrying the request's official
/// document — recipients, subject, cover note and the freshly rendered PDF all pre-filled — so a
/// person can review, adjust and send it from the mailbox itself. Nothing is sent and no status
/// changes: issuing remains the send path's job.
///
/// Recipient resolution, most specific first:
///  1. the ad-hoc override, when given;
///  2. the request's linked party — an architect's contact email, or a client's primary contact
///     email — falling back to the project's party when the request carries no party link;
///  3. the project contacts flagged ReceivesRequests — the same fallback the send path uses.
/// </summary>
public sealed class PrepareRequestEmailDraftHandler : ICommandHandler<PrepareRequestEmailDraft, RequestEmailDraft>
{
    private readonly JpmsContext context;
    private readonly RequestEmailReader emails;
    private readonly IMailboxGraphClient graph;

    public PrepareRequestEmailDraftHandler(JpmsContext context, RequestEmailReader emails, IMailboxGraphClient graph)
    {
        this.context = context;
        this.emails = emails;
        this.graph = graph;
    }

    public async Task<RequestEmailDraft> HandleAsync(PrepareRequestEmailDraft command, CancellationToken cancellationToken)
    {
        var request = await context.Requests
            .FirstOrDefaultAsync(r => r.RequestId == command.RequestId, cancellationToken);
        if (request is null) throw new InvalidOperationException($"Request '{command.RequestId}' not found.");

        var tagged = await emails.ForRequestAsync(command.RequestId, cancellationToken);
        var model = await RequestDocumentBuilder.BuildAsync(context, command.RequestId, tagged, cancellationToken);
        if (model is null) throw new InvalidOperationException($"Request '{command.RequestId}' not found.");

        var recipients = await ResolveRecipientsAsync(request, model, command.RecipientOverride, cancellationToken);
        if (recipients.Count == 0)
            throw new InvalidOperationException(
                "No recipient could be resolved. Link the request (or its project) to a client or architect " +
                "with a contact email, or flag a project contact to receive requests.");

        var pdf = RequestDocumentRenderer.Render(model);
        var draft = new MailboxDraftMessage(
            recipients,
            model.EmailSubject,
            BuildCoverNote(model),
            new[] { new MailboxDraftAttachment(model.FileName, "application/pdf", pdf) });

        var created = await graph.CreateDraftAsync(draft, cancellationToken);
        if (created is null)
            throw new InvalidOperationException(
                "The draft couldn't be created in the projects mailbox. Check the mailbox connection and try again.");

        return new RequestEmailDraft(
            request.RequestId,
            model.EmailSubject,
            recipients.Select(r => r.Email).ToList(),
            created.WebLink);
    }

    private async Task<List<MailboxDraftRecipient>> ResolveRecipientsAsync(
        RequestEntity request, RequestDocumentModel model, string? recipientOverride, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(recipientOverride))
            return new List<MailboxDraftRecipient> { new(recipientOverride.Trim()) };

        // The request's own party link wins; otherwise the project's assigned party.
        var partyKind = request.PartyKind;
        var partyId = request.PartyId;
        if (string.IsNullOrWhiteSpace(partyId))
        {
            var project = await context.Projects
                .Where(p => p.ProjectId == request.ProjectId)
                .Select(p => new { p.PartyKind, p.PartyId })
                .FirstOrDefaultAsync(cancellationToken);
            partyKind = project?.PartyKind ?? (int)PartyKind.Client;
            partyId = project?.PartyId;
        }

        var recipients = new List<MailboxDraftRecipient>();
        if (!string.IsNullOrWhiteSpace(partyId))
        {
            if (partyKind == (int)PartyKind.Architect)
            {
                var architect = await context.Architects.FindAsync(new object[] { partyId }, cancellationToken);
                if (!string.IsNullOrWhiteSpace(architect?.ContactEmail))
                    recipients.Add(new MailboxDraftRecipient(architect!.ContactEmail.Trim(), architect.ContactName ?? architect.Name));
            }
            else
            {
                var client = await context.Clients.FindAsync(new object[] { partyId }, cancellationToken);
                if (!string.IsNullOrWhiteSpace(client?.PrimaryContactEmail))
                    recipients.Add(new MailboxDraftRecipient(client!.PrimaryContactEmail.Trim(), client.PrimaryContactName ?? client.Name));
            }
        }

        // Same fallback the send path uses: the project contacts flagged to receive requests
        // (already collated on the document model).
        if (recipients.Count == 0)
            recipients.AddRange(model.Recipients.Select(r => new MailboxDraftRecipient(r.Email, r.Name)));

        return recipients;
    }

    /// <summary>The short branded HTML cover note — mirrors the worker's outbound send so a drafted
    /// email reads the same as an auto-issued one.</summary>
    private static string BuildCoverNote(RequestDocumentModel model)
    {
        var due = model.ResponseDue is { } d
            ? $"<p style=\"margin:0 0 12px\">A response is requested by <strong>{d:dd MMM yyyy}</strong>.</p>"
            : string.Empty;

        return $@"<div style=""font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#1A1E29;line-height:1.5"">
  <p style=""margin:0 0 12px"">Please find attached <strong>{model.TypeShort} {model.DisplayNumber}</strong> &mdash; {System.Net.WebUtility.HtmlEncode(model.Title)} &mdash; for project {System.Net.WebUtility.HtmlEncode(model.ProjectName)} ({System.Net.WebUtility.HtmlEncode(model.ProjectReference)}).</p>
  {due}
  <p style=""margin:0 0 12px"">The attached PDF contains the full details and any references. Please reply to this email to respond.</p>
  <p style=""margin:16px 0 0;color:#C09A51;font-weight:bold"">Jewel Bespoke Build</p>
  <p style=""margin:0;color:#FF8300;font-size:12px"">jewelbb.co.uk</p>
</div>";
    }
}
