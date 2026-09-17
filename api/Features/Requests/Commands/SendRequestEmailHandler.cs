using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.MailboxIntake.Sharing;
using Jewel.JPMS.Api.Features.Requests.Documents;
using Jewel.JPMS.Api.Features.Requests.Recipients;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// Emails the request's official document from the projects mailbox as a new thread — recipients,
/// subject, cover note and the freshly rendered PDF all composed here. SaveAsDraftOnly stops after
/// staging, leaving the reviewed draft in Drafts for Outlook, which is what this handler did for
/// everybody until 2026-09-17.
///
/// Recipients come from the shared <see cref="RequestRecipientResolver"/> (request party → project
/// party → project profile To rows, with the correspondence profile supplying CC/BCC). An ad-hoc
/// override addresses it to that one email instead, with no CC/BCC. Files on the request ride out
/// with the document, or travel as download links when they would push the message past the
/// Exchange ceiling.
///
/// Staging, the send, the degrade back to a draft and the audit row are the dispatcher's
/// (OutboundEmailDispatcher); the status move is RequestLifecycle's, shared with the reply.
/// </summary>
public sealed class SendRequestEmailHandler : ICommandHandler<SendRequestEmail, RequestEmailOutcome>
{
    private const string StagingRefused =
        "The email couldn't be staged in the projects mailbox, so nothing was sent. "
        + "Check the mailbox connection and try again.";

    private readonly JpmsContext context;
    private readonly OutboundEmailDispatcher dispatcher;
    private readonly MailboxIntakeOptions mailboxOptions;
    private readonly Attachments.IRequestAttachmentStore attachmentStore;
    private readonly IEmailFileShareStore shareStore;

    public SendRequestEmailHandler(
        JpmsContext context,
        OutboundEmailDispatcher dispatcher,
        MailboxIntakeOptions mailboxOptions,
        Attachments.IRequestAttachmentStore attachmentStore,
        IEmailFileShareStore shareStore)
    {
        this.context = context;
        this.dispatcher = dispatcher;
        this.mailboxOptions = mailboxOptions;
        this.attachmentStore = attachmentStore;
        this.shareStore = shareStore;
    }

    public async Task<RequestEmailOutcome> HandleAsync(SendRequestEmail command, CancellationToken cancellationToken)
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
        //
        // Request uploads are allowed up to 64 MB each, well past what one email carries, so files
        // that would push the message over the Exchange ceiling travel as 7-day download links in
        // the cover note instead. The official PDF is ALWAYS attached — it is the document. Linking
        // is best-effort like the file loading itself: if the share store is unconfigured or a link
        // can't be minted, everything stays attached exactly as before and the person reviewing the
        // draft in Outlook sees what they're sending.
        var pdfAttachment = new MailboxDraftAttachment(model.FileName, "application/pdf", pdf);
        var files = await LoadRequestFileAttachmentsAsync(request.RequestId, cancellationToken);
        var coverNote = BuildCoverNote(model);

        var attachments = new List<MailboxDraftAttachment> { pdfAttachment };
        var plan = EmailAttachmentPlanner.Split(files, reservedBytes: pdf.LongLength);
        if (plan.ToLink.Count == 0 || !shareStore.IsConfigured)
        {
            attachments.AddRange(files);
        }
        else
        {
            var links = await TryShareAsync(plan.ToLink, $"{model.TypeShort}-{model.DisplayNumber}", cancellationToken);
            if (links is null)
            {
                attachments.AddRange(files); // linking failed — see the note above
            }
            else
            {
                attachments.AddRange(plan.Attach);
                coverNote += EmailAttachmentPlanner.LinksHtmlBlock(links);
            }
        }

        var draft = new MailboxDraftMessage(
            recipients.To.Select(ToDraftRecipient).ToList(),
            model.EmailSubject,
            coverNote,
            attachments,
            Bcc: recipients.Bcc.Select(ToDraftRecipient).ToList(),
            Categories: new[] { TriageCategories.Marker, recordTag },
            Cc: recipients.Cc.Select(ToDraftRecipient).ToList());

        var filing = new OutboundEmailFiling(
            "Client", StagingRefused, request.ProjectId,
            RecordType.Request, request.RequestId, model.DisplayNumber);

        var dispatch = await dispatcher.DispatchAsync(draft, filing, command.SaveAsDraftOnly, cancellationToken);
        await RequestLifecycle.OpenIfNeedsActionAsync(context, request, cancellationToken);

        return new RequestEmailOutcome(
            request.RequestId,
            model.EmailSubject,
            recipients.To.Select(r => r.Email).ToList(),
            dispatch.WebLink,
            Cc: CopiedRecipients(recipients),
            Bcc: recipients.Bcc.Select(r => r.Email).ToList(),
            DraftMessageId: dispatch.MessageId,
            Sent: dispatch.Sent,
            FailureNote: dispatch.FailureNote);
    }

    /// <summary>The resolved Cc plus the projects mailbox, which the Graph client copies on every
    /// message itself — the confirmation the user reads must match the email that actually went.</summary>
    private List<string> CopiedRecipients(RequestRecipientSet recipients)
    {
        var copied = recipients.Cc.Select(recipient => recipient.Email).ToList();
        if (string.IsNullOrWhiteSpace(mailboxOptions.Mailbox)) return copied;
        var alreadyThere = recipients.To.Concat(recipients.Cc).Concat(recipients.Bcc)
            .Any(recipient => string.Equals(recipient.Email.Trim(), mailboxOptions.Mailbox, StringComparison.OrdinalIgnoreCase));
        if (alreadyThere) return copied;
        copied.Add(mailboxOptions.Mailbox.Trim());
        return copied;
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

    /// <summary>Mints a download link per file, or null if ANY link fails — the caller then attaches
    /// everything instead, because an email promising links it doesn't carry is worse than a draft
    /// Outlook refuses to send (the reviewer can still trim it there).</summary>
    private async Task<List<EmailFileShareLink>?> TryShareAsync(
        IReadOnlyList<MailboxDraftAttachment> toLink, string scope, CancellationToken cancellationToken)
    {
        var links = new List<EmailFileShareLink>();
        foreach (var file in toLink)
        {
            try
            {
                var link = await shareStore.ShareAsync(scope, file.FileName, file.ContentType, file.Content, cancellationToken);
                if (link is null) return null;
                links.Add(link);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return null;
            }
        }
        return links;
    }

    private static MailboxDraftRecipient ToDraftRecipient(CorrespondenceRecipient r) =>
        new(r.Email, string.IsNullOrWhiteSpace(r.Name) ? null : r.Name);

    /// <summary>The short branded HTML cover note — mirrors the worker's outbound send so a drafted
    /// email reads the same as an auto-issued one. Internal so the reply-draft path
    /// (<see cref="SendRequestReplyHandler"/>) reuses the identical note.</summary>
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
