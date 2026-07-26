using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake;
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
/// person can review, adjust and send it from the mailbox itself. Nothing is sent, but an Open
/// request moves to Awaiting Response the moment its draft lands in the mailbox — the working
/// assumption is that a drafted document goes out. If the team cancels the send they set the
/// request back to Open by hand. Requests already past Open (Responded, Approved, Closed…)
/// keep their status: re-drafting never rewinds a lifecycle.
///
/// Recipients come from the shared <see cref="RequestRecipientResolver"/> (request party →
/// project party → project profile To rows, with the correspondence profile supplying CC/BCC) —
/// the same resolution the worker send uses. An ad-hoc override addresses the draft to that one
/// email instead, with no CC/BCC.
/// </summary>
public sealed class PrepareRequestEmailDraftHandler : ICommandHandler<PrepareRequestEmailDraft, RequestEmailDraft>
{
    private readonly JpmsContext context;
    private readonly IMailboxGraphClient graph;
    private readonly Audit.AuditTrail audit;
    private readonly MailboxIntakeOptions mailboxOptions;
    private readonly Attachments.IRequestAttachmentStore attachmentStore;

    public PrepareRequestEmailDraftHandler(
        JpmsContext context,
        IMailboxGraphClient graph,
        Audit.AuditTrail audit,
        MailboxIntakeOptions mailboxOptions,
        Attachments.IRequestAttachmentStore attachmentStore)
    {
        this.context = context;
        this.graph = graph;
        this.audit = audit;
        this.mailboxOptions = mailboxOptions;
        this.attachmentStore = attachmentStore;
    }

    public async Task<RequestEmailDraft> HandleAsync(PrepareRequestEmailDraft command, CancellationToken cancellationToken)
    {
        var request = await context.Requests
            .FirstOrDefaultAsync(r => r.RequestId == command.RequestId, cancellationToken);
        if (request is null) throw new InvalidOperationException($"Request '{command.RequestId}' not found.");

        // EMAIL POLICY: only RFI / NOD / EOT documents are ever drafted for sending.
        var kind = (RequestType)request.Kind;
        if (!kind.IsEmailable())
            throw new InvalidOperationException(
                $"A {kind.DisplayName()} request is never emailed — only RFI, NOD and EOT documents " +
                "are drafted for sending. Promote the request first if it should go out as an RFI.");

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

        var model = await RequestDocumentBuilder.BuildAsync(
            context, command.RequestId, cancellationToken, recipients);
        if (model is null) throw new InvalidOperationException($"Request '{command.RequestId}' not found.");

        var pdf = RequestDocumentRenderer.Render(model);

        // The workflow tag rides on the draft and survives the send, so the Sent Items copy self-
        // associates with the record — and because this document opens a brand-new conversation,
        // replies to it inherit the tag through the thread sweep instead of waiting in triage.
        // Mirrors the worker's outbound send (MailboxActionWorker.SendRequestDocumentAsync).
        var recordTag = TriageCategories.ForRecord(await RequestTags.StemAsync(context, request, cancellationToken));

        // Files attached to the request ride out with the document. A site photograph is usually
        // the clearest thing in an RFI — the whole reason a site manager took it was so the
        // architect could see what they were being asked about — so it belongs on the email, not
        // only in the portal. Drawing LINKS are not sent: the architect issued those drawings, and
        // the PDF already cites them by code and revision.
        var attachments = new List<MailboxDraftAttachment>
        {
            new(model.FileName, "application/pdf", pdf)
        };
        attachments.AddRange(await LoadRequestFileAttachmentsAsync(request.RequestId, cancellationToken));

        var draft = new MailboxDraftMessage(
            recipients.To.Select(ToDraftRecipient).ToList(),
            model.EmailSubject,
            BuildCoverNote(model),
            attachments,
            Bcc: recipients.Bcc.Select(ToDraftRecipient).ToList(),
            Categories: new[] { TriageCategories.Marker, recordTag },
            Cc: recipients.Cc.Select(ToDraftRecipient).ToList());

        var created = await graph.CreateDraftAsync(draft, cancellationToken);
        if (created is null)
            throw new InvalidOperationException(
                "The draft couldn't be created in the projects mailbox. Check the mailbox connection and try again.");

        // The projects mailbox is copied on every draft by the Graph client itself, so report it
        // back alongside the resolved Cc — the confirmation the user reads must match the draft
        // that actually landed in Outlook.
        var cc = recipients.Cc.Select(r => r.Email).ToList();
        if (!string.IsNullOrWhiteSpace(mailboxOptions.Mailbox)
            && !recipients.To.Concat(recipients.Cc).Concat(recipients.Bcc)
                .Any(r => string.Equals(r.Email.Trim(), mailboxOptions.Mailbox, StringComparison.OrdinalIgnoreCase)))
            cc.Add(mailboxOptions.Mailbox.Trim());

        // Audit (client-facing): the drafted document, with its webLink, so the request's own
        // history says when it most likely went out and who prepared it. Written after the draft
        // actually landed — a failed create never records a send. To + Cc only, never Bcc.
        await audit.WriteAsync(
            AuditEventType.DraftCreated,
            $"{model.TypeShort} {model.DisplayNumber} document drafted to " +
            $"{string.Join(", ", recipients.To.Select(r => r.Email))}" +
            (cc.Count > 0 ? $" (copied: {string.Join(", ", cc)})" : "") +
            " — awaiting review and send.",
            pathway: "Client",
            projectId: request.ProjectId,
            recordType: RecordType.Request,
            recordId: request.RequestId,
            recordReference: model.DisplayNumber,
            emailMessageId: created.Id,
            webLink: created.WebLink,
            cancellationToken: cancellationToken);

        // Drafted means it's going out: a Needs-action request moves to Open (with the
        // correspondent, awaiting their response) now, and the team manually returns it to Needs
        // action if the send is cancelled. Only Needs action moves — a request already Open,
        // needing a variation or closed is never rewound by a re-draft.
        if ((RequestStatus)request.Status == RequestStatus.NeedsAction)
        {
            request.Status = (int)RequestStatus.Open;
            await context.SaveChangesAsync(cancellationToken);
        }

        return new RequestEmailDraft(
            request.RequestId,
            model.EmailSubject,
            recipients.To.Select(r => r.Email).ToList(),
            created.WebLink,
            Cc: cc,
            Bcc: recipients.Bcc.Select(r => r.Email).ToList());
    }

