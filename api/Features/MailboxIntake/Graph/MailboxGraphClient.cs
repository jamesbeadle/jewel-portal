using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Graph;

/// <summary>
/// Live mailbox access for the triage screen: read a page of a folder's messages and move a message
/// between folders. This is the engine of the live-read model — the Inbox is the triage queue and
/// the "General" folder is the discarded pile, both read fresh on each request, and every triage
/// action is a single move. No state is mirrored in the database.
///
/// All calls request immutable ids (<c>Prefer: IdType="ImmutableId"</c>) so a message's id stays
/// valid across a move; a move additionally re-finds the message by its stable internetMessageId if
/// the supplied id has gone stale, and skips a move into the folder the message already occupies
/// (Graph duplicates a same-folder move).
/// </summary>
public interface IMailboxGraphClient
{
    Task<PagedResult<MailboxMessage>> ListInboxAsync(int skip, int take, CancellationToken ct);
    Task<PagedResult<MailboxMessage>> ListDiscardedAsync(int skip, int take, CancellationToken ct);

    /// <summary>Move a message from the Inbox into the "General" (discarded) folder. Returns false if it can't be found.</summary>
    Task<bool> DiscardAsync(string messageId, string? internetMessageId, CancellationToken ct);

    /// <summary>Move a message from "General" back into the Inbox. Returns false if it can't be found.</summary>
    Task<bool> RestoreAsync(string messageId, string? internetMessageId, CancellationToken ct);

    /// <summary>Read the fields needed to record a message against a request, or null if it's gone.</summary>
    Task<MailboxSnapshot?> GetSnapshotAsync(string messageId, string? internetMessageId, CancellationToken ct);

    /// <summary>
    /// Ensure the request's folder (named e.g. "REQ-0001" under the "Requests" parent) and move the
    /// message into it. Returns the folder id so it can be stored on the request, or null on failure.
    /// </summary>
    Task<string?> MoveToRequestFolderAsync(string messageId, string? internetMessageId, string requestFolderName, CancellationToken ct);

    /// <summary>Move every message in a folder back into the Inbox (used to return a request to triage).
    /// Returns the number of messages moved.</summary>
    Task<int> ReturnFolderMessagesToInboxAsync(string folderId, CancellationToken ct);
}

/// <summary>The subset of a mailbox message recorded against a request when an email is assigned to
/// it: the author, the (preview) body, the received time, and the threading ids so later replies can
/// be stitched back.</summary>
public sealed record MailboxSnapshot(
    string InternetMessageId,
    string? ConversationId,
    string? InReplyTo,
    string FromEmail,
    string FromName,
    string Subject,
    string BodyPreview,
    DateTimeOffset ReceivedAt);

/// <summary>No-op client used when Graph credentials aren't configured for the API: triage shows
/// empty and actions report no-op rather than failing.</summary>
public sealed class NullMailboxGraphClient : IMailboxGraphClient
{
    private static PagedResult<MailboxMessage> Empty(int skip, int take) =>
        new(Array.Empty<MailboxMessage>(), 0, skip, take);

    public Task<PagedResult<MailboxMessage>> ListInboxAsync(int skip, int take, CancellationToken ct) =>
        Task.FromResult(Empty(skip, take));

    public Task<PagedResult<MailboxMessage>> ListDiscardedAsync(int skip, int take, CancellationToken ct) =>
        Task.FromResult(Empty(skip, take));

    public Task<bool> DiscardAsync(string messageId, string? internetMessageId, CancellationToken ct) => Task.FromResult(false);
    public Task<bool> RestoreAsync(string messageId, string? internetMessageId, CancellationToken ct) => Task.FromResult(false);
    public Task<MailboxSnapshot?> GetSnapshotAsync(string messageId, string? internetMessageId, CancellationToken ct) => Task.FromResult<MailboxSnapshot?>(null);
    public Task<string?> MoveToRequestFolderAsync(string messageId, string? internetMessageId, string requestFolderName, CancellationToken ct) => Task.FromResult<string?>(null);
    public Task<int> ReturnFolderMessagesToInboxAsync(string folderId, CancellationToken ct) => Task.FromResult(0);
}

