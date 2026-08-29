using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Jewel.JPMS.Models;
using Microsoft.Extensions.Logging;

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

/// <summary>No-op client used when Graph credentials aren't configured: triage shows empty and tag
/// operations report failure (so the UI shows an error rather than a false success).</summary>
public sealed class NullMailboxGraphClient : IMailboxGraphClient
{
    public Task<MailboxPage> ListInboxAsync(string? cursor, int take, bool newestFirst, CancellationToken ct) =>
        Task.FromResult(new MailboxPage(Array.Empty<MailboxMessage>(), null, 0));
    public Task<MailboxPage> ListDiscardedAsync(string? cursor, int take, bool newestFirst, CancellationToken ct) =>
        Task.FromResult(new MailboxPage(Array.Empty<MailboxMessage>(), null, 0));
    public Task<MailboxPage> ListByTagAsync(string tag, string? cursor, int take, CancellationToken ct) =>
        Task.FromResult(new MailboxPage(Array.Empty<MailboxMessage>(), null, 0));
    public Task<MailboxPage> ListTaggedAsync(string? cursor, int take, bool newestFirst, CancellationToken ct) =>
        Task.FromResult(new MailboxPage(Array.Empty<MailboxMessage>(), null, 0));
    public Task<MailboxPage> SearchAsync(string query, int take, CancellationToken ct) =>
        Task.FromResult(new MailboxPage(Array.Empty<MailboxMessage>(), null, 0));
    public Task<MailboxPage> ListByTagsAsync(IReadOnlyList<string> tags, string? cursor, int take, bool newestFirst, CancellationToken ct) =>
        Task.FromResult(new MailboxPage(Array.Empty<MailboxMessage>(), null, 0));
    public Task<MailboxPage> ListConversationAsync(string conversationId, CancellationToken ct) =>
        Task.FromResult(new MailboxPage(Array.Empty<MailboxMessage>(), null, 0));
    public Task<bool> RemoveTagAsync(string messageId, string? internetMessageId, string tag, CancellationToken ct) => Task.FromResult(false);
    public Task<bool> DiscardAsync(string messageId, string? internetMessageId, CancellationToken ct) => Task.FromResult(false);
    public Task<bool> RestoreAsync(string messageId, string? internetMessageId, CancellationToken ct) => Task.FromResult(false);
    public Task<bool> AssignAsync(string messageId, string? internetMessageId, string requestCategory, CancellationToken ct) => Task.FromResult(false);
    public Task<int> ClearRequestTagsAsync(string requestCategory, CancellationToken ct) => Task.FromResult(0);
    public Task<int> RetagAsync(string oldCategory, string newCategory, CancellationToken ct) => Task.FromResult(0);
    public Task<int> AddAliasTagAsync(string existingCategory, string aliasCategory, CancellationToken ct) => Task.FromResult(0);
    public Task<MailboxSnapshot?> GetSnapshotAsync(string messageId, string? internetMessageId, CancellationToken ct) => Task.FromResult<MailboxSnapshot?>(null);
    public Task<IReadOnlyList<string>> ListUntaggedIdsInConversationAsync(string conversationId, string category, CancellationToken ct, DateTimeOffset? receivedOnOrBefore = null) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    public Task<IReadOnlyList<string>> ListTaggedIdsInConversationAsync(string conversationId, string category, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    public Task<int> TagConversationMembersAsync(string conversationId, string category, CancellationToken ct, DateTimeOffset? receivedOnOrBefore = null) =>
        Task.FromResult(0);
    public Task<int> UntagConversationMembersAsync(string conversationId, string category, CancellationToken ct) =>
        Task.FromResult(0);
    public Task<MailboxDraft?> CreateDraftAsync(MailboxDraftMessage draft, CancellationToken ct) =>
        Task.FromResult<MailboxDraft?>(null);
    public Task<MailboxReplyDraft?> CreateReplyDraftAsync(MailboxReplyDraftMessage reply, CancellationToken ct) =>
        Task.FromResult<MailboxReplyDraft?>(null);
    public Task<bool> UpdateDraftEnvelopeAsync(string draftMessageId, IReadOnlyList<MailboxDraftRecipient> to,
        IReadOnlyList<MailboxDraftRecipient> cc, IReadOnlyList<MailboxDraftRecipient> bcc, string subject, CancellationToken ct) =>
        Task.FromResult(false);
    public Task<bool> SendDraftAsync(string draftMessageId, CancellationToken ct) => Task.FromResult(false);
    public Task<string?> GetWebLinkAsync(string messageId, CancellationToken ct) => Task.FromResult<string?>(null);
    public Task<MailboxDraftDeletion> DeleteDraftAsync(string draftMessageId, CancellationToken ct) =>
        Task.FromResult(new MailboxDraftDeletion(MailboxDraftDeleteOutcome.Failed));
}

/// <summary>Graph REST implementation (HttpClient + app-only token).</summary>
public sealed class MailboxGraphClient : IMailboxGraphClient
{
    private const string GraphBase = "https://graph.microsoft.com/v1.0";
    private const string Summary =
        "id,internetMessageId,conversationId,subject,bodyPreview,from,receivedDateTime,hasAttachments,categories,isDraft";

    private readonly HttpClient _http;
    private readonly GraphTokenProvider _tokens;
    private readonly MailboxIntakeOptions _options;
    private readonly ILogger<MailboxGraphClient> _logger;

    public MailboxGraphClient(
        HttpClient http, GraphTokenProvider tokens, MailboxIntakeOptions options, ILogger<MailboxGraphClient> logger)
    {
        _http = http;
        _tokens = tokens;
        _options = options;
        _logger = logger;
    }

    private string Mailbox => Uri.EscapeDataString(_options.Mailbox);

    // The triage queue and discarded pile are Inbox views by definition — the mailbox's own sent
    // copies are never "to be triaged". The tag/conversation reads below span the WHOLE mailbox
    // (Sent Items included) so a reply sent from the project mailbox itself appears in a record's
    // correspondence: a sent message never arrives back in the Inbox, so an inbox-scoped read would
    // silently drop the outbound leg of every thread. Unsent drafts are excluded client-side.

    public Task<MailboxPage> ListInboxAsync(string? cursor, int take, bool newestFirst, CancellationToken ct) =>
        ListFilteredAsync($"not categories/any(c:c eq '{TriageCategories.Marker}')", cursor, take, inboxOnly: true, newestFirst, ct);

    public Task<MailboxPage> ListDiscardedAsync(string? cursor, int take, bool newestFirst, CancellationToken ct) =>
        ListFilteredAsync($"categories/any(c:c eq '{TriageCategories.Discarded}')", cursor, take, inboxOnly: true, newestFirst, ct);

    public Task<MailboxPage> ListByTagAsync(string tag, string? cursor, int take, CancellationToken ct) =>
        ListFilteredAsync($"categories/any(c:c eq '{tag}')", cursor, take, inboxOnly: false, newestFirst: false, ct);

    public Task<MailboxPage> ListTaggedAsync(string? cursor, int take, bool newestFirst, CancellationToken ct) =>
        ListFilteredAsync($"categories/any(c:c eq '{TriageCategories.Marker}')", cursor, take, inboxOnly: false, newestFirst, ct);

