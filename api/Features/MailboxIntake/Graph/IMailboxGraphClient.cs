using System.Net;
using System.Net.Http.Headers;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Graph;
/// <summary>
/// Live mailbox access for triage, category-based: read folder pages filtered by category, and tag
/// messages. Every tag operation is <b>verified</b> — it writes the categories and then reads them
/// back, only reporting success if the change actually stuck. Nothing is ever moved, and the one
/// delete (<see cref="DeleteDraftAsync"/>) refuses anything Graph does not itself report as an
/// unsent draft — so duplication and lost mail remain impossible by construction.
///
/// All calls request immutable ids (<c>Prefer: IdType="ImmutableId"</c>) so a message id stays valid.
/// </summary>
public interface IMailboxGraphClient
{
    /// <summary>One page of the triage queue: Inbox messages NOT tagged triaged, oldest first by
    /// default (so the backlog is cleared from page one); newestFirst flips the read for triagers
    /// who want to see what's just arrived.</summary>
    Task<MailboxPage> ListInboxAsync(string? cursor, int take, bool newestFirst, CancellationToken ct);

    /// <summary>One page of the discarded pile: Inbox messages tagged discarded, oldest first by
    /// default; newestFirst flips the read.</summary>
    Task<MailboxPage> ListDiscardedAsync(string? cursor, int take, bool newestFirst, CancellationToken ct);

    /// <summary>One page of the emails tagged to a specific record (its workflow tag), oldest first.
    /// This is how a record reads its associated emails live — no copies are stored. Spans the whole
    /// mailbox (not just the Inbox) so the mailbox's own tagged sent copies are included; unsent
    /// drafts never surface.</summary>
    Task<MailboxPage> ListByTagAsync(string tag, string? cursor, int take, CancellationToken ct);

    /// <summary>One page of every tagged email (anything carrying the JPMS marker), oldest first by
    /// default (newestFirst flips it) — the management surface for the Tagged tab. Mailbox-wide,
    /// drafts excluded.</summary>
    Task<MailboxPage> ListTaggedAsync(string? cursor, int take, bool newestFirst, CancellationToken ct);

    /// <summary>Free-text search across the WHOLE mailbox (Graph $search over subjects, bodies,
    /// senders and attachment names), relevance-ordered, one page — the record pages' "Find
    /// emails" dialog. Drafts excluded, like every other read.</summary>
    Task<MailboxPage> SearchAsync(string query, int take, CancellationToken ct);

    /// <summary>Every message in one Graph conversation (the email + its replies/forwards), oldest
    /// first and regardless of tags — so a triage view can show an email's whole thread. Mailbox-wide:
    /// the mailbox's own sent replies (which never arrive back in the Inbox) take their place in the
    /// thread; unsent drafts are excluded. Not paged: a mail thread is small, so a single (capped)
    /// read returns it whole.</summary>
    Task<MailboxPage> ListConversationAsync(string conversationId, CancellationToken ct);

    /// <summary>One page of emails carrying ANY of the given workflow tags (an OR filter), oldest
    /// first by default (newestFirst flips it) — backs the Tagged tab's multi-select filter.</summary>
    Task<MailboxPage> ListByTagsAsync(IReadOnlyList<string> tags, string? cursor, int take, bool newestFirst, CancellationToken ct);

    /// <summary>Remove a single workflow tag from an email; if it was the last one, the email returns
    /// to the triage queue (the marker is dropped too). Verified by read-back.</summary>
    Task<bool> RemoveTagAsync(string messageId, string? internetMessageId, string tag, CancellationToken ct);

    /// <summary>Tag an email triaged + discarded. Returns true only once the tags are read back present.</summary>
    Task<bool> DiscardAsync(string messageId, string? internetMessageId, CancellationToken ct);

    /// <summary>Remove the triaged + discarded tags (undo a discard). Verified by read-back.</summary>
    Task<bool> RestoreAsync(string messageId, string? internetMessageId, CancellationToken ct);

