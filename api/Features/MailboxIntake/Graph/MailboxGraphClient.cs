using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Jewel.JPMS.Models;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Graph;

/// <summary>The categories the triage system stamps on a mailbox message. Triage never moves an
/// email — it tags it — so the Inbox stays whole and each view is a category filter.</summary>
public static class TriageCategories
{
    /// <summary>The marker present on any email that carries a JPMS workflow tag. The triage queue is
    /// Inbox WITHOUT this; the Tagged view is Inbox WITH it. Graph only filters categories by exact
    /// match (no "starts-with"), so this single marker is how we express "has any JPMS tag".</summary>
    public const string Marker = "JPMS";

    /// <summary>Prefix shared by every workflow tag (e.g. "JPMS/Discarded", "JPMS/RFI-001"). The bare
    /// <see cref="Marker"/> has no trailing slash, so it never matches this — that's how RemoveTag
    /// decides whether any workflow tags remain.</summary>
    public const string WorkflowPrefix = "JPMS/";

    /// <summary>Present on a discarded ("not a request") email.</summary>
    public const string Discarded = "JPMS/Discarded";

    /// <summary>The workflow tag for an email linked to a record, from its reference
    /// (e.g. "RFI-001" -> "JPMS/RFI-001", "BPI-0001" -> "JPMS/BPI-0001"). The record reads its emails
    /// back by this exact tag. Record-type-agnostic: the tag is just the reference stem.</summary>
    public static string ForRecord(string reference) => $"JPMS/{reference.Trim()}";

    /// <summary>Back-compat alias for <see cref="ForRecord"/>, kept while the Request path migrates to
    /// the record-agnostic link layer. Prefer <see cref="ForRecord"/> in new code.</summary>
    public static string ForRequest(string reference) => ForRecord(reference);