    public Task<MailboxPage> ListByTagsAsync(IReadOnlyList<string> tags, string? cursor, int take, bool newestFirst, CancellationToken ct)
    {
        if (tags.Count == 0)
            return ListTaggedAsync(cursor, take, newestFirst, ct);
        // OR the per-tag category filters: an email matching any selected tag is included. Single-quotes
        // in a category are escaped by doubling, per OData.
        var filter = string.Join(" or ",
            tags.Select(t => $"categories/any(c:c eq '{t.Replace("'", "''")}')"));
        return ListFilteredAsync(filter, cursor, take, inboxOnly: false, newestFirst, ct);
    }

    public async Task<MailboxPage> ListConversationAsync(string conversationId, CancellationToken ct)
    {
        // No category clause: the thread view wants every member — still-queued, discarded and
        // already-linked alike (their tags tell the triager what's been decided so far). Reads the
        // WHOLE mailbox, not just the Inbox, so the mailbox's own sent replies (Sent Items) take
        // their place in the thread; unsent drafts are skipped. Unlike the category lists this must
        // NOT use $orderby: Graph rejects a conversationId filter combined with $orderby as an
        // inefficient filter. A thread is small, so one max-size page covers it and we sort
        // oldest-first here instead.
        var filter = $"conversationId eq '{conversationId.Replace("'", "''")}'";
        var url = $"{GraphBase}/users/{Mailbox}/messages"
            + $"?$filter={Uri.EscapeDataString(filter)}"
            + $"&$select={Summary}&$top=100";

        var items = new List<MailboxMessage>();
        using var response = await SendAsync(HttpMethod.Get, url, content: null, ct, allowNotFound: true, consistencyEventual: true);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Conversation list failed: {Status}. {Detail}",
                (int)response.StatusCode, await SafeBodyAsync(response, ct));
            return new MailboxPage(items, null, 0);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (doc.RootElement.TryGetProperty("value", out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var item in arr.EnumerateArray())
                if (!IsDraft(item) && Parse(item) is { } m)
                    items.Add(m);

        items.Sort((a, b) => a.ReceivedAt.CompareTo(b.ReceivedAt));
        return new MailboxPage(items, null, items.Count);
    }

    public async Task<MailboxPage> SearchAsync(string query, int take, CancellationToken ct)
    {
        take = Math.Clamp(take, 1, 50);

        // Graph's message $search takes a KQL phrase and cannot combine with $filter, $orderby or
        // $count — results come back relevance-ordered, which is what a find dialog wants anyway.
        // A double quote inside the query would end the phrase early, so quotes are flattened.
        var phrase = "\"" + query.Replace("\"", " ").Trim() + "\"";
        var url = $"{GraphBase}/users/{Mailbox}/messages"
            + $"?$search={Uri.EscapeDataString(phrase)}"
            + $"&$select={Summary}&$top={take}";

        var items = new List<MailboxMessage>();
        using var response = await SendAsync(HttpMethod.Get, url, content: null, ct, allowNotFound: true, consistencyEventual: true);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Mailbox search failed: {Status}. {Detail}",
                (int)response.StatusCode, await SafeBodyAsync(response, ct));
            return new MailboxPage(items, null, 0);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (doc.RootElement.TryGetProperty("value", out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var item in arr.EnumerateArray())
                if (!IsDraft(item) && Parse(item) is { } m)
                    items.Add(m);

        return new MailboxPage(items, null, items.Count);
    }

    private async Task<MailboxPage> ListFilteredAsync(string filter, string? cursor, int take, bool inboxOnly, bool newestFirst, CancellationToken ct)
    {
        take = Math.Clamp(take, 1, 100);

        // The cursor is simply the offset of the next page (a small number) — URL-safe and impossible
        // to mangle in transit, unlike Graph's long nextLink. The probe confirmed $skip + $orderby +
        // $count (eventual) pages this filter correctly.
        var skip = 0;
        if (!string.IsNullOrEmpty(cursor) && int.TryParse(cursor, out var s) && s > 0)
            skip = s;

        // Triage views read the Inbox; tag reads span the whole mailbox so sent copies are included.
        var collection = inboxOnly ? "mailFolders/inbox/messages" : "messages";
        var url = $"{GraphBase}/users/{Mailbox}/{collection}"
            + $"?$filter={Uri.EscapeDataString(filter)}"
            // Oldest-first by default so triage users clear the backlog from page one instead of
            // paging to the end; the triage sort toggle flips this to newest-first. RecordEmailReader
            // is unaffected: it re-sorts oldest-first after collecting pages.
            + $"&$orderby=receivedDateTime%20{(newestFirst ? "desc" : "asc")}"
            + $"&$select={Summary}"
            + $"&$top={take}&$skip={skip}&$count=true";

        var items = new List<MailboxMessage>();
        int total = 0;

        using var response = await SendAsync(HttpMethod.Get, url, content: null, ct, allowNotFound: true, consistencyEventual: true);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Mailbox list failed: {Status}. {Detail}",
                (int)response.StatusCode, await SafeBodyAsync(response, ct));
            return new MailboxPage(items, null, 0);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        if (root.TryGetProperty("@odata.count", out var countEl) && countEl.TryGetInt32(out var c))
            total = c;
        // Paging must advance by the RAW page size (drafts included) — a draft dropped client-side
        // still occupied a $skip slot, so counting only kept items would re-read or loop on a page
        // that filtered down to nothing.
        var raw = 0;
        if (root.TryGetProperty("value", out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var item in arr.EnumerateArray())
            {
                raw++;
                if (!IsDraft(item) && Parse(item) is { } m)
                    items.Add(m);
            }

        // There's another page when we haven't reached the total yet; the next cursor is just the offset.
        var nextCursor = (skip + raw) < total ? (skip + take).ToString() : null;
        return new MailboxPage(items, nextCursor, total);
    }

    /// <summary>True when the raw Graph item is an unsent draft. Mailbox-wide reads include the
    /// Drafts folder, and the drafting flows tag drafts with workflow categories ahead of send —
    /// but an unsent draft is not yet correspondence, so it never surfaces on a read. Graph can't
    /// filter <c>isDraft</c> server-side, hence the client-side check.</summary>
    private static bool IsDraft(JsonElement item) =>
        item.TryGetProperty("isDraft", out var d) && d.ValueKind == JsonValueKind.True;

    public Task<bool> DiscardAsync(string messageId, string? internetMessageId, CancellationToken ct) =>
        AddTagAsync(messageId, internetMessageId, TriageCategories.Discarded, ct);

    public Task<bool> RestoreAsync(string messageId, string? internetMessageId, CancellationToken ct) =>
        RemoveTagAsync(messageId, internetMessageId, TriageCategories.Discarded, ct);

    public Task<bool> AssignAsync(string messageId, string? internetMessageId, string requestCategory, CancellationToken ct) =>
        AddTagAsync(messageId, internetMessageId, requestCategory, ct);

    public async Task<int> ClearRequestTagsAsync(string requestCategory, CancellationToken ct)
    {
        var cleared = 0;
        for (var guard = 0; guard < 20; guard++)
        {
            var ids = await FindIdsByCategoryAsync(requestCategory, ct);
            if (ids.Count == 0)
                break;

            var any = false;
            foreach (var id in ids)
                if (await RemoveTagAsync(id, null, requestCategory, ct))
                {
                    cleared++;
                    any = true;
                }

            if (!any)
                break;
        }
        return cleared;
    }

    public async Task<int> RetagAsync(string oldCategory, string newCategory, CancellationToken ct)
    {
        if (string.Equals(oldCategory, newCategory, StringComparison.OrdinalIgnoreCase))
            return 0;

        var retagged = 0;
        for (var guard = 0; guard < 20; guard++)
        {
            var ids = await FindIdsByCategoryAsync(oldCategory, ct);
            if (ids.Count == 0)
                break;

            var any = false;
            foreach (var id in ids)
                // Add the new tag first so the email always keeps a workflow tag (never bounced back to
                // triage mid-move), then drop the old one. Only count a message once both stick.
                if (await AddTagAsync(id, null, newCategory, ct)
                    && await RemoveTagAsync(id, null, oldCategory, ct))
                {
                    retagged++;
                    any = true;
                }

            if (!any)
                break;
        }
        return retagged;
    }

    public async Task<int> AddAliasTagAsync(string existingCategory, string aliasCategory, CancellationToken ct)
    {
        if (string.Equals(existingCategory, aliasCategory, StringComparison.OrdinalIgnoreCase))
            return 0;

        // Unlike RetagAsync, nothing is removed, so the category query keeps matching the messages
        // already processed — track them and stop once a pass yields nothing new. The query returns
        // one page per pass; in practice a record has aliased tags applied early in its life (RFI
        // promotion), long before its correspondence could outgrow a page.
        var aliased = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var guard = 0; guard < 20; guard++)
        {
            var ids = (await FindIdsByCategoryAsync(existingCategory, ct)).Where(seen.Add).ToList();
            if (ids.Count == 0)
                break;

            foreach (var id in ids)
                if (await AddTagAsync(id, null, aliasCategory, ct))
                    aliased++;
        }
        return aliased;
    }