    /// <summary>
    /// Reads the request's uploaded files back out of blob storage so they can ride on the draft.
    /// Best-effort by design: a file that has gone missing, or storage being unreachable, must not
    /// stop the RFI going out — the document itself is the thing that matters, and the person
    /// reviewing the draft in Outlook can see what is attached before they send it.
    /// </summary>
    private async Task<List<MailboxDraftAttachment>> LoadRequestFileAttachmentsAsync(
        string requestId, CancellationToken cancellationToken)
    {
        var attachments = new List<MailboxDraftAttachment>();

        var rows = await context.RequestAttachments
            .AsNoTracking()
            .Where(row => row.RequestId == requestId && row.Kind == (int)RequestAttachmentKind.File)
            .OrderBy(row => row.AddedAt)
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.BlobRef)) continue;
            try
            {
                var blob = await attachmentStore.OpenAsync(row.BlobRef, cancellationToken);
                if (blob is null) continue;
                await using var content = blob.Content;
                using var buffer = new MemoryStream();
                await content.CopyToAsync(buffer, cancellationToken);
                attachments.Add(new MailboxDraftAttachment(
                    string.IsNullOrWhiteSpace(row.FileName) ? "attachment" : row.FileName,
                    row.ContentType ?? blob.ContentType,
                    buffer.ToArray()));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Skip this one and carry on — see the note above.
            }
        }

        return attachments;
    }

    private static MailboxDraftRecipient ToDraftRecipient(CorrespondenceRecipient r) =>
        new(r.Email, string.IsNullOrWhiteSpace(r.Name) ? null : r.Name);

    /// <summary>The short branded HTML cover note — mirrors the worker's outbound send so a drafted
    /// email reads the same as an auto-issued one. Internal so the reply-draft path
    /// (<see cref="PrepareRequestReplyDraftHandler"/>) reuses the identical note.</summary>
    internal static string BuildCoverNote(RequestDocumentModel model)
    {
        var due = model.ResponseDue is { } d
            ? $"<p style=\"margin:0 0 12px\">A response is requested by <strong>{d:dd MMM yyyy}</strong>.</p>"
            : string.Empty;

        // Lead with the client-visible reference (RFI-012) — matching the subject line, the PDF and
        // the register — not the internal REQ number. The type word is only added in the pre-numbering
        // fallback, where DisplayReference is the bare REQ number.
        var displayRef = !string.IsNullOrWhiteSpace(model.Reference)
            ? model.Reference
            : $"{model.TypeShort} {model.DisplayNumber}".Trim();

        return $@"<div style=""font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#1A1E29;line-height:1.5"">
  <p style=""margin:0 0 12px"">Please find attached <strong>{displayRef}</strong> &mdash; {System.Net.WebUtility.HtmlEncode(model.Title)} &mdash; for project {System.Net.WebUtility.HtmlEncode(model.ProjectName)} ({System.Net.WebUtility.HtmlEncode(model.ProjectReference)}).</p>
  {due}
  <p style=""margin:0 0 12px"">The attached PDF contains the full details and any references. Please reply to this email to respond.</p>
  <p style=""margin:16px 0 0;color:#C09A51;font-weight:bold"">Jewel Bespoke Build</p>
  <p style=""margin:0;color:#FF8300;font-size:12px"">jewelbb.co.uk</p>
</div>";
    }
}
