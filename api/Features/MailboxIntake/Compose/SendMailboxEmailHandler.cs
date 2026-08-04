using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Progress.Storage;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Compose;

/// <summary>
/// Triage compose: stage a draft in the projects mailbox and SEND it (docs: the 2026-08-04 send
/// decision, reversing ADR-006's draft-only rule — see the ADR amendment). One handler covers both
/// shapes: a reply inside an existing conversation (Graph's createReplyAll supplies the threading
/// headers and quoted history; the composer's envelope then replaces the scaffolding wholesale) and
/// a brand-new outbound email.
///
/// Failure ordering puts the irreversible step last and keeps every failure recoverable:
///   validate → resolve attachments → (optional) raise request → stage draft (+envelope, categories,
///   attachments) → SEND → tag the inbound thread → audit.
/// A failed SEND therefore triages nothing and loses nothing — the reviewed draft stays in Drafts
/// (outcome Sent=false + webLink, so the user can finish in Outlook). A failed tag AFTER a send is
/// visible (the email is still queued, now with a Replied hint) and idempotently retryable. To-dos
/// raised alongside a compose are a separate command the UI runs first — see TriageQueue.
///
/// Replying IS a triage decision (MarkThreadHandled): after a successful send the inbound thread is
/// tagged JPMS/Replied + pathway — unless the compose also filed it to a record (opt-in
/// AlsoRaiseRequest or LinkRecord), whose tag already says more than "replied".
/// </summary>
public sealed class SendMailboxEmailHandler : ICommandHandler<SendMailboxEmail, ComposeOutcome>
{
    /// <summary>Cap on the combined size of a composed email's attachments — the usual Exchange
    /// message-size ceiling, applied here so a too-big email fails before anything is staged.</summary>
    public const long MaxTotalAttachmentBytes = 25_000_000;

    private readonly JpmsContext context;
    private readonly IMailboxGraphClient graph;
    private readonly IIntakeMessageReader reader;
    private readonly RecordThreadTagger threadTagger;
    private readonly RecordProviderRegistry providers;
    private readonly IDrawingBlobStore drawingBlobs;
    private readonly IProgressPhotoStore photoBlobs;
    private readonly ComposeHtmlPipeline pipeline;
    private readonly AuditTrail audit;
    private readonly ICommandHandler<CreateRequestFromMessage, Request> createRequest;

    public SendMailboxEmailHandler(
        JpmsContext context,
        IMailboxGraphClient graph,
        IIntakeMessageReader reader,
        RecordThreadTagger threadTagger,
        RecordProviderRegistry providers,
        IDrawingBlobStore drawingBlobs,
        IProgressPhotoStore photoBlobs,
        ComposeHtmlPipeline pipeline,
        AuditTrail audit,
        ICommandHandler<CreateRequestFromMessage, Request> createRequest)
    {
        this.context = context;
        this.graph = graph;
        this.reader = reader;
        this.threadTagger = threadTagger;
        this.providers = providers;
        this.drawingBlobs = drawingBlobs;
        this.photoBlobs = photoBlobs;
        this.pipeline = pipeline;
        this.audit = audit;
        this.createRequest = createRequest;
    }

    /// <summary>An uploaded file's bytes, delivered by the endpoint from the multipart request and
    /// matched to a <see cref="ComposeAttachmentRef"/> (Source=Upload) by part name.</summary>
    public sealed record UploadedFile(string FileName, string ContentType, byte[] Content);

    // ICommandHandler shape: a plain JSON call carries no uploads.
    public Task<ComposeOutcome> HandleAsync(SendMailboxEmail command, CancellationToken cancellationToken) =>
        HandleAsync(command, uploads: null, cancellationToken);