    // --- Verified tag operations: write the categories, then read them back to confirm. ---
    // A single tag is added/removed at a time, and the marker is kept in lockstep: present whenever
    // the email has at least one JPMS/ workflow tag, absent (→ back to triage) when the last one goes.

    /// <summary>Add a workflow tag (ensuring the marker), verified by read-back. Registers the tag and
    /// marker in the mailbox master category list so they show as coloured labels in Outlook.</summary>
    private async Task<bool> AddTagAsync(string messageId, string? imid, string tag, CancellationToken ct)
    {
        await EnsureMasterCategoryAsync(TriageCategories.Marker, ct);
        await EnsureMasterCategoryAsync(tag, ct);

        var loaded = await LoadAsync(messageId, imid, ct);
        if (loaded is null)
        {
            _logger.LogWarning("Tag-add skipped: message {MessageId} not found.", messageId);
            return false;
        }
        var (id, current) = loaded.Value;

        var updated = current
            .Concat(new[] { TriageCategories.Marker, tag })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (!await PatchCategoriesAsync(id, updated, ct))
            return false;

        var after = await GetCategoriesAsync(id, ct);
        var ok = after is not null
            && after.Contains(tag, StringComparer.OrdinalIgnoreCase)
            && after.Contains(TriageCategories.Marker, StringComparer.OrdinalIgnoreCase);
        if (!ok) _logger.LogWarning("Tag-add ({Tag}) for {MessageId} did not verify.", tag, messageId);
        return ok;
    }

    /// <summary>Remove a workflow tag; if no JPMS/ workflow tags remain afterwards, also remove the
    /// marker so the email returns to the triage queue. Verified by read-back.</summary>
    public async Task<bool> RemoveTagAsync(string messageId, string? imid, string tag, CancellationToken ct)
    {
        var loaded = await LoadAsync(messageId, imid, ct);
        if (loaded is null)
        {
            _logger.LogWarning("Tag-remove skipped: message {MessageId} not found.", messageId);
            return false;
        }
        var (id, current) = loaded.Value;

        var remaining = current
            .Where(c => !c.Equals(tag, StringComparison.OrdinalIgnoreCase))
            .ToList();
        // No record/workflow tags left → drop any pathway tags and the marker too (back to triage).
        // Bucket tags are not triage decisions, so an email can never sit outside the queue carrying
        // only a pathway: removing the last record tag removes the pathway with it.
        if (!remaining.Any(c => TriageCategories.IsWorkflowTag(c) && !TriageCategories.IsBucketTag(c)))
        {
            remaining.RemoveAll(TriageCategories.IsBucketTag);
            remaining.RemoveAll(c => c.Equals(TriageCategories.Marker, StringComparison.OrdinalIgnoreCase));
        }

        if (!await PatchCategoriesAsync(id, remaining.ToArray(), ct))
            return false;

        var after = await GetCategoriesAsync(id, ct);
        var ok = after is not null && !after.Contains(tag, StringComparer.OrdinalIgnoreCase);
        if (!ok) _logger.LogWarning("Tag-remove ({Tag}) for {MessageId} did not verify.", tag, messageId);
        return ok;
    }

    // Categories we've already ensured exist in the mailbox master list this process (load-once cache).
    private HashSet<string>? _masterCategories;
    private readonly SemaphoreSlim _masterCategoryGate = new(1, 1);
    // Set once if the app lacks MailboxSettings.ReadWrite (403): we then stop trying entirely so a missing
    // permission doesn't add a failing Graph call to every single tag operation.
    private bool _masterCategoriesDisabled;

    /// <summary>Ensure a category exists in the mailbox's master category list (so Outlook shows it as a
    /// coloured label). Idempotent and best-effort: tagging still works if this fails — the label just
    /// won't be coloured. Needs the <c>MailboxSettings.ReadWrite</c> app permission; without it we get a
    /// 403 and quietly disable this step. The master list is read once per process and cached.</summary>
    private async Task EnsureMasterCategoryAsync(string name, CancellationToken ct)
    {
        if (_masterCategoriesDisabled)
            return;
        if (_masterCategories is not null && _masterCategories.Contains(name))
            return;

        await _masterCategoryGate.WaitAsync(ct);
        try
        {
            if (_masterCategoriesDisabled)
                return;

            var listUrl = $"{GraphBase}/users/{Mailbox}/outlook/masterCategories";

            if (_masterCategories is null)
            {
                _masterCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using var existing = await SendAsync(HttpMethod.Get, listUrl, content: null, ct, allowNotFound: true);
                if (existing.StatusCode == HttpStatusCode.Forbidden)
                {
                    _masterCategoriesDisabled = true;
                    _logger.LogInformation(
                        "Mailbox master categories not writable (needs MailboxSettings.ReadWrite). Tags still apply; "
                        + "they just won't show as coloured labels in Outlook.");
                    return;
                }
                if (existing.IsSuccessStatusCode)
                {
                    await using var stream = await existing.Content.ReadAsStreamAsync(ct);
                    using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                    if (doc.RootElement.TryGetProperty("value", out var arr) && arr.ValueKind == JsonValueKind.Array)
                        foreach (var item in arr.EnumerateArray())
                            if (item.TryGetProperty("displayName", out var dn) && dn.GetString() is { Length: > 0 } d)
                                _masterCategories.Add(d);
                }
            }

            if (_masterCategories.Contains(name))
                return;

            var payload = JsonContent.Create(new { displayName = name, color = ColourFor(name) });
            using var create = await SendAsync(HttpMethod.Post, listUrl, payload, ct, allowNotFound: true);
            if (create.StatusCode == HttpStatusCode.Forbidden)
            {
                _masterCategoriesDisabled = true;
                return;
            }
            if (!create.IsSuccessStatusCode)
                _logger.LogWarning("Master-category create for {Name} failed: {Status} (tagging continues).",
                    name, (int)create.StatusCode);

            // Cache regardless, so we don't hammer Graph retrying a category that can't be created.
            _masterCategories.Add(name);
        }
        finally
        {
            _masterCategoryGate.Release();
        }
    }

