using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components;

namespace Jewel.JPMS.Features.Triage;

// The composer's working state and its two outcomes. Send mode delivers there and then through
// the compose endpoint (the to-do composer and the record pages); stage mode snapshots the
// envelope into a StagedOutboxReply and hands it to the host (the Control Centre's Outbox).
// A reply prefills its envelope reply-all from the anchor's live detail (sender or Reply-To in
// To; original To + Cc in Cc, minus the projects mailbox); a FORWARD (Forward=true) starts with a
// blank envelope and a "FW:" subject — the original email's attachments travel with it
// automatically (Graph's createForward carries them), so the picker doesn't offer them again;
// a new email starts blank.
public partial class MailReplyComposer
{
    /// <summary>The email being replied to — null composes a brand-new outbound email
    /// (send mode only; the Outbox lines up replies, not fresh threads).</summary>
    [Parameter] public MailboxMessage? ReplyTo { get; set; }

    /// <summary>Forward the anchored email instead of replying to it: blank envelope, "FW:"
    /// subject, original attachments carried automatically, and the send never triages the
    /// thread. Only meaningful with <see cref="ReplyTo"/> (a draft carries its own kind).</summary>
    [Parameter] public bool Forward { get; set; }

    /// <summary>A queued reply re-opened for editing (stage mode): the fields load from the
    /// snapshot instead of the reply-all prefill, and confirming updates the same entry.</summary>
    [Parameter] public StagedOutboxReply? Draft { get; set; }

    /// <summary>Project pool for the attachment picker's drawing / photo sources.</summary>
    [Parameter] public IReadOnlyList<Project> Projects { get; set; } = Array.Empty<Project>();
    /// <summary>The project whose drawings/photos the attachment picker offers; also travels on
    /// the send command (it is required there only when AlsoRaiseRequest is set).</summary>
    [Parameter] public string? ProjectId { get; set; }

    // ---- Send mode ----
    /// <summary>Tag the inbound thread Replied (+ pathway) after a successful reply send. Off by
    /// default: every current host replies to mail that is already triaged/tagged.</summary>
    [Parameter] public bool MarkThreadHandled { get; set; }
    /// <summary>File the sent email to a record at compose time — the to-do composer's new-email
    /// branch; a reply usually needs nothing here (it inherits the thread's tags).</summary>
    [Parameter] public RecordType? LinkRecordType { get; set; }
    [Parameter] public string? LinkRecordId { get; set; }
    [Parameter] public EventCallback<ComposeOutcome> OnSent { get; set; }

    // ---- Stage mode (set ⇒ stage; nothing sends from here) ----
    [Parameter] public EventCallback<StagedOutboxReply> OnStage { get; set; }

    [Parameter] public EventCallback OnCancel { get; set; }

    /// <summary>The sentence beside the button — what confirming will actually do, phrased for
    /// where the composer sits. Null falls back to a mode-appropriate default.</summary>
    [Parameter] public string? FooterNote { get; set; }

    private MailboxMessageDetail? anchorDetail;
    private string toField = "";
    private string ccField = "";
    private string bccField = "";
    private bool showBcc;
    private string subject = "";
    private string body = "";
    private IReadOnlyList<ComposeDraftAttachment> attachments = Array.Empty<ComposeDraftAttachment>();
    private bool sending;
    private string? error;

    private bool StageMode => OnStage.HasDelegate;
    private bool IsEditingDraft => Draft is not null;
    private bool IsReply => ReplyTo is not null || Draft is not null;
    private bool IsForward => Draft?.IsForward ?? (Forward && ReplyTo is not null);

    private string? AnchorMessageId => ReplyTo?.Id ?? Draft?.MessageId;
    private string? AnchorInternetMessageId => ReplyTo?.InternetMessageId ?? Draft?.InternetMessageId;

    private string HeaderLabel =>
        StageMode ? $"{KindLabel} — lined up in the Outbox, sent by Apply"
        : IsReply ? $"{KindLabel} — sends from the projects mailbox"
        : "New email — sends from the projects mailbox";

    private string KindLabel => IsForward ? "Forward" : "Reply";

    private string PrimaryLabel =>
        StageMode ? (IsEditingDraft ? "Update in Outbox" : "Line up in Outbox")
        : sending ? "Sending…" : "Send";

    private string DefaultFooterNote =>
        StageMode
            ? $"Nothing sends yet — the {(IsForward ? "forward" : "reply")} waits in the Outbox for this triage's Apply."
            : "The sent email joins the thread from the projects mailbox and files itself by the thread's tags.";

    private bool IsSendable =>
        MailCompose.ParseRecipients(toField).Count > 0
        && !string.IsNullOrWhiteSpace(subject)
        && MailCompose.HtmlHasContent(body);