    public async Task<ComposeOutcome> HandleAsync(
        SendMailboxEmail command, IReadOnlyDictionary<string, UploadedFile>? uploads, CancellationToken cancellationToken)
    {
        // ---- 1. Validate -------------------------------------------------------------------------
        var isReply = !string.IsNullOrWhiteSpace(command.ReplyToMessageId);
        var subject = command.Subject?.Trim() ?? "";
        var to = CleanRecipients(command.To);
        var cc = CleanRecipients(command.Cc);
        var bcc = CleanRecipients(command.Bcc);

        if (to.Count + cc.Count + bcc.Count == 0)
            throw new InvalidOperationException("Add at least one recipient before sending.");
        if (to.Count == 0)
            throw new InvalidOperationException("Add a To recipient (Cc/Bcc-only emails are refused by most mail servers).");
        if (subject.Length == 0)
            throw new InvalidOperationException("Write a subject before sending.");
        if (string.IsNullOrWhiteSpace(command.Body))
            throw new InvalidOperationException("Write the email before sending.");
        if (command.AlsoRaiseRequest && !isReply)
            throw new InvalidOperationException("A request can only be raised from a reply to an email.");
        if (command.AlsoRaiseRequest && string.IsNullOrWhiteSpace(command.ProjectId))
            throw new InvalidOperationException("Choose the project the request is raised on.");
        if (command.AlsoRaiseRequest && command.LinkRecordType is not null)
            throw new InvalidOperationException("Raise a request or link an existing record — not both in one send.");

        // ---- 2. Read the replied-to email (fresh ids + thread context) ---------------------------
        MailboxSnapshot? snapshot = null;
        if (isReply)
            snapshot = await graph.GetSnapshotAsync(command.ReplyToMessageId!, command.ReplyToInternetMessageId, cancellationToken)
                ?? throw new InvalidOperationException("The email you're replying to could not be read from the mailbox.");

        // Pathway: an existing thread bucket always wins (the composer shows it as fixed); otherwise
        // the triager's explicit choice files the thread when the reply triages it.
        var existingBucket = (snapshot?.Categories ?? Array.Empty<string>())
            .FirstOrDefault(TriageCategories.IsBucketTag);
        var chosenBucket = MapPathway(command.Pathway);
        var effectiveBucket = existingBucket ?? (command.AlsoRaiseRequest ? TriageCategories.Client : chosenBucket);

        var willHandleThread = isReply && command.MarkThreadHandled;
        var filesToRecord = command.AlsoRaiseRequest || command.LinkRecordType is not null;
        if (willHandleThread && !filesToRecord && effectiveBucket is null)
            throw new InvalidOperationException("Choose who this correspondence is with (Client / Subcontractor / Internal) before sending.");

        // ---- 3. Resolve attachments (bytes in hand before anything is created) -------------------
        var attachments = await ResolveAttachmentsAsync(command, uploads, cancellationToken);

        // ---- 4. Body ------------------------------------------------------------------------------
        var composed = command.BodyIsHtml
            ? pipeline.FromHtml(command.Body)
            : new ComposeHtmlPipeline.ComposedBody(ComposeHtmlPipeline.FromPlainText(command.Body), Array.Empty<MailboxDraftAttachment>());
        var allAttachments = attachments.Concat(composed.InlineImages).ToList();
        if (allAttachments.Sum(a => a.Content.LongLength) > MaxTotalAttachmentBytes)
            throw new InvalidOperationException("The attachments total more than 25 MB — remove some, or share large files as drawing links.");

        // ---- 5. Optional record filing (tag-first, verified — the recoverable half) ---------------
        Request? raisedRequest = null;
        string? recordTag = null;
        LinkableRecord? linkedRecord = null;

        if (command.AlsoRaiseRequest)
        {
            // The old "Reply in thread" composite, now opt-in: create the General request exactly as
            // "Create new → Request" would (email + thread tagged first, anchor verified), carrying
            // the written reply as its description.
            raisedRequest = await createRequest.HandleAsync(
                new CreateRequestFromMessage(
                    command.ReplyToMessageId!,
                    command.ProjectId!,
                    RequestType.General,
                    Reference: "",
                    Title: string.IsNullOrWhiteSpace(snapshot!.Subject) ? "(no subject)" : snapshot.Subject.Trim(),
                    Description: $"Replied to email in thread with:\n\n{PlainTextOf(command)}",
                    InternetMessageId: command.ReplyToInternetMessageId ?? snapshot.InternetMessageId,
                    RaisedByEmail: command.SenderEmail),
                cancellationToken);

            recordTag = TriageCategories.ForRecord(
                RequestTags.Stem(
                    await RequestTags.ProjectRefAsync(context, command.ProjectId!, cancellationToken),
                    command.ProjectId!,
                    raisedRequest.Reference.Trim()));
        }
        else if (command.LinkRecordType is { } linkType && !string.IsNullOrWhiteSpace(command.LinkRecordId))
        {
            linkedRecord = await providers.For(linkType).FindAsync(command.LinkRecordId!, cancellationToken)
                ?? throw new InvalidOperationException($"{linkType} record '{command.LinkRecordId}' not found.");
            recordTag = TriageCategories.ForRecord(linkedRecord.TagReference);
            var recordBucket = TriageCategories.BucketFor(linkType) ?? chosenBucket;

            if (existingBucket is not null && recordBucket is not null
                && TriageCategories.CrossesClientWall(existingBucket, recordBucket))
                throw new InvalidOperationException(
                    $"This thread is filed under {AuditTrail.PathwayLabel(existingBucket)}; {linkedRecord.Reference} would file it under {AuditTrail.PathwayLabel(recordBucket)}. "
                    + "Client correspondence is never mixed with subcontractor or internal correspondence.");
            effectiveBucket = existingBucket ?? recordBucket ?? effectiveBucket;

            if (isReply)
            {
                // File the inbound thread to the record now (anchor verified) — same tagging as a
                // triage link, and recoverable: if the send later fails, the thread is filed but
                // unanswered, which the outcome reports honestly.
                var tagged = await threadTagger.TagThreadAsync(
                    command.ReplyToMessageId!, snapshot!.InternetMessageId, snapshot.ConversationId,
                    recordTag, cancellationToken, anchorReceivedAt: snapshot.ReceivedAt);
                if (!tagged)
                    throw new InvalidOperationException("The email couldn't be tagged to the record. Nothing was sent — please try again.");
            }
        }

        // ---- 6. Stage the draft -------------------------------------------------------------------
        // Categories on the draft = what the SENT COPY should carry, so it self-files: the marker +
        // record tag (or Replied for a record-less handled reply) + pathway. A brand-new email with
        // no record chosen carries none — its sent copy simply lives in Sent Items, like any mail
        // sent from Outlook, and replies to it queue as fresh correspondence.
        var draftCategories = new List<string>();
        if (recordTag is not null || willHandleThread)
        {
            draftCategories.Add(TriageCategories.Marker);
            draftCategories.Add(recordTag ?? TriageCategories.Replied);
            if (effectiveBucket is not null) draftCategories.Add(effectiveBucket);
        }
        // (A new email with no record chosen carries no categories at all — a pathway tag without a
        // workflow tag would violate the bucket invariant, and Sent Items never queues anyway.)

        string draftId;
        string? webLink;
        if (isReply)
        {
            var replyDraft = await graph.CreateReplyDraftAsync(
                new MailboxReplyDraftMessage(
                    command.ReplyToMessageId!,
                    HtmlCoverNote: composed.Html,
                    Attachments: allAttachments,
                    Categories: draftCategories.Count == 0 ? null : draftCategories),
                cancellationToken);
            if (replyDraft is null)
            {
                await RollBackRaisedRequestAsync(raisedRequest, recordTag, cancellationToken);
                throw new InvalidOperationException(
                    "The reply couldn't be staged in the projects mailbox, so nothing was sent and nothing was triaged. "
                    + "The original email may no longer be there, or the mailbox connection failed — check and try again.");
            }
            draftId = replyDraft.Id;
            webLink = replyDraft.WebLink;

            // The composer's envelope is authoritative — replace Graph's reply-all scaffolding with
            // exactly what the user saw (the projects mailbox keeps its Cc copy server-side).
            if (!await graph.UpdateDraftEnvelopeAsync(draftId, ToDraft(to), ToDraft(cc), ToDraft(bcc), subject, cancellationToken))
            {
                await RollBackRaisedRequestAsync(raisedRequest, recordTag, cancellationToken);
                throw new InvalidOperationException(
                    "The recipients couldn't be applied to the draft, so nothing was sent. "
                    + "A partial draft may remain in the mailbox's Drafts folder — check and try again.");
            }
        }
        else
        {
            var draft = await graph.CreateDraftAsync(
                new MailboxDraftMessage(
                    ToDraft(to), subject, composed.Html, allAttachments,
                    Bcc: bcc.Count == 0 ? null : ToDraft(bcc),
                    Categories: draftCategories.Count == 0 ? null : draftCategories,
                    Cc: cc.Count == 0 ? null : ToDraft(cc)),
                cancellationToken);
            if (draft is null)
                throw new InvalidOperationException(
                    "The email couldn't be staged in the projects mailbox, so nothing was sent. "
                    + "The mailbox connection may have failed — check and try again.");
            draftId = draft.Id;
            webLink = draft.WebLink;
        }

        var toAddresses = to.Select(r => r.Email).ToList();
        var ccAddresses = cc.Select(r => r.Email).ToList();
        var pathwayLabel = AuditTrail.PathwayLabel(effectiveBucket);
        var projectId = NullIfEmpty(command.ProjectId) ?? NullIfEmpty(linkedRecord?.ProjectId);

        // ---- 7. Save-as-draft stops here ----------------------------------------------------------
        if (command.SaveAsDraftOnly)
        {
            await audit.WriteAsync(
                AuditEventType.DraftCreated,
                $"Draft \"{subject}\" staged for {Recipients(toAddresses, ccAddresses)} — review and send from Outlook.",
                pathway: pathwayLabel,
                projectId: projectId,
                recordType: linkedRecord?.Type ?? (raisedRequest is not null ? RecordType.Request : (RecordType?)null),
                recordId: linkedRecord?.RecordId ?? raisedRequest?.RequestId,
                recordReference: linkedRecord?.Reference ?? raisedRequest?.Reference ?? "",
                conversationId: snapshot?.ConversationId,
                emailMessageId: command.ReplyToMessageId,
                internetMessageId: snapshot?.InternetMessageId,
                webLink: webLink,
                cancellationToken: cancellationToken);

            return new ComposeOutcome(
                draftId, webLink, Sent: false, subject, toAddresses, ccAddresses,
                ThreadHandled: raisedRequest is not null || (isReply && recordTag is not null),
                FailureNote: null,
                RaisedRequest: raisedRequest);
        }

        // ---- 8. SEND — the irreversible step, last ------------------------------------------------
        var sent = await graph.SendDraftAsync(draftId, cancellationToken);
        if (!sent)
        {
            // The reviewed draft survives in Drafts; nothing else changes state here. A raised
            // request / record link (which tagged the thread already) is kept: the words are
            // written and waiting in Outlook, exactly like the old draft-only flow.
            await audit.WriteAsync(
                AuditEventType.EmailSendFailed,
                $"Send failed for \"{subject}\" ({Recipients(toAddresses, ccAddresses)}) — the draft is saved in the mailbox's Drafts folder.",
                pathway: pathwayLabel,
                projectId: projectId,
                conversationId: snapshot?.ConversationId,
                emailMessageId: command.ReplyToMessageId,
                internetMessageId: snapshot?.InternetMessageId,
                webLink: webLink,
                cancellationToken: cancellationToken);

            return new ComposeOutcome(
                draftId, webLink, Sent: false, subject, toAddresses, ccAddresses,
                ThreadHandled: raisedRequest is not null || (isReply && recordTag is not null),
                FailureNote: "The send didn't go through — your email is saved as a draft in the projects mailbox. "
                    + "Open it in Outlook to send it from there, or try again.",
                RaisedRequest: raisedRequest);
        }

        // ---- 9. The reply triages the thread ------------------------------------------------------
        var threadHandled = raisedRequest is not null || (isReply && recordTag is not null);
        if (willHandleThread && recordTag is null && snapshot is not null)
        {
            // Dealt with by answering: JPMS/Replied + pathway across the thread up to the anchor.
            // Best-effort AFTER the send — a failure leaves the email visibly queued (with a
            // Replied hint chip from the sent copy) and re-tagging is idempotent.
            try
            {
                threadHandled = await threadTagger.TagThreadAsync(
                    command.ReplyToMessageId!, snapshot.InternetMessageId, snapshot.ConversationId,
                    TriageCategories.Replied, cancellationToken, anchorReceivedAt: snapshot.ReceivedAt);
                if (threadHandled && effectiveBucket is not null && existingBucket is null)
                    await threadTagger.TagThreadAsync(
                        command.ReplyToMessageId!, snapshot.InternetMessageId, snapshot.ConversationId,
                        effectiveBucket, cancellationToken, anchorReceivedAt: snapshot.ReceivedAt);
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { /* visible + retryable */ }
        }

        // ---- 10. Audit ----------------------------------------------------------------------------
        // Immutable ids: the draft's id stays valid on the sent message, so re-read its webLink to
        // point the audit row at the sent copy.
        var sentWebLink = await graph.GetWebLinkAsync(draftId, cancellationToken) ?? webLink;
        await audit.WriteAsync(
            AuditEventType.EmailSent,
            $"Sent \"{subject}\" {Recipients(toAddresses, ccAddresses)}.",
            pathway: pathwayLabel,
            projectId: projectId,
            recordType: linkedRecord?.Type ?? (raisedRequest is not null ? RecordType.Request : (RecordType?)null),
            recordId: linkedRecord?.RecordId ?? raisedRequest?.RequestId,
            recordReference: linkedRecord?.Reference ?? raisedRequest?.Reference ?? "",
            conversationId: snapshot?.ConversationId,
            emailMessageId: command.ReplyToMessageId,
            internetMessageId: snapshot?.InternetMessageId,
            webLink: sentWebLink,
            cancellationToken: cancellationToken);

        // A raised request whose reply has now been SENT moves Needs action → Open (the ball is
        // with the correspondent) — same rule as the draft flows, only now the send is real.
        if (raisedRequest is not null)
        {
            var raisedEntity = await context.Requests.FirstOrDefaultAsync(r => r.RequestId == raisedRequest.RequestId, cancellationToken);
            if (raisedEntity is not null && (RequestStatus)raisedEntity.Status == RequestStatus.NeedsAction)
            {
                raisedEntity.Status = (int)RequestStatus.Open;
                await context.SaveChangesAsync(cancellationToken);
                raisedRequest = raisedRequest with { Status = RequestStatus.Open };
            }
        }

        return new ComposeOutcome(
            draftId, sentWebLink, Sent: true, subject, toAddresses, ccAddresses,
            ThreadHandled: threadHandled,
            FailureNote: null,
            RaisedRequest: raisedRequest);
    }

    // ---- helpers ---------------------------------------------------------------------------------

    private async Task<List<MailboxDraftAttachment>> ResolveAttachmentsAsync(
        SendMailboxEmail command, IReadOnlyDictionary<string, SendMailboxEmailHandler.UploadedFile>? uploads, CancellationToken ct)
    {
        var resolved = new List<MailboxDraftAttachment>();
        foreach (var reference in command.Attachments ?? Array.Empty<ComposeAttachmentRef>())
        {
            switch (reference.Source)
            {
                case ComposeAttachmentSource.Upload:
                    if (uploads is null || !uploads.TryGetValue(reference.Id, out var file))
                        throw new InvalidOperationException("An attached file didn't arrive with the request — remove it and attach it again.");
                    resolved.Add(new MailboxDraftAttachment(file.FileName, file.ContentType, file.Content));
                    break;

                case ComposeAttachmentSource.Drawing:
                {
                    var revision = await context.DrawingRevisions
                        .FirstOrDefaultAsync(r => r.DrawingRevisionId == reference.Id, ct)
                        ?? throw new InvalidOperationException("A selected drawing revision no longer exists.");
                    var blob = await drawingBlobs.OpenAsync(revision.BlobRef, ct)
                        ?? throw new InvalidOperationException($"The drawing file for {revision.FileName} couldn't be read from storage.");
                    resolved.Add(new MailboxDraftAttachment(
                        revision.FileName, blob.ContentType, await ReadAllAsync(blob.Content, ct)));
                    break;
                }

                case ComposeAttachmentSource.ProgressPhoto:
                {
                    var photo = await context.ProgressPhotos
                        .FirstOrDefaultAsync(p => p.ProgressPhotoId == reference.Id, ct)
                        ?? throw new InvalidOperationException("A selected progress photo no longer exists.");
                    var blob = await photoBlobs.OpenAsync(photo.BlobRef, ct)
                        ?? throw new InvalidOperationException($"The photo file for {photo.FileName} couldn't be read from storage.");
                    resolved.Add(new MailboxDraftAttachment(
                        photo.FileName, blob.ContentType, await ReadAllAsync(blob.Content, ct)));
                    break;
                }

                case ComposeAttachmentSource.OriginalMessage:
                {
                    var sourceMessageId = reference.SourceMessageId ?? command.ReplyToMessageId
                        ?? throw new InvalidOperationException("An original-email attachment needs the message it belongs to.");
                    var content = await reader.GetAttachmentAsync(sourceMessageId, reference.Id, ct)
                        ?? throw new InvalidOperationException("An attachment on the original email couldn't be read from the mailbox.");
                    resolved.Add(new MailboxDraftAttachment(content.Name, content.ContentType, content.Content));
                    break;
                }

                default:
                    throw new InvalidOperationException("Unknown attachment source.");
            }
        }
        return resolved;
    }

    private static async Task<byte[]> ReadAllAsync(Stream stream, CancellationToken ct)
    {
        await using (stream)
        {
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);
            return buffer.ToArray();
        }
    }