    // Outlook category colour presets: marker grey, discarded red, replied teal, pathways distinct
    // (Client green, Subcontractor orange, Internal purple), record tags blue.
    private static string ColourFor(string name) =>
        name.Equals(TriageCategories.Marker, StringComparison.OrdinalIgnoreCase) ? "preset8"
        : name.Equals(TriageCategories.Discarded, StringComparison.OrdinalIgnoreCase) ? "preset0"
        : name.Equals(TriageCategories.Replied, StringComparison.OrdinalIgnoreCase) ? "preset6"
        : name.Equals(TriageCategories.Client, StringComparison.OrdinalIgnoreCase) ? "preset4"
        : name.Equals(TriageCategories.Subcontractor, StringComparison.OrdinalIgnoreCase) ? "preset1"
        : name.Equals(TriageCategories.Internal, StringComparison.OrdinalIgnoreCase) ? "preset9"
        : "preset5";

    // Resolve the live id + current categories, re-finding by internetMessageId if the id is stale.
    private async Task<(string Id, string[] Categories)?> LoadAsync(string messageId, string? imid, CancellationToken ct)
    {
        var cats = await GetCategoriesAsync(messageId, ct);
        if (cats is not null) return (messageId, cats);

        if (string.IsNullOrEmpty(imid)) return null;
        var found = await FindByInternetMessageIdAsync(imid, ct);
        if (string.IsNullOrEmpty(found)) return null;
        cats = await GetCategoriesAsync(found, ct);
        return cats is null ? null : (found, cats);
    }

    private async Task<string[]?> GetCategoriesAsync(string messageId, CancellationToken ct)
    {
        var url = $"{GraphBase}/users/{Mailbox}/messages/{Uri.EscapeDataString(messageId)}?$select=categories";
        using var response = await SendAsync(HttpMethod.Get, url, content: null, ct, allowNotFound: true);
        if (!response.IsSuccessStatusCode) return null;

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (doc.RootElement.TryGetProperty("categories", out var arr) && arr.ValueKind == JsonValueKind.Array)
            return arr.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0).ToArray();
        return Array.Empty<string>();
    }

    private async Task<bool> PatchCategoriesAsync(string messageId, string[] categories, CancellationToken ct)
    {
        var url = $"{GraphBase}/users/{Mailbox}/messages/{Uri.EscapeDataString(messageId)}";
        using var response = await SendAsync(HttpMethod.Patch, url, JsonContent.Create(new { categories }), ct);
        if (!response.IsSuccessStatusCode)
            _logger.LogWarning("Category PATCH failed for {MessageId}: {Status}.", messageId, (int)response.StatusCode);
        return response.IsSuccessStatusCode;
    }