    /// <summary>Tag an email triaged + assigned to the given request category. Verified by read-back.</summary>
    Task<bool> AssignAsync(string messageId, string? internetMessageId, string requestCategory, CancellationToken ct);

    /// <summary>Remove the triaged + request tags from every email assigned to a request (return-to-triage).
    /// Returns how many were cleared.</summary>
    Task<int> ClearRequestTagsAsync(string requestCategory, CancellationToken ct);

    /// <summary>Move every email carrying <paramref name="oldCategory"/> onto <paramref name="newCategory"/>
    /// — used when a record's reference is renamed so its linked correspondence follows the new tag. The
    /// new tag is added before the old one is removed, so an email never loses its marker mid-move (and so
    /// never bounces back to triage). Verified per message. Returns how many were retagged.</summary>
    Task<int> RetagAsync(string oldCategory, string newCategory, CancellationToken ct);

    /// <summary>Add <paramref name="aliasCategory"/> to every email carrying <paramref name="existingCategory"/>,
    /// KEEPING the existing tag in place — used when a record gains a new reference on RFI promotion
    /// (REQ-#### → RFI-NNN): the immutable REQ tag stays on the mail as a permanent audit alias while
    /// live reads follow the new tag. Verified per message. Returns how many emails gained the alias.</summary>
    Task<int> AddAliasTagAsync(string existingCategory, string aliasCategory, CancellationToken ct);

    /// <summary>Read the fields needed to record an email against a request, or null if it's gone.</summary>
    Task<MailboxSnapshot?> GetSnapshotAsync(string messageId, string? internetMessageId, CancellationToken ct);

    /// <summary>Message ids in the given conversation that do NOT yet carry the category — i.e. the
    /// thread members still to be tagged (e.g. replies that arrived after the original link).
    /// Mailbox-wide; unsent drafts are skipped. The category test is applied client-side: the
    /// all-mailbox view only reliably supports the plain conversationId filter.
    /// receivedOnOrBefore (when given) restricts the sweep to members received at or before that
    /// moment — a triage decision covers the thread UP TO the email it was made on; anything that
    /// arrived after it queues for its own decision, even when it was already sitting in the
    /// mailbox when the decision landed.</summary>
    Task<IReadOnlyList<string>> ListUntaggedIdsInConversationAsync(string conversationId, string category, CancellationToken ct, DateTimeOffset? receivedOnOrBefore = null);

    /// <summary>Message ids in the given conversation that currently carry the category — i.e. the
    /// thread members to un-tag when reversing a thread-wide tag (e.g. restoring a discarded thread).
    /// Mailbox-wide; unsent drafts are skipped (a tagged draft is the drafting flow's to manage).</summary>
    Task<IReadOnlyList<string>> ListTaggedIdsInConversationAsync(string conversationId, string category, CancellationToken ct);

    /// <summary>Add a workflow category (plus the JPMS marker) to every conversation member that
    /// doesn't yet carry it, using Graph JSON batching (20 PATCHes per round-trip) — the fast path
    /// for thread-wide sweeps. A long thread must not cost several Graph round-trips per member:
    /// the hosting platform's 45-second request ceiling turns per-message walks over big threads
    /// into guaranteed 500s (JPMS-2B6023 — a 56-email thread ≈ 340 sequential calls).
    /// <paramref name="receivedOnOrBefore"/> restricts the sweep exactly as
    /// <see cref="ListUntaggedIdsInConversationAsync"/> does. Best-effort per member with no
    /// read-back verification — callers verify their ANCHOR message individually via
    /// <see cref="AssignAsync"/>; the sweep is the same best-effort covering the anchor's thread.
    /// Returns how many members were patched successfully.</summary>
    Task<int> TagConversationMembersAsync(string conversationId, string category, CancellationToken ct, DateTimeOffset? receivedOnOrBefore = null);