    private async Task RollBackRaisedRequestAsync(Request? raised, string? tag, CancellationToken ct)
    {
        if (raised is null) return;
        // Best-effort: pull the tags back off so the email returns to the queue, then delete the
        // request — half-triaged (request created, nothing sent) is worse than not triaged at all.
        if (tag is not null)
            try { await graph.ClearRequestTagsAsync(tag, ct); } catch { /* best-effort */ }
        var entity = await context.Requests.FirstOrDefaultAsync(r => r.RequestId == raised.RequestId, ct);
        if (entity is not null)
        {
            context.Requests.Remove(entity);
            await context.SaveChangesAsync(ct);
        }
    }

    private static List<ComposeRecipient> CleanRecipients(IReadOnlyList<ComposeRecipient>? recipients) =>
        (recipients ?? Array.Empty<ComposeRecipient>())
        .Where(r => !string.IsNullOrWhiteSpace(r.Email) && r.Email.Contains('@'))
        .Select(r => new ComposeRecipient(r.Email.Trim(), string.IsNullOrWhiteSpace(r.Name) ? null : r.Name!.Trim()))
        .GroupBy(r => r.Email, StringComparer.OrdinalIgnoreCase)
        .Select(g => g.First())
        .ToList();

    private static List<MailboxDraftRecipient> ToDraft(IReadOnlyList<ComposeRecipient> recipients) =>
        recipients.Select(r => new MailboxDraftRecipient(r.Email, r.Name)).ToList();

    private static string? MapPathway(string? pathway) =>
        string.Equals(pathway?.Trim(), "Client", StringComparison.OrdinalIgnoreCase) ? TriageCategories.Client
        : string.Equals(pathway?.Trim(), "Subcontractor", StringComparison.OrdinalIgnoreCase) ? TriageCategories.Subcontractor
        : string.Equals(pathway?.Trim(), "Internal", StringComparison.OrdinalIgnoreCase) ? TriageCategories.Internal
        : null;

    private static string Recipients(IReadOnlyList<string> to, IReadOnlyList<string> cc) =>
        cc.Count == 0
            ? $"to {string.Join("; ", to)}"
            : $"to {string.Join("; ", to)} (cc {string.Join("; ", cc)})";

    // The raised request's description wants readable text; an HTML body is stripped to its text.
    private static string PlainTextOf(SendMailboxEmail command)
    {
        if (!command.BodyIsHtml) return command.Body.Trim();
        var text = System.Text.RegularExpressions.Regex.Replace(command.Body, "<br\\s*/?>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        text = System.Text.RegularExpressions.Regex.Replace(text, "</(p|div|li)>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        text = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", "");
        return System.Net.WebUtility.HtmlDecode(text).Trim();
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