    // Tag MAINTENANCE (clear / rename) sweeps the whole mailbox and deliberately INCLUDES drafts:
    // pending drafts carry workflow categories ahead of send, so a rename that skipped them would
    // leave the eventual sent copy orphaned on the old tag.
    private async Task<IReadOnlyList<string>> FindIdsByCategoryAsync(string category, CancellationToken ct)
    {
        var filter = Uri.EscapeDataString($"categories/any(c:c eq '{category}')");
        var url = $"{GraphBase}/users/{Mailbox}/messages?$filter={filter}&$select=id&$top=50&$count=true";
        var ids = new List<string>();
        using var response = await SendAsync(HttpMethod.Get, url, content: null, ct, allowNotFound: true, consistencyEventual: true);
        if (!response.IsSuccessStatusCode) return ids;

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (doc.RootElement.TryGetProperty("value", out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var item in arr.EnumerateArray())
                if (item.TryGetProperty("id", out var idEl) && idEl.GetString() is { Length: > 0 } id)
                    ids.Add(id);
        return ids;
    }

    // The two per-conversation sibling queries read the thread with the PLAIN conversationId filter
    // (via ListConversationAsync — the one filter shape the mailbox-wide view reliably supports) and
    // apply the category test CLIENT-SIDE. They previously pushed a combined server-side filter
    // (conversationId + [negated] categories/any), which the inbox-folder endpoint accepted but the
    // all-mailbox view rejects as too complex — and because sibling tagging is best-effort, the
    // failure surfaced as silently-empty results: the anchor tagged fine, its thread never followed.
    // A thread is small (one capped page), so the client-side check costs nothing.
    // NB: MailboxMessage.Categories carries exactly the JPMS workflow tags (Parse strips the marker
    // and user categories), and both callers pass workflow tags, so Contains is a faithful test.

    public async Task<IReadOnlyList<string>> ListUntaggedIdsInConversationAsync(string conversationId, string category, CancellationToken ct, DateTimeOffset? receivedOnOrBefore = null)
    {
        // Thread members that don't yet carry the tag → still to be tagged. Drafts never surface
        // from ListConversationAsync: an unsent draft is tagged by its drafting flow, not swept.
        // When receivedOnOrBefore is given, members received after the anchor stay out of the sweep
        // — they queue for their own triage decision (client-side, like the category test: the
        // thread is already read whole).
        var thread = await ListConversationAsync(conversationId, ct);
        return thread.Items
            .Where(m => !m.Categories.Contains(category, StringComparer.OrdinalIgnoreCase))
            .Where(m => receivedOnOrBefore is null || m.ReceivedAt <= receivedOnOrBefore)
            .Select(m => m.Id)
            .ToList();
    }

    public async Task<IReadOnlyList<string>> ListTaggedIdsInConversationAsync(string conversationId, string category, CancellationToken ct)
    {
        // Thread members that carry the tag → to un-tag when reversing a thread-wide tag.
        var thread = await ListConversationAsync(conversationId, ct);
        return thread.Items
            .Where(m => m.Categories.Contains(category, StringComparer.OrdinalIgnoreCase))
            .Select(m => m.Id)
            .ToList();
    }

    public async Task<int> TagConversationMembersAsync(
        string conversationId, string category, CancellationToken ct, DateTimeOffset? receivedOnOrBefore = null)
    {
        await EnsureMasterCategoryAsync(TriageCategories.Marker, ct);
        await EnsureMasterCategoryAsync(category, ct);

        // One raw read of the thread (full category lists included) replaces the old
        // GET-current + PATCH + GET-verify walk per member; the cutoff semantics are identical to
        // ListUntaggedIdsInConversationAsync.
        var members = await ListConversationRawAsync(conversationId, ct);
        var updates = members
            .Where(m => !m.Categories.Contains(category, StringComparer.OrdinalIgnoreCase))
            .Where(m => receivedOnOrBefore is null || m.ReceivedAt <= receivedOnOrBefore)
            .Select(m => (m.Id, m.Categories
                .Concat(new[] { TriageCategories.Marker, category })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()))
            .ToList();
        return await BatchPatchCategoriesAsync(updates, ct);
    }

    public async Task<int> UntagConversationMembersAsync(string conversationId, string category, CancellationToken ct)
    {
        var members = await ListConversationRawAsync(conversationId, ct);
        var updates = new List<(string Id, string[] Categories)>();
        foreach (var member in members
            .Where(m => m.Categories.Contains(category, StringComparer.OrdinalIgnoreCase)))
        {
            var remaining = member.Categories
                .Where(c => !c.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToList();
            // Same rule as RemoveTagAsync: no record/workflow tags left → drop pathway tags and the
            // marker too, so the member returns to the triage queue rather than sitting outside it
            // carrying only a bucket.
            if (!remaining.Any(c => TriageCategories.IsWorkflowTag(c) && !TriageCategories.IsBucketTag(c)))
            {
                remaining.RemoveAll(TriageCategories.IsBucketTag);
                remaining.RemoveAll(c => c.Equals(TriageCategories.Marker, StringComparison.OrdinalIgnoreCase));
            }
            updates.Add((member.Id, remaining.ToArray()));
        }
        return await BatchPatchCategoriesAsync(updates, ct);
    }

    // The raw thread view backing the batched sweeps: every member's id plus its FULL category list
    // — marker, pathway tags and the user's own Outlook categories included, because a categories
    // PATCH replaces the whole array, so the sweep must write back exactly what it read plus/minus
    // the one tag. (MailboxMessage.Categories is no use here: Parse deliberately strips everything
    // but the record tags.) Same constraints as ListConversationAsync: whole mailbox, no $orderby
    // (Graph rejects it beside a conversationId filter), one capped page, unsent drafts skipped.
    private async Task<List<(string Id, string[] Categories, DateTimeOffset ReceivedAt)>> ListConversationRawAsync(
        string conversationId, CancellationToken ct)
    {
        var members = new List<(string Id, string[] Categories, DateTimeOffset ReceivedAt)>();
        if (string.IsNullOrWhiteSpace(conversationId))
            return members;

        var filter = $"conversationId eq '{conversationId.Replace("'", "''")}'";
        var url = $"{GraphBase}/users/{Mailbox}/messages"
            + $"?$filter={Uri.EscapeDataString(filter)}"
            + "&$select=id,categories,receivedDateTime,isDraft&$top=100";

        using var response = await SendAsync(HttpMethod.Get, url, content: null, ct, allowNotFound: true, consistencyEventual: true);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Raw conversation list failed: {Status}. {Detail}",
                (int)response.StatusCode, await SafeBodyAsync(response, ct));
            return members;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (doc.RootElement.TryGetProperty("value", out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var item in arr.EnumerateArray())
            {
                if (IsDraft(item)) continue;
                var id = item.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                if (string.IsNullOrEmpty(id)) continue;

                var categories = Array.Empty<string>();
                if (item.TryGetProperty("categories", out var catsEl) && catsEl.ValueKind == JsonValueKind.Array)
                    categories = catsEl.EnumerateArray()
                        .Select(c => c.GetString() ?? "")
                        .Where(c => c.Length > 0)
                        .ToArray();

                DateTimeOffset receivedAt = default;
                if (item.TryGetProperty("receivedDateTime", out var rdt) && rdt.TryGetDateTimeOffset(out var parsedAt))
                    receivedAt = parsedAt;

                members.Add((id, categories, receivedAt));
            }
        return members;
    }

    // Graph JSON batching (POST /$batch): up to 20 sub-requests per round-trip, each PATCHing one
    // message's categories. Sub-request failures are logged and skipped — the sweeps are
    // best-effort by contract (the caller's anchor tag is the verified association). Returns how
    // many PATCHes came back 2xx.
    private async Task<int> BatchPatchCategoriesAsync(
        IReadOnlyList<(string Id, string[] Categories)> updates, CancellationToken ct)
    {
        var patched = 0;
        foreach (var chunk in updates.Chunk(20))
        {
            var payload = new
            {
                requests = chunk.Select((update, index) => new
                {
                    id = index.ToString(),
                    method = "PATCH",
                    url = $"/users/{Mailbox}/messages/{Uri.EscapeDataString(update.Id)}",
                    // Sub-requests do NOT inherit the outer request's headers, and every id this
                    // client handles is an IMMUTABLE id (SendAsync sends Prefer: IdType=ImmutableId
                    // on every direct call). Without repeating that preference here, Graph reads
                    // each id as a standard id, fails to resolve it, and every PATCH in the batch
                    // 404s — silently, because the sweep is best-effort. The anchor (tagged via a
                    // direct call) still worked, which is exactly how this hid: threads stopped
                    // following their anchor out of the queue while every apply looked successful.
                    headers = new Dictionary<string, string>
                    {
                        ["Content-Type"] = "application/json",
                        ["Prefer"] = "IdType=\"ImmutableId\""
                    },
                    body = new { categories = update.Categories }
                }).ToArray()
            };

            using var response = await SendAsync(HttpMethod.Post, $"{GraphBase}/$batch", JsonContent.Create(payload), ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Category batch PATCH failed outright: {Status}. {Detail}",
                    (int)response.StatusCode, await SafeBodyAsync(response, ct));
                continue;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            if (doc.RootElement.TryGetProperty("responses", out var responses) && responses.ValueKind == JsonValueKind.Array)
                foreach (var sub in responses.EnumerateArray())
                {
                    var status = sub.TryGetProperty("status", out var statusEl) && statusEl.TryGetInt32(out var code) ? code : 0;
                    if (status is >= 200 and < 300)
                    {
                        patched++;
                        continue;
                    }
                    var messageId = sub.TryGetProperty("id", out var subId)
                        && int.TryParse(subId.GetString(), out var at) && at >= 0 && at < chunk.Length
                            ? chunk[at].Id : "(unknown)";
                    _logger.LogWarning("Category batch PATCH for {MessageId} failed: {Status}.", messageId, status);
                }
        }
        return patched;
    }

    private async Task<string?> FindByInternetMessageIdAsync(string internetMessageId, CancellationToken ct)
    {
        var escaped = internetMessageId.Replace("'", "''");
        var filter = Uri.EscapeDataString($"internetMessageId eq '{escaped}'");
        var url = $"{GraphBase}/users/{Mailbox}/messages?$filter={filter}&$select=id&$top=1";
        using var response = await SendAsync(HttpMethod.Get, url, content: null, ct, allowNotFound: true);
        if (!response.IsSuccessStatusCode) return null;

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (doc.RootElement.TryGetProperty("value", out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var item in arr.EnumerateArray())
                if (item.TryGetProperty("id", out var idEl) && idEl.GetString() is { Length: > 0 } id)
                    return id;
        return null;
    }

    public async Task<MailboxSnapshot?> GetSnapshotAsync(string messageId, string? internetMessageId, CancellationToken ct)
    {
        var id = await GetCategoriesAsync(messageId, ct) is not null
            ? messageId
            : (string.IsNullOrEmpty(internetMessageId) ? null : await FindByInternetMessageIdAsync(internetMessageId, ct));
        if (string.IsNullOrEmpty(id))
            return null;

        var url = $"{GraphBase}/users/{Mailbox}/messages/{Uri.EscapeDataString(id)}"
            + "?$select=internetMessageId,conversationId,subject,bodyPreview,from,receivedDateTime,internetMessageHeaders,categories";
        using var response = await SendAsync(HttpMethod.Get, url, content: null, ct, allowNotFound: true);
        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        var imid = root.TryGetProperty("internetMessageId", out var im) ? im.GetString() ?? "" : "";
        var conversationId = root.TryGetProperty("conversationId", out var conv) ? conv.GetString() : null;
        var subject = root.TryGetProperty("subject", out var s) ? s.GetString() ?? "" : "";
        var preview = root.TryGetProperty("bodyPreview", out var bp) ? bp.GetString() ?? "" : "";
        DateTimeOffset receivedAt = default;
        if (root.TryGetProperty("receivedDateTime", out var rdt) && rdt.TryGetDateTimeOffset(out var parsed))
            receivedAt = parsed;

        string fromEmail = "", fromName = "";
        if (root.TryGetProperty("from", out var from) && from.ValueKind == JsonValueKind.Object
            && from.TryGetProperty("emailAddress", out var addr) && addr.ValueKind == JsonValueKind.Object)
        {
            fromEmail = addr.TryGetProperty("address", out var a) ? a.GetString() ?? "" : "";
            fromName = addr.TryGetProperty("name", out var nm) ? nm.GetString() ?? "" : "";
        }

        string? inReplyTo = null;
        if (root.TryGetProperty("internetMessageHeaders", out var headers) && headers.ValueKind == JsonValueKind.Array)
            foreach (var h in headers.EnumerateArray())
            {
                var name = h.TryGetProperty("name", out var hn) ? hn.GetString() : null;
                if (string.Equals(name, "In-Reply-To", StringComparison.OrdinalIgnoreCase))
                    inReplyTo = h.TryGetProperty("value", out var hv) ? hv.GetString() : null;
            }

        var snapshotCategories = new List<string>();
        if (root.TryGetProperty("categories", out var snapCats) && snapCats.ValueKind == JsonValueKind.Array)
            foreach (var c in snapCats.EnumerateArray())
                if (c.GetString() is { Length: > 0 } cat)
                    snapshotCategories.Add(cat);

        return new MailboxSnapshot(imid, conversationId, inReplyTo, fromEmail, fromName, subject, preview, receivedAt, snapshotCategories);
    }

    private static MailboxMessage? Parse(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object) return null;
        var id = item.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        if (string.IsNullOrEmpty(id)) return null;

        var imid = item.TryGetProperty("internetMessageId", out var im) ? im.GetString() ?? "" : "";
        var subject = item.TryGetProperty("subject", out var s) ? s.GetString() ?? "" : "";
        var preview = item.TryGetProperty("bodyPreview", out var bp) ? bp.GetString() ?? "" : "";
        var hasAttachments = item.TryGetProperty("hasAttachments", out var ha) && ha.ValueKind == JsonValueKind.True;

        DateTimeOffset receivedAt = default;
        if (item.TryGetProperty("receivedDateTime", out var rdt) && rdt.TryGetDateTimeOffset(out var parsed))
            receivedAt = parsed;

        string fromEmail = "", fromName = "";
        if (item.TryGetProperty("from", out var from) && from.ValueKind == JsonValueKind.Object
            && from.TryGetProperty("emailAddress", out var addr) && addr.ValueKind == JsonValueKind.Object)
        {
            fromEmail = addr.TryGetProperty("address", out var a) ? a.GetString() ?? "" : "";
            fromName = addr.TryGetProperty("name", out var nm) ? nm.GetString() ?? "" : "";
        }

        // Only the JPMS record tags become chips (e.g. "JPMS/Discarded", "JPMS/RFI-001"); the bare
        // "JPMS" marker, the pathway tags and any of the user's own Outlook categories are left out.
        // The pathway travels separately as Bucket so clients never parse tag strings for it.
        var categories = new List<string>();
        string? bucket = null;
        if (item.TryGetProperty("categories", out var cats) && cats.ValueKind == JsonValueKind.Array)
            foreach (var c in cats.EnumerateArray())
                if (c.GetString() is { Length: > 0 } cat && TriageCategories.IsWorkflowTag(cat))
                {
                    if (TriageCategories.IsBucketTag(cat)) bucket ??= cat;
                    else categories.Add(cat);
                }

        var conversationId = item.TryGetProperty("conversationId", out var conv) ? conv.GetString() ?? "" : "";

        return new MailboxMessage(id, imid, fromEmail, fromName, subject, preview, hasAttachments, receivedAt, categories, conversationId, bucket);
    }

    // Graph only accepts attachments up to ~3 MB inline; larger files stream through an upload session.
    private const long InlineAttachmentLimit = 3_000_000;

    // NOTE (decision 2026-08-07): the projects mailbox is deliberately NOT auto-Cc'd on outbound
    // drafts (this removes the old WithProjectsMailboxCopy rule). A Cc'd copy arrives back in the
    // mailbox's Inbox as delivered mail WITHOUT the draft's categories, so every send landed
    // straight back in the triage queue as an apparently new email. The outbound leg is not lost:
    // the sent copy lives in Sent Items, record correspondence reads tags across the whole mailbox
    // (Sent Items included), and replies still return because the mailbox is the From address.

    public async Task<MailboxDraft?> CreateDraftAsync(MailboxDraftMessage draft, CancellationToken ct)
    {
        // POST /users/{mailbox}/messages creates the message in the Drafts folder. Attachments under
        // the ~3 MB inline limit go in the same call; anything larger (e.g. drawings) is streamed
        // onto the created draft through an upload session afterwards.
        var url = $"{GraphBase}/users/{Mailbox}/messages";
        var large = draft.Attachments.Where(a => a.Content.LongLength > InlineAttachmentLimit).ToList();
        var small = large.Count == 0 ? draft.Attachments : draft.Attachments.Where(a => a.Content.LongLength <= InlineAttachmentLimit).ToList();

        using var content = JsonContent.Create(BuildMessagePayload(draft with { Attachments = small }));
        using var response = await SendAsync(HttpMethod.Post, url, content, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Draft create failed: {Status}. {Detail}",
                (int)response.StatusCode, await SafeBodyAsync(response, ct));
            return null;
        }

        string? id, webLink;
        await using (var stream = await response.Content.ReadAsStreamAsync(ct))
        using (var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct))
        {
            var root = doc.RootElement;
            id = root.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            if (string.IsNullOrEmpty(id))
                return null;
            webLink = root.TryGetProperty("webLink", out var wl) ? wl.GetString() : null;
        }

        foreach (var attachment in large)
        {
            if (!await UploadLargeAttachmentAsync(id, attachment, ct))
                return null; // the incomplete draft is left in Drafts for a human to inspect/retry
        }

        return new MailboxDraft(id, webLink);
    }

