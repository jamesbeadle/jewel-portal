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
    /// <summary>Present on any email that has been triaged (the queue = Inbox without this).</summary>
    public const string Triaged = "JPMS/Triaged";

    /// <summary>Present on a discarded ("not a request") email.</summary>
    public const string Discarded = "JPMS/Discarded";

    /// <summary>The category for an email assigned to a request, e.g. "JPMS/REQ-0014".</summary>
    public static string ForRequest(int number) => $"JPMS/REQ-{number:0000}";
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
    /// <summary>One page of the triage queue: Inbox messages NOT tagged triaged, newest first.</summary>
    Task<MailboxPage> ListInboxAsync(string? cursor, int take, CancellationToken ct);

    /// <summary>One page of the discarded pile: Inbox messages tagged discarded, newest first.</summary>
    Task<MailboxPage> ListDiscardedAsync(string? cursor, int take, CancellationToken ct);

    /// <summary>Tag an email triaged + discarded. Returns true only once the tags are read back present.</summary>
    Task<bool> DiscardAsync(string messageId, string? internetMessageId, CancellationToken ct);

    /// <summary>Remove the triaged + discarded tags (undo a discard). Verified by read-back.</summary>
    Task<bool> RestoreAsync(string messageId, string? internetMessageId, CancellationToken ct);

    /// <summary>Tag an email triaged + assigned to the given request category. Verified by read-back.</summary>
    Task<bool> AssignAsync(string messageId, string? internetMessageId, string requestCategory, CancellationToken ct);

    /// <summary>Remove the triaged + request tags from every email assigned to a request (return-to-triage).
    /// Returns how many were cleared.</summary>
    Task<int> ClearRequestTagsAsync(string requestCategory, CancellationToken ct);

    /// <summary>Read the fields needed to record an email against a request, or null if it's gone.</summary>
    Task<MailboxSnapshot?> GetSnapshotAsync(string messageId, string? internetMessageId, CancellationToken ct);
}

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
    public Task<bool> DiscardAsync(string messageId, string? internetMessageId, CancellationToken ct) => Task.FromResult(false);
    public Task<bool> RestoreAsync(string messageId, string? internetMessageId, CancellationToken ct) => Task.FromResult(false);
    public Task<bool> AssignAsync(string messageId, string? internetMessageId, string requestCategory, CancellationToken ct) => Task.FromResult(false);
    public Task<int> ClearRequestTagsAsync(string requestCategory, CancellationToken ct) => Task.FromResult(0);
    public Task<MailboxSnapshot?> GetSnapshotAsync(string messageId, string? internetMessageId, CancellationToken ct) => Task.FromResult<MailboxSnapshot?>(null);
}

/// <summary>Graph REST implementation (HttpClient + app-only token).</summary>
public sealed class MailboxGraphClient : IMailboxGraphClient
{
    private const string GraphBase = "https://graph.microsoft.com/v1.0";
    private const string Summary =
        "id,internetMessageId,subject,bodyPreview,from,receivedDateTime,hasAttachments";

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
        ListFilteredAsync($"not categories/any(c:c eq '{TriageCategories.Triaged}')", cursor, take, ct);

    public Task<MailboxPage> ListDiscardedAsync(string? cursor, int take, CancellationToken ct) =>
        ListFilteredAsync($"categories/any(c:c eq '{TriageCategories.Discarded}')", cursor, take, ct);

    private async Task<MailboxPage> ListFilteredAsync(string filter, string? cursor, int take, CancellationToken ct)
    {
        take = Math.Clamp(take, 1, 100);
        var url = $"{GraphBase}/users/{Mailbox}/mailFolders/inbox/messages"
            + $"?$filter={Uri.EscapeDataString(filter)}"
            + "&$orderby=receivedDateTime%20desc"
            + $"&$select={Summary}"
            + $"&$top={take}&$count=true";
        if (!string.IsNullOrEmpty(cursor))
            url += $"&$skiptoken={Uri.EscapeDataString(cursor)}";

        var items = new List<MailboxMessage>();
        int total = 0;
        string? nextCursor = null;

        // $count + the negated filter require advanced-query mode (ConsistencyLevel: eventual), which
        // pages by skiptoken rather than $skip — see the probe results.
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
        if (root.TryGetProperty("@odata.nextLink", out var nl) && nl.GetString() is { } link)
            nextCursor = ExtractSkipToken(link);

        return new MailboxPage(items, nextCursor, total);
    }

    // The continuation token is carried in the nextLink's $skiptoken query parameter.
    private static string? ExtractSkipToken(string nextLink)
    {
        const string marker = "$skiptoken=";
        var i = nextLink.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (i < 0) return null;
        var rest = nextLink[(i + marker.Length)..];
        var amp = rest.IndexOf('&');
        var token = amp >= 0 ? rest[..amp] : rest;
        return Uri.UnescapeDataString(token);
    }

    public Task<bool> DiscardAsync(string messageId, string? internetMessageId, CancellationToken ct) =>
        AddCategoriesAsync(messageId, internetMessageId, new[] { TriageCategories.Triaged, TriageCategories.Discarded }, ct);

    public Task<bool> RestoreAsync(string messageId, string? internetMessageId, CancellationToken ct) =>
        RemoveCategoriesAsync(messageId, internetMessageId, new[] { TriageCategories.Triaged, TriageCategories.Discarded }, ct);

    public Task<bool> AssignAsync(string messageId, string? internetMessageId, string requestCategory, CancellationToken ct) =>
        AddCategoriesAsync(messageId, internetMessageId, new[] { TriageCategories.Triaged, requestCategory }, ct);

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
                if (await RemoveCategoriesAsync(id, null, new[] { TriageCategories.Triaged, requestCategory }, ct))
                {
                    cleared++;
                    any = true;
                }

            if (!any)
                break;
        }
        return cleared;
    }

    // --- Verified tag operations: write the categories, then read them back to confirm. ---

    private async Task<bool> AddCategoriesAsync(string messageId, string? imid, string[] add, CancellationToken ct)
    {
        var loaded = await LoadAsync(messageId, imid, ct);
        if (loaded is not { } m)
        {
            _logger.LogWarning("Tag-add skipped: message {MessageId} not found.", messageId);
            return false;
        }

        var updated = m.Categories.Concat(add).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (!await PatchCategoriesAsync(m.Id, updated, ct))
            return false;

        var after = await GetCategoriesAsync(m.Id, ct);
        var ok = after is not null && add.All(a => after.Contains(a, StringComparer.OrdinalIgnoreCase));
        if (!ok) _logger.LogWarning("Tag-add for {MessageId} did not verify.", messageId);
        return ok;
    }

    private async Task<bool> RemoveCategoriesAsync(string messageId, string? imid, string[] remove, CancellationToken ct)
    {
        var loaded = await LoadAsync(messageId, imid, ct);
        if (loaded is not { } m)
        {
            _logger.LogWarning("Tag-remove skipped: message {MessageId} not found.", messageId);
            return false;
        }

        var updated = m.Categories.Where(c => !remove.Contains(c, StringComparer.OrdinalIgnoreCase)).ToArray();
        if (!await PatchCategoriesAsync(m.Id, updated, ct))
            return false;

        var after = await GetCategoriesAsync(m.Id, ct);
        var ok = after is not null && !remove.Any(r => after.Contains(r, StringComparer.OrdinalIgnoreCase));
        if (!ok) _logger.LogWarning("Tag-remove for {MessageId} did not verify.", messageId);
        return ok;
    }

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

        return new MailboxMessage(id, imid, fromEmail, fromName, subject, preview, hasAttachments, receivedAt);
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