    /// <summary>Remove a workflow category from every conversation member that carries it, using
    /// Graph JSON batching — the fast inverse of <see cref="TagConversationMembersAsync"/>. A member
    /// left with no workflow tag also loses its pathway tags and the marker (back to the triage
    /// queue), mirroring <see cref="RemoveTagAsync"/>. Best-effort per member; returns how many
    /// members were patched successfully.</summary>
    Task<int> UntagConversationMembersAsync(string conversationId, string category, CancellationToken ct);

    /// <summary>
    /// Create a draft message in the mailbox's Drafts folder — recipients, subject, HTML body and
    /// attachments all pre-filled — for a person to review and send from the mailbox itself.
    /// Nothing is sent. Returns the draft's identity (with a webLink to open it in Outlook on the
    /// web when Graph provides one), or null when the mailbox is unconfigured / the create failed.
    /// </summary>
    Task<MailboxDraft?> CreateDraftAsync(MailboxDraftMessage draft, CancellationToken ct);

    /// <summary>
    /// Create a draft REPLY to an existing mailbox message — same conversation thread, "RE:" subject,
    /// original recipients and quoted history all supplied by Graph (createReplyAll) — then prepend
    /// the given cover note above the quoted history, apply the workflow categories and attach the
    /// files. The recipient's mail client threads the sent copy under the existing conversation.
    /// With <see cref="MailboxReplyDraftMessage.Forward"/> set the draft is a FORWARD instead
    /// (createForward): "FW:" subject, no recipients pre-filled, quoted history — and the original
    /// message's attachments are carried onto the draft by Graph itself, so callers must not attach
    /// them again. Nothing is sent. Returns null when the mailbox is unconfigured, the original
    /// message is gone, or a step failed (a partially prepared draft may remain in Drafts for a
    /// human to inspect).
    /// </summary>
    Task<MailboxReplyDraft?> CreateReplyDraftAsync(MailboxReplyDraftMessage reply, CancellationToken ct);

    /// <summary>
    /// Replace a staged draft's envelope — To/Cc/Bcc/Subject — with exactly what the composer
    /// submitted. The visible envelope is authoritative: Graph's createReplyAll recipients are only
    /// scaffolding for the threading headers and quoted history, and nothing is added behind the
    /// composer's back (the projects mailbox is never auto-Cc'd — a delivered copy would land back
    /// in the triage queue). Verified by response status; returns false on failure (the draft is
    /// left as it was).
    /// </summary>
    Task<bool> UpdateDraftEnvelopeAsync(
        string draftMessageId,
        IReadOnlyList<MailboxDraftRecipient> to,
        IReadOnlyList<MailboxDraftRecipient> cc,
        IReadOnlyList<MailboxDraftRecipient> bcc,
        string subject,
        CancellationToken ct);

    /// <summary>
    /// SEND a staged draft (<c>POST …/messages/{id}/send</c>). This is the single send chokepoint
    /// in the whole system — every outbound email still passes through the draft plumbing above
    /// (category stamping, large-attachment upload sessions), and only
    /// this one call, made on an explicit human "Send" in the portal, moves it to Sent Items.
    /// No agent tool is wired to it. Needs the Mail.Send application permission (decision
    /// 2026-08-04, reversing ADR-006's draft-only rule); without consent Graph returns 403 and the
    /// caller degrades to "draft saved". Retries once on 429 honouring Retry-After. Because every
    /// call requests immutable ids, the draft's id remains valid on the sent message.
    /// </summary>
    Task<bool> SendDraftAsync(string draftMessageId, CancellationToken ct);

    /// <summary>Read one message's webLink (e.g. re-reading a just-sent message so the audit row
    /// can link to the sent copy), or null if unavailable.</summary>
    Task<string?> GetWebLinkAsync(string messageId, CancellationToken ct);

    /// <summary>
    /// DELETE one unsent draft (<c>DELETE …/messages/{id}</c>) — the undo for the draft-staging
    /// calls above when a staged draft was superseded or raised in error. The one guarded
    /// exception to this client's no-delete rule: the message is read back first and the delete is
    /// REFUSED (<see cref="MailboxDraftDeleteOutcome.NotADraft"/>) unless Graph itself reports
    /// <c>isDraft: true</c>, so delivered or sent mail can never be removed through this client
    /// whatever id it is handed. Graph moves the deleted draft to Deleted Items rather than wiping
    /// it, so a mistaken delete is recoverable from Outlook for a while.
    /// </summary>
    Task<MailboxDraftDeletion> DeleteDraftAsync(string draftMessageId, CancellationToken ct);
}