    public async Task<MailboxReplyDraft?> CreateReplyDraftAsync(MailboxReplyDraftMessage reply, CancellationToken ct)
    {
        // 1. createReplyAll stages the reply draft in Drafts: same conversation, "RE:" subject,
        //    thread headers (In-Reply-To/References), quoted history in the body, and the original
        //    sender + copied recipients pre-filled — everything a mail client needs to show the sent
        //    copy inside the existing thread. createForward is the same shape for a FORWARD: "FW:"
        //    subject, quoted history, no recipients — and Graph copies the original message's
        //    attachments onto the draft itself (matching Outlook's forward), which is why the caller
        //    never re-attaches them.
        var createAction = reply.Forward ? "createForward" : "createReplyAll";
        var createUrl = $"{GraphBase}/users/{Mailbox}/messages/{Uri.EscapeDataString(reply.MessageId)}/{createAction}";
        string? id, webLink;
        string subject, existingBody, existingType;
        List<string> to, cc;
        using (var createContent = JsonContent.Create(new Dictionary<string, object?>()))
        using (var createResponse = await SendAsync(HttpMethod.Post, createUrl, createContent, ct))
        {
            if (!createResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("{Kind}-draft create failed: {Status}. {Detail}",
                    reply.Forward ? "Forward" : "Reply",
                    (int)createResponse.StatusCode, await SafeBodyAsync(createResponse, ct));
                return null;
            }
            await using var stream = await createResponse.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;
            id = root.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            if (string.IsNullOrEmpty(id)) return null;
            webLink = root.TryGetProperty("webLink", out var wl) ? wl.GetString() : null;
            subject = root.TryGetProperty("subject", out var s) ? s.GetString() ?? "" : "";
            (existingType, existingBody) = ReadBody(root);
            to = ReadRecipients(root, "toRecipients");
            cc = ReadRecipients(root, "ccRecipients");
        }

        // 2. Put the cover note ABOVE the quoted history Graph supplied, and tag the draft with the
        //    workflow categories so the sent copy (and its tagged replies) group under the record.
        var quoted = string.Equals(existingType, "html", StringComparison.OrdinalIgnoreCase)
            ? existingBody
            : $"<pre style=\"font-family:inherit;white-space:pre-wrap\">{System.Net.WebUtility.HtmlEncode(existingBody)}</pre>";
        var patchPayload = new Dictionary<string, object?>
        {
            ["body"] = new Dictionary<string, object?>
            {
                ["contentType"] = "HTML",
                ["content"] = reply.HtmlCoverNote + quoted
            }
        };
        if (reply.Categories is { Count: > 0 } categories)
            patchPayload["categories"] = categories.ToArray();

        // The projects mailbox is NOT added to Cc (decision 2026-08-07, see the note above
        // CreateDraftAsync) — a Cc'd copy would be delivered back to the Inbox untagged and land
        // in the triage queue. createReplyAll's own recipients (the original correspondents) are
        // kept as-is.

        using (var patchContent = JsonContent.Create(patchPayload))
        using (var patchResponse = await SendAsync(HttpMethod.Patch, $"{GraphBase}/users/{Mailbox}/messages/{Uri.EscapeDataString(id)}", patchContent, ct))
        {
            if (!patchResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Reply-draft body patch failed: {Status}. {Detail}",
                    (int)patchResponse.StatusCode, await SafeBodyAsync(patchResponse, ct));
                return null; // the bare reply draft is left in Drafts for a human to inspect
            }
        }