/// <summary>Graph REST implementation (HttpClient + app-only token), in the same style as the API's
/// message reader.</summary>
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

    public Task<PagedResult<MailboxMessage>> ListInboxAsync(int skip, int take, CancellationToken ct) =>
        ListFolderAsync("inbox", skip, take, ct);

    public async Task<PagedResult<MailboxMessage>> ListDiscardedAsync(int skip, int take, CancellationToken ct)
    {
        // The discarded pile is a top-level folder (sibling of the Inbox); if it doesn't exist yet
        // there is nothing discarded.
        var discardFolder = await EnsureFolderAsync(_options.DiscardFolder, parent: null, ct);
        if (string.IsNullOrEmpty(discardFolder))
            return new PagedResult<MailboxMessage>(Array.Empty<MailboxMessage>(), 0, skip, take);

        return await ListFolderAsync(discardFolder, skip, take, ct);
    }

    private async Task<PagedResult<MailboxMessage>> ListFolderAsync(string folder, int skip, int take, CancellationToken ct)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 100);

        var url = $"{GraphBase}/users/{Mailbox}/mailFolders/{Uri.EscapeDataString(folder)}/messages"
            + $"?$select={Summary}&$orderby=receivedDateTime%20desc&$top={take}&$skip={skip}";

        var items = new List<MailboxMessage>();
        using (var response = await SendAsync(HttpMethod.Get, url, content: null, ct, allowNotFound: true))
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
                return new PagedResult<MailboxMessage>(items, 0, skip, take);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Mailbox list {Folder} failed: {Status}.", folder, (int)response.StatusCode);
                return new PagedResult<MailboxMessage>(items, 0, skip, take);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            if (doc.RootElement.TryGetProperty("value", out var arr) && arr.ValueKind == JsonValueKind.Array)
                foreach (var item in arr.EnumerateArray())
                    if (Parse(item) is { } message)
                        items.Add(message);
        }

        var total = await CountFolderAsync(folder, ct) ?? (skip + items.Count);
        return new PagedResult<MailboxMessage>(items, total, skip, take);
    }

    private async Task<int?> CountFolderAsync(string folder, CancellationToken ct)
    {
        var url = $"{GraphBase}/users/{Mailbox}/mailFolders/{Uri.EscapeDataString(folder)}/messages/$count";
        using var response = await SendAsync(HttpMethod.Get, url, content: null, ct, allowNotFound: true, consistencyEventual: true);
        if (!response.IsSuccessStatusCode)
            return null;
        var text = await response.Content.ReadAsStringAsync(ct);
        return int.TryParse(text, out var count) ? count : null;
    }

    public async Task<bool> DiscardAsync(string messageId, string? internetMessageId, CancellationToken ct)
    {
        // Top-level folder (sibling of the Inbox), found-or-created on demand.
        var discardFolder = await EnsureFolderAsync(_options.DiscardFolder, parent: null, ct);
        if (string.IsNullOrEmpty(discardFolder))
        {
            _logger.LogWarning("Discard skipped: could not resolve the '{Folder}' folder.", _options.DiscardFolder);
            return false;
        }
        return await MoveAsync(messageId, internetMessageId, discardFolder, ct);
    }

    public Task<bool> RestoreAsync(string messageId, string? internetMessageId, CancellationToken ct) =>
        MoveAsync(messageId, internetMessageId, "inbox", ct);

    public async Task<MailboxSnapshot?> GetSnapshotAsync(string messageId, string? internetMessageId, CancellationToken ct)
    {
        var (liveId, _) = await ResolveLiveAsync(messageId, internetMessageId, ct);
        if (string.IsNullOrEmpty(liveId))
            return null;

        var url = $"{GraphBase}/users/{Mailbox}/messages/{Uri.EscapeDataString(liveId)}"
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

    public async Task<string?> MoveToRequestFolderAsync(string messageId, string? internetMessageId, string requestFolderName, CancellationToken ct)
    {
        // The request folder is "<requestFolderName>" (e.g. REQ-0001) under the top-level "Requests" parent.
        var parentId = await EnsureFolderAsync(_options.RequestsParentFolder, parent: null, ct);
        if (string.IsNullOrEmpty(parentId))
            return null;
        var folderId = await EnsureFolderAsync(requestFolderName, parentId, ct);
        if (string.IsNullOrEmpty(folderId))
            return null;
        return await MoveAsync(messageId, internetMessageId, folderId, ct) ? folderId : null;
    }

    public async Task<int> ReturnFolderMessagesToInboxAsync(string folderId, CancellationToken ct)
    {
        var moved = 0;
        // Each move removes a message from the folder, so re-read from the start each pass. Bounded so
        // a persistently-failing move can't spin forever.
        for (var guard = 0; guard < 100; guard++)
        {
            var page = await ListFolderAsync(folderId, 0, 50, ct);
            if (page.Items.Count == 0)
                break;

            var movedThisPass = 0;
            foreach (var m in page.Items)
                if (await MoveAsync(m.Id, m.InternetMessageId, "inbox", ct))
                {
                    moved++;
                    movedThisPass++;
                }

            if (movedThisPass == 0)
                break;
        }
        return moved;
    }

    /// <summary>
    /// Move a message to a destination folder, healing a stale id and refusing a same-folder move.
    /// Returns false when the message cannot be found at all (already deleted) so callers don't fail.
    /// </summary>
    private async Task<bool> MoveAsync(string messageId, string? internetMessageId, string destination, CancellationToken ct)
    {
        var (liveId, currentFolderId) = await ResolveLiveAsync(messageId, internetMessageId, ct);
        if (string.IsNullOrEmpty(liveId))
        {
            _logger.LogWarning("Move skipped: message {MessageId} not found in the mailbox.", messageId);
            return false;
        }

        // Same-folder guard: Graph's /move duplicates a message when the destination is the folder it
        // already occupies, so resolve the destination to a concrete id and compare.
        var destinationId = await GetFolderIdAsync(destination, ct) ?? destination;
        if (!string.IsNullOrEmpty(currentFolderId) && string.Equals(currentFolderId, destinationId, StringComparison.Ordinal))
            return true;

        var url = $"{GraphBase}/users/{Mailbox}/messages/{Uri.EscapeDataString(liveId)}/move";
        var body = JsonContent.Create(new { destinationId = destination });
        using var response = await SendAsync(HttpMethod.Post, url, body, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Move of message {MessageId} to {Destination} failed: {Status}.",
                messageId, destination, (int)response.StatusCode);
            return false;
        }
        return true;
    }

    /// <summary>Resolve the message's live id + current folder, re-finding it by internetMessageId if
    /// the supplied id no longer resolves. Returns (null, null) if it cannot be found at all.</summary>
    private async Task<(string? Id, string? FolderId)> ResolveLiveAsync(string messageId, string? internetMessageId, CancellationToken ct)
    {
        var folderId = await GetParentFolderIdAsync(messageId, ct);
        if (!string.IsNullOrEmpty(folderId))
            return (messageId, folderId);

        if (string.IsNullOrEmpty(internetMessageId))
            return (null, null);

        var foundId = await FindByInternetMessageIdAsync(internetMessageId, ct);
        if (string.IsNullOrEmpty(foundId))
            return (null, null);

        return (foundId, await GetParentFolderIdAsync(foundId, ct));
    }

    private async Task<string?> GetParentFolderIdAsync(string messageId, CancellationToken ct)
    {
        var url = $"{GraphBase}/users/{Mailbox}/messages/{Uri.EscapeDataString(messageId)}?$select=parentFolderId";
        using var response = await SendAsync(HttpMethod.Get, url, content: null, ct, allowNotFound: true);
        if (!response.IsSuccessStatusCode)
            return null;
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return doc.RootElement.TryGetProperty("parentFolderId", out var p) ? p.GetString() : null;
    }

    private async Task<string?> GetFolderIdAsync(string wellKnownName, CancellationToken ct)
    {
        var url = $"{GraphBase}/users/{Mailbox}/mailFolders/{Uri.EscapeDataString(wellKnownName)}?$select=id";
        using var response = await SendAsync(HttpMethod.Get, url, content: null, ct, allowNotFound: true);
        if (!response.IsSuccessStatusCode)
            return null;
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;
    }

    private async Task<string?> FindByInternetMessageIdAsync(string internetMessageId, CancellationToken ct)
    {
        var escaped = internetMessageId.Replace("'", "''");
        var filter = Uri.EscapeDataString($"internetMessageId eq '{escaped}'");
        var url = $"{GraphBase}/users/{Mailbox}/messages?$filter={filter}&$select=id&$top=1";
        using var response = await SendAsync(HttpMethod.Get, url, content: null, ct, allowNotFound: true);
        if (!response.IsSuccessStatusCode)
            return null;
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (doc.RootElement.TryGetProperty("value", out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var item in arr.EnumerateArray())
                if (item.TryGetProperty("id", out var idEl) && idEl.GetString() is { Length: > 0 } id)
                    return id;
        return null;
    }

    /// <summary>Find-or-create a folder by name. A null/empty parent means a top-level (mailbox-root)
    /// folder; otherwise the parent is a folder id or a well-known name like "inbox". Returns the
    /// folder's id, or null on failure.</summary>
    private async Task<string?> EnsureFolderAsync(string displayName, string? parent, CancellationToken ct)
    {
        var collection = string.IsNullOrEmpty(parent)
            ? $"{GraphBase}/users/{Mailbox}/mailFolders"
            : $"{GraphBase}/users/{Mailbox}/mailFolders/{Uri.EscapeDataString(parent)}/childFolders";

        var escaped = displayName.Replace("'", "''");
        var findUrl = $"{collection}?$select=id,displayName&$filter={Uri.EscapeDataString($"displayName eq '{escaped}'")}";
        using (var found = await SendAsync(HttpMethod.Get, findUrl, content: null, ct, allowNotFound: true))
        {
            if (found.IsSuccessStatusCode)
            {
                await using var stream = await found.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                if (doc.RootElement.TryGetProperty("value", out var arr) && arr.ValueKind == JsonValueKind.Array)
                    foreach (var f in arr.EnumerateArray())
                        if (f.TryGetProperty("id", out var idEl) && idEl.GetString() is { Length: > 0 } existing)
                            return existing;
            }
        }

        using var created = await SendAsync(HttpMethod.Post, collection, JsonContent.Create(new { displayName }), ct);
        if (!created.IsSuccessStatusCode)
        {
            _logger.LogWarning("Ensure-folder '{Name}' failed: {Status}.", displayName, (int)created.StatusCode);
            return null;
        }
        await using var cstream = await created.Content.ReadAsStreamAsync(ct);
        using var cdoc = await JsonDocument.ParseAsync(cstream, cancellationToken: ct);
        return cdoc.RootElement.TryGetProperty("id", out var cid) ? cid.GetString() : null;
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
        // Immutable ids so a stored id keeps resolving across moves (see class summary).
        request.Headers.TryAddWithoutValidation("Prefer", "IdType=\"ImmutableId\"");
        if (consistencyEventual)
            request.Headers.TryAddWithoutValidation("ConsistencyLevel", "eventual");

        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode && !(allowNotFound && response.StatusCode == HttpStatusCode.NotFound))
        {
            var detail = await SafeBodyAsync(response, ct);
            _logger.LogWarning("Graph {Method} {Status} for a mailbox call. {Detail}",
                method, (int)response.StatusCode, detail);
        }
        return response;
    }

    private static async Task<string> SafeBodyAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try { return await response.Content.ReadAsStringAsync(ct); } catch { return "(no body)"; }
    }
}
