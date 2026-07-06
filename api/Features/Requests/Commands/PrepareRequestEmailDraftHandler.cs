using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Requests.Documents;
using Jewel.JPMS.Api.Features.Requests.Recipients;
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
/// Recipients come from the shared <see cref="RequestRecipientResolver"/> (request party →
/// project party → project profile To rows, with the correspondence profile supplying CC/BCC) —
/// the same resolution the worker send uses. An ad-hoc override addresses the draft to that one
/// email instead, with no CC/BCC.
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

        // An ad-hoc override addresses the draft to that one email, nothing copied; otherwise the
        // shared resolver supplies the full To/CC/BCC set the send path would use.
        var recipients = !string.IsNullOrWhiteSpace(command.RecipientOverride)
            ? new RequestRecipientSet(
                new[] { new CorrespondenceRecipient("", command.RecipientOverride.Trim(), CorrespondenceRouting.To) },
                Array.Empty<CorrespondenceRecipient>(),
                Array.Empty<CorrespondenceRecipient>())
            : await RequestRecipientResolver.ResolveAsync(context, request, cancellationToken);
        if (!recipients.HasTo)
            throw new InvalidOperationException(
                "No recipient could be resolved. Link the request (or its project) to a client or architect " +
                "with a contact, or set a project contact's routing to To.");

        var tagged = await emails.ForRequestAsync(command.RequestId, cancellationToken);
        var model = await RequestDocumentBuilder.BuildAsync(
            context, command.RequestId, tagged, cancellationToken, recipients);
        if (model is null) throw new InvalidOperationException($"Request '{command.RequestId}' not found.");

        var pdf = RequestDocumentRenderer.Render(model);
        var draft = new MailboxDraftMessage(
            recipients.To.Select(ToDraftRecipient).ToList(),
            model.EmailSubject,
            BuildCoverNote(model),
            new[] { new MailboxDraftAttachment(model.FileName, "application/pdf", pdf) },
            Bcc: recipients.Bcc.Select(ToDraftRecipient).ToList(),
            Cc: recipients.Cc.Select(ToDraftRecipient).ToList());

        var created = await graph.CreateDraftAsync(draft, cancellationToken);
        if (created is null)
            throw new InvalidOperationException(
                "The draft couldn't be created in the projects mailbox. Check the mailbox connection and try again.");

        return new RequestEmailDraft(
            request.RequestId,
            model.EmailSubject,
            recipients.To.Select(r => r.Email).ToList(),
            created.WebLink,
            Cc: recipients.Cc.Select(r => r.Email).ToList(),
            Bcc: recipients.Bcc.Select(r => r.Email).ToList());
    }

    private static MailboxDraftRecipient ToDraftRecipient(CorrespondenceRecipient r) =>
        new(r.Email, string.IsNullOrWhiteSpace(r.Name) ? null : r.Name);

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