        // 3. Attach the files — small ones inline, anything larger through an upload session.
        foreach (var attachment in reply.Attachments)
        {
            if (attachment.Content.LongLength > InlineAttachmentLimit)
            {
                if (!await UploadLargeAttachmentAsync(id, attachment, ct)) return null;
                continue;
            }
            var attachPayload = AttachmentPayload(attachment);
            using var attachContent = JsonContent.Create(attachPayload);
            using var attachResponse = await SendAsync(
                HttpMethod.Post, $"{GraphBase}/users/{Mailbox}/messages/{Uri.EscapeDataString(id)}/attachments", attachContent, ct);
            if (!attachResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Reply-draft attachment failed: {Status}. {Detail}",
                    (int)attachResponse.StatusCode, await SafeBodyAsync(attachResponse, ct));
                return null; // the incomplete draft is left in Drafts for a human to inspect/retry
            }
        }

        return new MailboxReplyDraft(id, webLink, subject, to, cc);
    }

    private static (string ContentType, string Content) ReadBody(JsonElement message)
    {
        if (message.TryGetProperty("body", out var body) && body.ValueKind == JsonValueKind.Object)
            return (
                body.TryGetProperty("contentType", out var t) ? t.GetString() ?? "html" : "html",
                body.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "");
        return ("html", "");
    }

    private static List<string> ReadRecipients(JsonElement message, string property)
    {
        var result = new List<string>();
        if (message.TryGetProperty(property, out var recipients) && recipients.ValueKind == JsonValueKind.Array)
            foreach (var r in recipients.EnumerateArray())
                if (r.TryGetProperty("emailAddress", out var addr) && addr.ValueKind == JsonValueKind.Object
                    && addr.TryGetProperty("address", out var a) && a.GetString() is { Length: > 0 } email)
                    result.Add(email);
        return result;
    }

    // Streams one >3 MB file onto a draft via Graph's attachment upload session (chunked PUTs to a
    // pre-authenticated URL — no bearer header on the chunks).
    private async Task<bool> UploadLargeAttachmentAsync(string messageId, MailboxDraftAttachment attachment, CancellationToken ct)
    {
        var sessionUrl = $"{GraphBase}/users/{Mailbox}/messages/{Uri.EscapeDataString(messageId)}/attachments/createUploadSession";
        var sessionPayload = new Dictionary<string, object?>
        {
            ["AttachmentItem"] = new Dictionary<string, object?>
            {
                ["attachmentType"] = "file",
                ["name"] = attachment.FileName,
                ["size"] = attachment.Content.LongLength
            }
        };

        string? uploadUrl;
        using (var sessionContent = JsonContent.Create(sessionPayload))
        using (var sessionResponse = await SendAsync(HttpMethod.Post, sessionUrl, sessionContent, ct))
        {
            if (!sessionResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Attachment upload session failed: {Status}. {Detail}",
                    (int)sessionResponse.StatusCode, await SafeBodyAsync(sessionResponse, ct));
                return false;
            }
            await using var stream = await sessionResponse.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            uploadUrl = doc.RootElement.TryGetProperty("uploadUrl", out var u) ? u.GetString() : null;
        }
        if (string.IsNullOrEmpty(uploadUrl)) return false;

        const int ChunkSize = 3_276_800; // 10 × 320 KiB, the required granularity
        var data = attachment.Content;
        for (long offset = 0; offset < data.LongLength; offset += ChunkSize)
        {
            var count = (int)Math.Min(ChunkSize, data.LongLength - offset);
            using var chunk = new ByteArrayContent(data, (int)offset, count);
            chunk.Headers.ContentRange = new ContentRangeHeaderValue(offset, offset + count - 1, data.LongLength);
            using var put = new HttpRequestMessage(HttpMethod.Put, uploadUrl) { Content = chunk };
            using var putResponse = await _http.SendAsync(put, ct);
            if (!putResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Attachment chunk upload failed at {Offset}: {Status}.",
                    offset, (int)putResponse.StatusCode);
                return false;
            }
        }
        return true;
    }

    // One Graph fileAttachment payload, inline images carrying their contentId so the body's
    // cid: references resolve in the recipient's mail client.
    private static Dictionary<string, object?> AttachmentPayload(MailboxDraftAttachment a)
    {
        var payload = new Dictionary<string, object?>
        {
            ["@odata.type"] = "#microsoft.graph.fileAttachment",
            ["name"] = a.FileName,
            ["contentType"] = a.ContentType,
            ["contentBytes"] = Convert.ToBase64String(a.Content)
        };
        if (a.IsInline)
        {
            payload["isInline"] = true;
            if (!string.IsNullOrEmpty(a.ContentId))
                payload["contentId"] = a.ContentId;
        }
        return payload;
    }

    // The Graph message shape used by draft-create.
    private static Dictionary<string, object?> BuildMessagePayload(MailboxDraftMessage message)
    {
        static Dictionary<string, object?> Recipient(MailboxDraftRecipient r) => new()
        {
            ["emailAddress"] = string.IsNullOrWhiteSpace(r.Name)
                ? new Dictionary<string, object?> { ["address"] = r.Email }
                : new Dictionary<string, object?> { ["address"] = r.Email, ["name"] = r.Name }
        };

        var payload = new Dictionary<string, object?>
        {
            ["subject"] = message.Subject,
            ["body"] = new Dictionary<string, object?>
            {
                ["contentType"] = "HTML",
                ["content"] = message.HtmlBody
            },
            ["toRecipients"] = message.To.Select(Recipient).ToArray(),
            ["attachments"] = message.Attachments.Select(AttachmentPayload).ToArray()
        };

        if (message.Cc is { Count: > 0 } cc)
            payload["ccRecipients"] = cc.Select(Recipient).ToArray();
        if (message.Bcc is { Count: > 0 } bcc)
            payload["bccRecipients"] = bcc.Select(Recipient).ToArray();
        if (message.Categories is { Count: > 0 } categories)
            payload["categories"] = categories.ToArray();

        return payload;
    }

    public async Task<bool> UpdateDraftEnvelopeAsync(
        string draftMessageId,
        IReadOnlyList<MailboxDraftRecipient> to,
        IReadOnlyList<MailboxDraftRecipient> cc,
        IReadOnlyList<MailboxDraftRecipient> bcc,
        string subject,
        CancellationToken ct)
    {
        // The composer's envelope replaces Graph's scaffolding wholesale. The projects mailbox is
        // NOT re-added to Cc (decision 2026-08-07, see the note above CreateDraftAsync) — the
        // envelope the user saw is exactly what is sent.
        static Dictionary<string, object?> Recipient(MailboxDraftRecipient r) => new()
        {
            ["emailAddress"] = string.IsNullOrWhiteSpace(r.Name)
                ? new Dictionary<string, object?> { ["address"] = r.Email }
                : new Dictionary<string, object?> { ["address"] = r.Email, ["name"] = r.Name }
        };

        var payload = new Dictionary<string, object?>
        {
            ["subject"] = subject,
            ["toRecipients"] = to.Select(Recipient).ToArray(),
            ["ccRecipients"] = cc.Select(Recipient).ToArray(),
            ["bccRecipients"] = bcc.Select(Recipient).ToArray()
        };

        var url = $"{GraphBase}/users/{Mailbox}/messages/{Uri.EscapeDataString(draftMessageId)}";
        using var content = JsonContent.Create(payload);
        using var response = await SendAsync(HttpMethod.Patch, url, content, ct);
        if (!response.IsSuccessStatusCode)
            _logger.LogWarning("Draft envelope PATCH failed for {MessageId}: {Status}. {Detail}",
                draftMessageId, (int)response.StatusCode, await SafeBodyAsync(response, ct));
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SendDraftAsync(string draftMessageId, CancellationToken ct)
    {
        // The one send call in the system (see the interface note). Graph answers 202 with an empty
        // body on success. One retry on 429, honouring Retry-After — a send is a user-facing click,
        // so a long backoff loop is worse than an honest failure (the draft survives either way).
        var url = $"{GraphBase}/users/{Mailbox}/messages/{Uri.EscapeDataString(draftMessageId)}/send";
        for (var attempt = 0; ; attempt++)
        {
            using var response = await SendAsync(HttpMethod.Post, url, content: null, ct);
            if (response.IsSuccessStatusCode)
                return true;

            if (response.StatusCode == (HttpStatusCode)429 && attempt == 0)
            {
                var delay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(2);
                if (delay > TimeSpan.FromSeconds(15)) delay = TimeSpan.FromSeconds(15);
                await Task.Delay(delay, ct);
                continue;
            }

            _logger.LogWarning("Draft send failed for {MessageId}: {Status}. {Detail}",
                draftMessageId, (int)response.StatusCode, await SafeBodyAsync(response, ct));
            return false;
        }
    }

    public async Task<string?> GetWebLinkAsync(string messageId, CancellationToken ct)
    {
        var url = $"{GraphBase}/users/{Mailbox}/messages/{Uri.EscapeDataString(messageId)}?$select=webLink";
        using var response = await SendAsync(HttpMethod.Get, url, content: null, ct, allowNotFound: true);
        if (!response.IsSuccessStatusCode) return null;
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return doc.RootElement.TryGetProperty("webLink", out var wl) ? wl.GetString() : null;
    }

    public async Task<MailboxDraftDeletion> DeleteDraftAsync(string draftMessageId, CancellationToken ct)
    {
        // Read-back first: the delete only ever fires on a message Graph itself says is an unsent
        // draft. Handing this method a delivered or sent message's id must be a refusal, not a
        // deletion — the mailbox is a system of record and the client's no-delete rule covers
        // everything except unsent drafts. (A draft sent between the read-back and the DELETE is a
        // theoretical race Graph gives no conditional delete for; the window is milliseconds.)
        var checkUrl = $"{GraphBase}/users/{Mailbox}/messages/{Uri.EscapeDataString(draftMessageId)}?$select=id,isDraft,subject";
        string? subject;
        using (var check = await SendAsync(HttpMethod.Get, checkUrl, content: null, ct, allowNotFound: true))
        {
            if (check.StatusCode == HttpStatusCode.NotFound)
                return new MailboxDraftDeletion(MailboxDraftDeleteOutcome.NotFound);
            if (!check.IsSuccessStatusCode)
            {
                _logger.LogWarning("Draft delete read-back failed for {MessageId}: {Status}. {Detail}",
                    draftMessageId, (int)check.StatusCode, await SafeBodyAsync(check, ct));
                return new MailboxDraftDeletion(MailboxDraftDeleteOutcome.Failed);
            }
            await using var stream = await check.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;
            subject = root.TryGetProperty("subject", out var s) ? s.GetString() : null;
            if (!(root.TryGetProperty("isDraft", out var d) && d.ValueKind == JsonValueKind.True))
                return new MailboxDraftDeletion(MailboxDraftDeleteOutcome.NotADraft, subject);
        }

        var deleteUrl = $"{GraphBase}/users/{Mailbox}/messages/{Uri.EscapeDataString(draftMessageId)}";
        using var response = await SendAsync(HttpMethod.Delete, deleteUrl, content: null, ct, allowNotFound: true);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return new MailboxDraftDeletion(MailboxDraftDeleteOutcome.NotFound, subject);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Draft delete failed for {MessageId}: {Status}. {Detail}",
                draftMessageId, (int)response.StatusCode, await SafeBodyAsync(response, ct));
            return new MailboxDraftDeletion(MailboxDraftDeleteOutcome.Failed, subject);
        }
        return new MailboxDraftDeletion(MailboxDraftDeleteOutcome.Deleted, subject);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string url, HttpContent? content, CancellationToken ct,
        bool allowNotFound = false, bool consistencyEventual = false)
    {
        var token = await _tokens.GetTokenAsync(ct);
        using var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        // Immutable ids so a stored id keeps resolving; eventual consistency for $count + negated filters.
        request.Headers.TryAddWithoutValidation("Prefer", "IdType=\"ImmutableId\"");
        if (consistencyEventual)
            request.Headers.TryAddWithoutValidation("ConsistencyLevel", "eventual");

        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode && !(allowNotFound && response.StatusCode == HttpStatusCode.NotFound))
            _logger.LogWarning("Graph {Method} {Status}.", method, (int)response.StatusCode);
        return response;
    }

    private static async Task<string> SafeBodyAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try { return await response.Content.ReadAsStringAsync(ct); } catch { return "(no body)"; }
    }
}