/// <summary>How a draft delete ended — precise, so callers can answer honestly. NotADraft means
/// the id resolved to a message that is NOT an unsent draft (sent/received mail; nothing was
/// touched); NotFound means no message with that id (already deleted, or never existed); Failed
/// means the mailbox is unconfigured or Graph refused.</summary>
public enum MailboxDraftDeleteOutcome { Deleted, NotADraft, NotFound, Failed }

/// <summary>The result of <see cref="IMailboxGraphClient.DeleteDraftAsync"/>: the outcome, plus
/// the message's subject when the read-back resolved one (so callers can name what was — or was
/// not — deleted).</summary>
public sealed record MailboxDraftDeletion(MailboxDraftDeleteOutcome Outcome, string? Subject = null);

/// <summary>A new message for the mailbox, placed in Drafts via CreateDraftAsync. Cc, Bcc and
/// Categories are optional so existing draft-only callers are unchanged (Cc sits last purely to
/// preserve older positional constructions).</summary>
public sealed record MailboxDraftMessage(
    IReadOnlyList<MailboxDraftRecipient> To,
    string Subject,
    string HtmlBody,
    IReadOnlyList<MailboxDraftAttachment> Attachments,
    IReadOnlyList<MailboxDraftRecipient>? Bcc = null,
    IReadOnlyList<string>? Categories = null,
    IReadOnlyList<MailboxDraftRecipient>? Cc = null);

/// <summary>A draft recipient (address plus optional display name).</summary>
public sealed record MailboxDraftRecipient(string Email, string? Name = null);

/// <summary>A file attached to a draft, sent as a Graph fileAttachment (base64 contentBytes).
/// IsInline + ContentId mark an image embedded in the HTML body (referenced as
/// <c>src="cid:{ContentId}"</c> — how pasted screenshots travel); both default off so every
/// existing caller is unchanged.</summary>
public sealed record MailboxDraftAttachment(
    string FileName, string ContentType, byte[] Content, bool IsInline = false, string? ContentId = null);

/// <summary>A created draft: its Graph id and (usually) a webLink that opens it in Outlook on the web.</summary>
public sealed record MailboxDraft(string Id, string? WebLink);

/// <summary>A reply-draft to stage on an existing mailbox message: the cover note goes above the
/// quoted history Graph supplies; recipients come from the original message (reply-all). Forward
/// stages a forward draft instead (createForward — "FW:" subject, no recipients, and the original
/// attachments carried over by Graph; Attachments here are EXTRA files on top of those).</summary>
public sealed record MailboxReplyDraftMessage(
    string MessageId,
    string HtmlCoverNote,
    IReadOnlyList<MailboxDraftAttachment> Attachments,
    IReadOnlyList<string>? Categories = null,
    bool Forward = false);

/// <summary>A created reply draft: identity plus the subject and recipients Graph pre-filled from
/// the original message, so callers can report who the reply is addressed to.</summary>
public sealed record MailboxReplyDraft(
    string Id,
    string? WebLink,
    string Subject,
    IReadOnlyList<string> To,
    IReadOnlyList<string> Cc);

/// <summary>The subset of a mailbox message recorded against a request when an email is assigned.</summary>
public sealed record MailboxSnapshot(
    string InternetMessageId,
    string? ConversationId,
    string? InReplyTo,
    string FromEmail,
    string FromName,
    string Subject,
    string BodyPreview,
    DateTimeOffset ReceivedAt,
    // The message's current raw categories (marker, record tags, pathway tags, user categories).
    // Read so the pathway guards can see which bucket a thread already carries before tagging.
    IReadOnlyList<string>? Categories = null);