    protected override async Task OnInitializedAsync()
    {
        if (Draft is { } draft)
        {
            // Editing a queued reply: the snapshot is the truth; the detail fetch below only
            // feeds the attachment picker's "from the original email" list.
            toField = draft.ToField;
            ccField = draft.CcField;
            bccField = draft.BccField;
            showBcc = !string.IsNullOrWhiteSpace(draft.BccField);
            subject = draft.Subject;
            body = draft.Body;
            attachments = draft.Attachments;
        }

        if (AnchorMessageId is not { } anchorId) return;

        // A forward starts with a blank envelope — the whole point is choosing NEW recipients —
        // so its subject can prefill immediately; the detail fetch below only feeds the
        // attachment hint (Graph carries the originals on the forward draft itself).
        if (Draft is null && ReplyTo is { } forwardOf && IsForward)
            subject = MailCompose.ForwardSubjectFor(forwardOf.Subject);

        try
        {
            anchorDetail = await Intake.GetMessageDetailAsync(anchorId, AnchorInternetMessageId);
            if (Draft is null && ReplyTo is { } replyTo && !IsForward) PrefillReplyEnvelope(replyTo, anchorDetail);
        }
        catch
        {
            // The live detail also feeds the reply-all Cc set; without it the reply still works,
            // addressed to the sender alone. (A forward's envelope was never going to prefill.)
            if (Draft is null && ReplyTo is { } replyTo && !IsForward)
            {
                toField = replyTo.FromEmail;
                subject = MailCompose.ReplySubjectFor(replyTo.Subject);
            }
        }
    }

    private void PrefillReplyEnvelope(MailboxMessage replyTo, MailboxMessageDetail loaded)
    {
        var toAddress = loaded.ReplyTo ?? loaded.FromEmail ?? replyTo.FromEmail;
        toField = toAddress ?? "";
        var ccAddresses = (loaded.To ?? Array.Empty<string>())
            .Concat(loaded.Cc ?? Array.Empty<string>())
            .Where(address => !string.IsNullOrWhiteSpace(address))
            .Where(address => !address.Equals(toAddress, StringComparison.OrdinalIgnoreCase))
            .Where(address => loaded.MailboxAddress is null
                || !address.Equals(loaded.MailboxAddress, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        ccField = string.Join("; ", ccAddresses);
        subject = MailCompose.ReplySubjectFor(loaded.Subject ?? replyTo.Subject);
    }

    private Task ConfirmAsync() => StageMode ? StageAsync() : SendAsync();

    // Stage: snapshot the envelope into the (new or edited) Outbox entry — no network, no send.
    private async Task StageAsync()
    {
        if (!IsSendable) return;
        var entry = Draft ?? new StagedOutboxReply
        {
            IsForward = IsForward,
            MessageId = ReplyTo!.Id,
            InternetMessageId = ReplyTo.InternetMessageId,
            AnchorSubject = ReplyTo.Subject,
            AnchorFrom = string.IsNullOrWhiteSpace(ReplyTo.FromName) ? ReplyTo.FromEmail : ReplyTo.FromName,
            AnchorReceivedAt = ReplyTo.ReceivedAt,
            AnchorTags = ReplyTo.Categories
        };
        entry.ToField = toField;
        entry.CcField = ccField;
        entry.BccField = bccField;
        entry.Subject = subject.Trim();
        entry.Body = body;
        entry.Attachments = attachments;
        await OnStage.InvokeAsync(entry);
    }

    private async Task SendAsync()
    {
        if (sending || !IsSendable) return;
        error = null;
        sending = true;
        try
        {
            var command = new SendMailboxEmail(
                ReplyToMessageId: AnchorMessageId,
                ReplyToInternetMessageId: AnchorInternetMessageId,
                To: MailCompose.ParseRecipients(toField),
                Cc: MailCompose.ParseRecipients(ccField),
                Bcc: MailCompose.ParseRecipients(bccField),
                Subject: subject.Trim(),
                Body: body,
                BodyIsHtml: true,
                Attachments: attachments.Select(attachment => attachment.ToRef()).ToList(),
                MarkThreadHandled: MarkThreadHandled && !IsForward,
                LinkRecordType: LinkRecordType,
                LinkRecordId: LinkRecordId,
                ProjectId: string.IsNullOrWhiteSpace(ProjectId) ? null : ProjectId,
                Forward: IsForward);
            var outcome = await Intake.SendComposedEmailAsync(command, MailCompose.UploadPartsOf(attachments));
            await OnSent.InvokeAsync(outcome);
        }
        catch (CommandFailedException ex) { error = ex.Message; }
        catch { error = "The send didn't complete. Please try again."; }
        finally { sending = false; }
    }
}