    /// <summary>True if a category is a JPMS workflow tag (not the bare marker, not a user category).</summary>
    public static bool IsWorkflowTag(string category) =>
        category.StartsWith(WorkflowPrefix, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Live mailbox access for triage, category-based: read folder pages filtered by category, and tag
/// messages. Every tag operation is <b>verified</b> — it writes the categories and then reads them
/// back, only reporting success if the change actually stuck. Nothing is ever moved or deleted, so
/// duplication and lost mail are impossible by construction.
///
/// All calls request immutable ids (<c>Prefer: IdType="ImmutableId"</c>) so a message id stays valid.
/// </summary>
public interface IMailboxGraphClient
{
    /// <summary>One page of the triage queue: Inbox messages NOT tagged triaged, oldest first (so
    /// the backlog is cleared from page one).</summary>
    Task<MailboxPage> ListInboxAsync(string? cursor, int take, CancellationToken ct);

    /// <summary>One page of the discarded pile: Inbox messages tagged discarded, oldest first.</summary>
    Task<MailboxPage> ListDiscardedAsync(string? cursor, int take, CancellationToken ct);

    /// <summary>One page of the emails tagged to a specific record (its workflow tag), oldest first.
    /// This is how a record reads its associated emails live — no copies are stored.</summary>
    Task<MailboxPage> ListByTagAsync(string tag, string? cursor, int take, CancellationToken ct);

    /// <summary>One page of every tagged email (anything carrying the JPMS marker), oldest first —
    /// the management surface for the Tagged tab.</summary>
    Task<MailboxPage> ListTaggedAsync(string? cursor, int take, CancellationToken ct);

    /// <summary>One page of emails carrying ANY of the given workflow tags (an OR filter), oldest
    /// first — backs the Tagged tab's multi-select filter.</summary>
    Task<MailboxPage> ListByTagsAsync(IReadOnlyList<string> tags, string? cursor, int take, CancellationToken ct);

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

    /// <summary>Read the fields needed to record an email against a request, or null if it's gone.</summary>
    Task<MailboxSnapshot?> GetSnapshotAsync(string messageId, string? internetMessageId, CancellationToken ct);

    /// <summary>The distinct conversation ids of the Inbox messages currently carrying a record's tag.
    /// These are the email threads the record already touches — used to find new replies to pull in.</summary>
    Task<IReadOnlyList<string>> ListInboxConversationIdsByCategoryAsync(string category, CancellationToken ct);

    /// <summary>Inbox message ids in the given conversation that do NOT yet carry the category — i.e. the
    /// thread members still to be tagged (e.g. replies that arrived after the original link).</summary>
    Task<IReadOnlyList<string>> ListUntaggedInboxIdsInConversationAsync(string conversationId, string category, CancellationToken ct);

    /// <summary>Inbox message ids in the given conversation that currently carry the category — i.e. the
    /// thread members to un-tag when reversing a thread-wide tag (e.g. restoring a discarded thread).</summary>
    Task<IReadOnlyList<string>> ListTaggedInboxIdsInConversationAsync(string conversationId, string category, CancellationToken ct);

    /// <summary>
    /// Create a draft message in the mailbox's Drafts folder — recipients, subject, HTML body and
    /// attachments all pre-filled — for a person to review and send from the mailbox itself.
    /// Nothing is sent. Returns the draft's identity (with a webLink to open it in Outlook on the
    /// web when Graph provides one), or null when the mailbox is unconfigured / the create failed.
    /// </summary>
    Task<MailboxDraft?> CreateDraftAsync(MailboxDraftMessage draft, CancellationToken ct);
}

/// <summary>A new draft to place in the mailbox's Drafts folder (never sent by JPMS).</summary>
public sealed record MailboxDraftMessage(
    IReadOnlyList<MailboxDraftRecipient> To,
    string Subject,
    string HtmlBody,
    IReadOnlyList<MailboxDraftAttachment> Attachments);

/// <summary>A draft recipient (address plus optional display name).</summary>
public sealed record MailboxDraftRecipient(string Email, string? Name = null);

/// <summary>A file attached to a draft, sent as a Graph fileAttachment (base64 contentBytes).</summary>
public sealed record MailboxDraftAttachment(string FileName, string ContentType, byte[] Content);

/// <summary>A created draft: its Graph id and (usually) a webLink that opens it in Outlook on the web.</summary>
public sealed record MailboxDraft(string Id, string? WebLink);

/// <summary>The subset of a mailbox message recorded against a request when an email is assigned.</summary>
public sealed record MailboxSnapshot(
    string InternetMessageId,
    string? ConversationId,
    string? InReplyTo,
    string FromEmail,
    string FromName,
    string Subject,
    string BodyPreview,
    DateTimeOffset ReceivedAt);

/// <summary>No-op client used when Graph credentials aren't configured: triage shows empty and tag
/// operations report failure (so the UI shows an error rather than a false success).</summary>
public sealed class NullMailboxGraphClient : IMailboxGraphClient
{
    public Task<MailboxPage> ListInboxAsync(string? cursor, int take, CancellationToken ct) =>
        Task.FromResult(new MailboxPage(Array.Empty<MailboxMessage>(), null, 0));
    public Task<MailboxPage> ListDiscardedAsync(string? cursor, int take, CancellationToken ct) =>
        Task.FromResult(new MailboxPage(Array.Empty<MailboxMessage>(), null, 0));
    public Task<MailboxPage> ListByTagAsync(string tag, string? cursor, int take, CancellationToken ct) =>
        Task.FromResult(new MailboxPage(Array.Empty<MailboxMessage>(), null, 0));
    public Task<MailboxPage> ListTaggedAsync(string? cursor, int take, CancellationToken ct) =>
        Task.FromResult(new MailboxPage(Array.Empty<MailboxMessage>(), null, 0));
    public Task<MailboxPage> ListByTagsAsync(IReadOnlyList<string> tags, string? cursor, int take, CancellationToken ct) =>
        Task.FromResult(new MailboxPage(Array.Empty<MailboxMessage>(), null, 0));
    public Task<bool> RemoveTagAsync(string messageId, string? internetMessageId, string tag, CancellationToken ct) => Task.FromResult(false);
    public Task<bool> DiscardAsync(string messageId, string? internetMessageId, CancellationToken ct) => Task.FromResult(false);
    public Task<bool> RestoreAsync(string messageId, string? internetMessageId, CancellationToken ct) => Task.FromResult(false);
    public Task<bool> AssignAsync(string messageId, string? internetMessageId, string requestCategory, CancellationToken ct) => Task.FromResult(false);
    public Task<int> ClearRequestTagsAsync(string requestCategory, CancellationToken ct) => Task.FromResult(0);
    public Task<int> RetagAsync(string oldCategory, string newCategory, CancellationToken ct) => Task.FromResult(0);
    public Task<MailboxSnapshot?> GetSnapshotAsync(string messageId, string? internetMessageId, CancellationToken ct) => Task.FromResult<MailboxSnapshot?>(null);
    public Task<IReadOnlyList<string>> ListInboxConversationIdsByCategoryAsync(string category, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    public Task<IReadOnlyList<string>> ListUntaggedInboxIdsInConversationAsync(string conversationId, string category, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    public Task<IReadOnlyList<string>> ListTaggedInboxIdsInConversationAsync(string conversationId, string category, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    public Task<MailboxDraft?> CreateDraftAsync(MailboxDraftMessage draft, CancellationToken ct) =>
        Task.FromResult<MailboxDraft?>(null);
}

/// <summary>Graph REST implementation (HttpClient + app-only token).</summary>
public sealed class MailboxGraphClient : IMailboxGraphClient
{
    private const string GraphBase = "https://graph.microsoft.com/v1.0";
    private const string Summary =
        "id,internetMessageId,subject,bodyPreview,from,receivedDateTime,hasAttachments,categories";

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

    public Task<MailboxPage> ListInboxAsync(string? cursor, int take, CancellationToken ct) =>
        ListFilteredAsync($"not categories/any(c:c eq '{TriageCategories.Marker}')", cursor, take, ct);

    public Task<MailboxPage> ListDiscardedAsync(string? cursor, int take, CancellationToken ct) =>
        ListFilteredAsync($"categories/any(c:c eq '{TriageCategories.Discarded}')", cursor, take, ct);

    public Task<MailboxPage> ListByTagAsync(string tag, string? cursor, int take, CancellationToken ct) =>
        ListFilteredAsync($"categories/any(c:c eq '{tag}')", cursor, take, ct);

    public Task<MailboxPage> ListTaggedAsync(string? cursor, int take, CancellationToken ct) =>
        ListFilteredAsync($"categories/any(c:c eq '{TriageCategories.Marker}')", cursor, take, ct);

    public Task<MailboxPage> ListByTagsAsync(IReadOnlyList<string> tags, string? cursor, int take, CancellationToken ct)
    {
        if (tags.Count == 0)
            return ListTaggedAsync(cursor, take, ct);
        // OR the per-tag category filters: an email matching any selected tag is included. Single-quotes
        // in a category are escaped by doubling, per OData.
        var filter = string.Join(" or ",
            tags.Select(t => $"categories/any(c:c eq '{t.Replace("'", "''")}')"));
        return ListFilteredAsync(filter, cursor, take, ct);
    }

    private async Task<MailboxPage> ListFilteredAsync(string filter, string? cursor, int take, CancellationToken ct)
    {
        take = Math.Clamp(take, 1, 100);

        // The cursor is simply the offset of the next page (a small number) — URL-safe and impossible
        // to mangle in transit, unlike Graph's long nextLink. The probe confirmed $skip + $orderby +
        // $count (eventual) pages this filter correctly.
        var skip = 0;
        if (!string.IsNullOrEmpty(cursor) && int.TryParse(cursor, out var s) && s > 0)
            skip = s;

        var url = $"{GraphBase}/users/{Mailbox}/mailFolders/inbox/messages"
            + $"?$filter={Uri.EscapeDataString(filter)}"
            // Oldest-first so triage users clear the backlog from page one instead of paging to the
            // end. RecordEmailReader is unaffected: it re-sorts oldest-first after collecting pages.
            + "&$orderby=receivedDateTime%20asc"
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
        if (root.TryGetProperty("value", out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var item in arr.EnumerateArray())
                if (Parse(item) is { } m)
                    items.Add(m);

        // There's another page when we haven't reached the total yet; the next cursor is just the offset.
        var nextCursor = (skip + items.Count) < total ? (skip + take).ToString() : null;
        return new MailboxPage(items, nextCursor, total);
    }

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
            var ids = await FindInboxIdsByCategoryAsync(requestCategory, ct);
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
            var ids = await FindInboxIdsByCategoryAsync(oldCategory, ct);
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
        // No workflow tags left → drop the marker too (back to triage).
        if (!remaining.Any(TriageCategories.IsWorkflowTag))
            remaining.RemoveAll(c => c.Equals(TriageCategories.Marker, StringComparison.OrdinalIgnoreCase));

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

    // Outlook category colour presets: marker grey, discarded red, workflow tags blue.
    private static string ColourFor(string name) =>
        name.Equals(TriageCategories.Marker, StringComparison.OrdinalIgnoreCase) ? "preset8"
        : name.Equals(TriageCategories.Discarded, StringComparison.OrdinalIgnoreCase) ? "preset0"
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

    private async Task<IReadOnlyList<string>> FindInboxIdsByCategoryAsync(string category, CancellationToken ct)
    {
        var filter = Uri.EscapeDataString($"categories/any(c:c eq '{category}')");
        var url = $"{GraphBase}/users/{Mailbox}/mailFolders/inbox/messages?$filter={filter}&$select=id&$top=50&$count=true";
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

    public async Task<IReadOnlyList<string>> ListInboxConversationIdsByCategoryAsync(string category, CancellationToken ct)
    {
        var filter = $"categories/any(c:c eq '{category.Replace("'", "''")}')";
        var conversationIds = await CollectInboxFieldAsync(filter, "conversationId", ct);
        return conversationIds.Distinct(StringComparer.Ordinal).ToList();
    }

    public Task<IReadOnlyList<string>> ListUntaggedInboxIdsInConversationAsync(string conversationId, string category, CancellationToken ct)
    {
        // conversationId eq '…' AND the email doesn't already carry the tag → the thread members still
        // to be tagged. The negated category clause needs eventual consistency (handled in CollectInboxFieldAsync).
        var filter = $"conversationId eq '{conversationId.Replace("'", "''")}' "
            + $"and not categories/any(c:c eq '{category.Replace("'", "''")}')";
        return CollectInboxFieldAsync(filter, "id", ct);
    }

    public Task<IReadOnlyList<string>> ListTaggedInboxIdsInConversationAsync(string conversationId, string category, CancellationToken ct)
    {
        // conversationId eq '…' AND the email carries the tag → the thread members to un-tag.
        var filter = $"conversationId eq '{conversationId.Replace("'", "''")}' "
            + $"and categories/any(c:c eq '{category.Replace("'", "''")}')";
        return CollectInboxFieldAsync(filter, "id", ct);
    }

    // Page through Inbox messages matching the filter and collect one string field ("id" /
    // "conversationId") from each. Mirrors ListFilteredAsync's $skip paging; eventual consistency for
    // $count + negated filters. Guard-bounded so a pathological thread can't loop forever.
    private async Task<IReadOnlyList<string>> CollectInboxFieldAsync(string filter, string field, CancellationToken ct)
    {
        var results = new List<string>();
        var skip = 0;
        for (var guard = 0; guard < 20; guard++)
        {
            var url = $"{GraphBase}/users/{Mailbox}/mailFolders/inbox/messages"
                + $"?$filter={Uri.EscapeDataString(filter)}"
                + $"&$select={field}&$top=100&$skip={skip}&$count=true";
            using var response = await SendAsync(HttpMethod.Get, url, content: null, ct, allowNotFound: true, consistencyEventual: true);
            if (!response.IsSuccessStatusCode) break;

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var pageCount = 0;
            if (doc.RootElement.TryGetProperty("value", out var arr) && arr.ValueKind == JsonValueKind.Array)
                foreach (var item in arr.EnumerateArray())
                {
                    pageCount++;
                    if (item.TryGetProperty(field, out var el) && el.GetString() is { Length: > 0 } value)
                        results.Add(value);
                }
            if (pageCount < 100) break;
            skip += 100;
        }
        return results;
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
            + "?$select=internetMessageId,conversationId,subject,bodyPreview,from,receivedDateTime,internetMessageHeaders";
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

        return new MailboxSnapshot(imid, conversationId, inReplyTo, fromEmail, fromName, subject, preview, receivedAt);
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

        // Only the JPMS workflow tags become chips (e.g. "JPMS/Discarded", "JPMS/RFI-001"); the bare
        // "JPMS" marker and any of the user's own Outlook categories are left out.
        var categories = new List<string>();
        if (item.TryGetProperty("categories", out var cats) && cats.ValueKind == JsonValueKind.Array)
            foreach (var c in cats.EnumerateArray())
                if (c.GetString() is { Length: > 0 } cat && TriageCategories.IsWorkflowTag(cat))
                    categories.Add(cat);

        return new MailboxMessage(id, imid, fromEmail, fromName, subject, preview, hasAttachments, receivedAt, categories);
    }

    public async Task<MailboxDraft?> CreateDraftAsync(MailboxDraftMessage draft, CancellationToken ct)
    {
        // POST /users/{mailbox}/messages creates the message in the Drafts folder. Attachments under
        // the ~3 MB inline limit (our request-document PDFs are far smaller) go in the same call.
        var url = $"{GraphBase}/users/{Mailbox}/messages";

        var payload = new Dictionary<string, object?>
        {
            ["subject"] = draft.Subject,
            ["body"] = new Dictionary<string, object?>
            {
                ["contentType"] = "HTML",
                ["content"] = draft.HtmlBody
            },
            ["toRecipients"] = draft.To.Select(r => new Dictionary<string, object?>
            {
                ["emailAddress"] = string.IsNullOrWhiteSpace(r.Name)
                    ? new Dictionary<string, object?> { ["address"] = r.Email }
                    : new Dictionary<string, object?> { ["address"] = r.Email, ["name"] = r.Name }
            }).ToArray(),
            ["attachments"] = draft.Attachments.Select(a => new Dictionary<string, object?>
            {
                ["@odata.type"] = "#microsoft.graph.fileAttachment",
                ["name"] = a.FileName,
                ["contentType"] = a.ContentType,
                ["contentBytes"] = Convert.ToBase64String(a.Content)
            }).ToArray()
        };

        using var content = JsonContent.Create(payload);
        using var response = await SendAsync(HttpMethod.Post, url, content, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Draft create failed: {Status}. {Detail}",
                (int)response.StatusCode, await SafeBodyAsync(response, ct));
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;
        var id = root.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        if (string.IsNullOrEmpty(id))
            return null;
        var webLink = root.TryGetProperty("webLink", out var wl) ? wl.GetString() : null;
        return new MailboxDraft(id, webLink);
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
