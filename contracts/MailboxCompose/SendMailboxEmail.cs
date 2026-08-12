using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.MailboxCompose;

// Triage compose: send an email from the shared projects mailbox — a REPLY inside an existing
// conversation (ReplyToMessageId set) or a brand-new outbound thread (ReplyToMessageId null).
// The visible envelope is authoritative: whatever To/Cc/Bcc/Subject the composer shows is exactly
// what goes on the wire (the server re-applies it to the staged draft before sending); the projects
// mailbox is auto-Cc'd server-side so the thread stays complete in one place.
//
// Sending is real (decision 2026-08-04, reversing the draft-only rule of ADR-006): pressing Send
// delivers the email there and then. SaveAsDraftOnly stops after staging, leaving the reviewed
// draft in the mailbox's Drafts folder for Outlook — the old behaviour, kept as an explicit choice.
// A failed send also degrades to that: the draft survives, nothing is triaged, and the outcome says
// so, so an email can never be lost between the portal and the mailbox.
//
// A reply is also a triage decision: with MarkThreadHandled (default), the inbound thread is tagged
// JPMS/Replied plus the chosen pathway once the send succeeds, so the email leaves the queue —
// dealing with an email by answering it is as real a decision as filing it. Optionally the reply
// can additionally be filed to an existing record (LinkRecordType/LinkRecordId) or raise a General
// request carrying the reply as its description (AlsoRaiseRequest — the old "Reply in thread"
// composite, now opt-in). SenderEmail is stamped server-side from the signed-in user.
public sealed record SendMailboxEmail(
    // Reply target, or null for a brand-new email. InternetMessageId re-finds the message if its
    // Graph id changed since the list was rendered.
    string? ReplyToMessageId,
    string? ReplyToInternetMessageId,
    IReadOnlyList<ComposeRecipient> To,
    IReadOnlyList<ComposeRecipient> Cc,
    IReadOnlyList<ComposeRecipient> Bcc,
    string Subject,
    // The composed body. BodyIsHtml=false means plain textarea text (encoded line-by-line
    // server-side); true means composer HTML, which the server sanitises and whose pasted
    // data:image/* images become inline cid attachments.
    string Body,
    bool BodyIsHtml,
    // Attachments by reference — uploaded files travel as multipart parts named by Id; drawings,
    // progress photos, system record documents and the original email's own attachments are
    // resolved server-side by id.
    IReadOnlyList<ComposeAttachmentRef> Attachments,
    // Stop after staging the draft (review + send from Outlook) instead of sending.
    bool SaveAsDraftOnly = false,
    // "Client" / "Subcontractor" / "Internal" — the pathway the thread files under when the reply
    // triages it. Required when MarkThreadHandled is set on a reply; the client wall applies.
    string? Pathway = null,
    // Tag the inbound thread JPMS/Replied (+ pathway) after a successful reply send, so it leaves
    // the triage queue. Ignored for new emails (nothing is queued to handle).
    bool MarkThreadHandled = true,
    // Optionally file the thread to an existing record at compose time (same tagging as a triage
    // link, wall/lane guards included).
    RecordType? LinkRecordType = null,
    string? LinkRecordId = null,
    // Additionally raise a General request from the replied-to email, carrying the reply as its
    // description — the old ReplyInThreadFromMessage composite, now an explicit choice.
    bool AlsoRaiseRequest = false,
    // Required when AlsoRaiseRequest: the project the request is raised on.
    string? ProjectId = null,
    // Stamped server-side from the signed-in user; the client cannot spoof it.
    string SenderEmail = "") : ICommand<ComposeOutcome>;

/// <summary>A composer recipient (address plus optional display name).</summary>
public sealed record ComposeRecipient(string Email, string? Name = null);

/// <summary>Where a composed attachment's bytes come from.</summary>
public enum ComposeAttachmentSource
{
    /// <summary>A file uploaded from the user's computer — travels as a multipart part named by Id.</summary>
    Upload = 0,
    /// <summary>A drawing revision from the project's drawing register — Id is the DrawingRevisionId.</summary>
    Drawing = 1,
    /// <summary>A progress photo from the project's progress updates — Id is the ProgressPhotoId.</summary>
    ProgressPhoto = 2,
    /// <summary>An attachment on a mailbox message (forwarding it on) — Id is the Graph attachment
    /// id, SourceMessageId the Graph message id it belongs to.</summary>
    OriginalMessage = 3,
    /// <summary>The official PDF of a system record, rendered server-side at send time so the
    /// attached document is always the record as it currently stands. Id is the record id;
    /// RecordType says which type. The request family (RFI/NOD/EOT…) is the only type with an
    /// official document so far — extend the resolver as more record types grow one.</summary>
    RecordDocument = 4
}

/// <summary>One attachment on a composed email, by reference (bytes are resolved server-side —
/// uploads from the multipart request, the rest from blob storage / the mailbox / the record's
/// document renderer).</summary>
public sealed record ComposeAttachmentRef(
    ComposeAttachmentSource Source,
    string Id,
    string? SourceMessageId = null,
    // Display-only: the file name the composer showed (the server re-derives the real one).
    string FileName = "",
    // RecordDocument only: the record type Id belongs to, so the server knows which renderer.
    RecordType? RecordType = null);

/// <summary>
/// What happened to a composed email. Sent=true means it was delivered from the projects mailbox;
/// Sent=false means it is staged in the mailbox's Drafts folder (either by choice — SaveAsDraftOnly
/// — or because the send failed after staging; FailureNote says which). WebLink opens the message
/// in Outlook on the web. ThreadHandled reports whether the inbound thread was tagged out of the
/// queue; RaisedRequest carries the General request when AlsoRaiseRequest created one.
/// </summary>
public sealed record ComposeOutcome(
    string MessageId,
    string? WebLink,
    bool Sent,
    string Subject,
    IReadOnlyList<string> To,
    IReadOnlyList<string> Cc,
    bool ThreadHandled = false,
    string? FailureNote = null,
    Request? RaisedRequest = null);
