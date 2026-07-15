using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Graph;

/// <summary>
/// Microsoft Graph REST client (HttpClient + app-only token). Deliberately uses raw REST rather
/// than the Graph SDK to keep the dependency surface small and the calls explicit.
/// </summary>
public sealed class GraphMailClient : IGraphMailClient
{
    private const string GraphBase = "https://graph.microsoft.com/v1.0";

    // Fields requested on every message. internetMessageHeaders is only returned when explicitly
    // selected; we use it to recover In-Reply-To / References for thread matching.
    private const string MessageSelect =
        "id,internetMessageId,conversationId,subject,bodyPreview,from,receivedDateTime,hasAttachments,internetMessageHeaders";

    private readonly HttpClient _http;
    private readonly GraphTokenProvider _tokens;
    private readonly MailboxIntakeOptions _options;
    private readonly ILogger<GraphMailClient> _logger;

    public GraphMailClient(
        HttpClient http,
        GraphTokenProvider tokens,
        MailboxIntakeOptions options,
        ILogger<GraphMailClient> logger)
    {
        _http = http;
        _tokens = tokens;
        _options = options;
        _logger = logger;
    }

    private string Mailbox => Uri.EscapeDataString(_options.Mailbox);

    public async Task<GraphMessagePage> GetDeltaPageAsync(string? link, CancellationToken ct)
    {
        // IMPORTANT: do NOT add $top to a /messages/delta query. On delta, Graph treats $top as a
        // cap on the *entire* initial enumeration (not a page size): it returns up to $top items,
        // then issues the @odata.deltaLink and declares the backlog complete — silently dropping the
        // rest of the folder. We rely on Graph's default delta paging via @odata.nextLink instead,
        // which the caller drains until the real deltaLink appears, so the whole Inbox is imported.
        var url = link ?? $"{GraphBase}/users/{Mailbox}/mailFolders/inbox/messages/delta"
            + $"?$select={MessageSelect}";

        // Keep the delta feed on its existing id type — its cursor is tied to the id type it was
        // created with. The ingested id is reconciled to an immutable one the first time the message
        // is acted on (the reconcile sweep refreshes a stale id).
        using var response = await SendAsync(HttpMethod.Get, url, content: null, ct, preferImmutableIds: false);
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        var messages = new List<GraphMessage>();
        if (root.TryGetProperty("value", out var value) && value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                var parsed = ParseMessage(item);
                if (parsed is not null)
                    messages.Add(parsed);
            }
        }

        string? nextLink = root.TryGetProperty("@odata.nextLink", out var n) ? n.GetString() : null;
        string? deltaLink = root.TryGetProperty("@odata.deltaLink", out var d) ? d.GetString() : null;
        return new GraphMessagePage(messages, nextLink, deltaLink);
    }

    public async Task<GraphMessage?> GetMessageAsync(string graphMessageId, CancellationToken ct)
    {
        var url = $"{GraphBase}/users/{Mailbox}/messages/{Uri.EscapeDataString(graphMessageId)}?$select={MessageSelect}";
        using var response = await SendAsync(HttpMethod.Get, url, content: null, ct, allowNotFound: true);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return ParseMessage(doc.RootElement);
    }

    public async Task<IReadOnlyList<GraphInboxItem>> ListInboxMessageIdentitiesAsync(CancellationToken ct)
    {
        // A normal (non-delta) collection read, so $top IS a real page size here (unlike delta). We
        // page via @odata.nextLink until the whole Inbox is enumerated. Only the two identity fields
        // are selected to keep the payload small.
        var items = new List<GraphInboxItem>();
        string? url = $"{GraphBase}/users/{Mailbox}/mailFolders/inbox/messages"
            + "?$select=id,internetMessageId&$top=100";

        while (!string.IsNullOrEmpty(url))
        {
            using var response = await SendAsync(HttpMethod.Get, url, content: null, ct);
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;

            if (root.TryGetProperty("value", out var value) && value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in value.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object) continue;
                    var id = item.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                    var imid = item.TryGetProperty("internetMessageId", out var imidEl) ? imidEl.GetString() : null;
                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(imid))
                        items.Add(new GraphInboxItem(id, imid));
                }
            }

            url = root.TryGetProperty("@odata.nextLink", out var n) ? n.GetString() : null;
        }

        return items;
    }

    public async Task<string> MoveMessageAsync(string graphMessageId, string destinationFolderId, CancellationToken ct)
    {
        var url = $"{GraphBase}/users/{Mailbox}/messages/{Uri.EscapeDataString(graphMessageId)}/move";
        var body = JsonContent.Create(new { destinationId = destinationFolderId });
        using var response = await SendAsync(HttpMethod.Post, url, body, ct);

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var newId = doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;
        if (string.IsNullOrEmpty(newId))
            throw new InvalidOperationException("Graph move did not return a new message id.");
        return newId;
    }

    public async Task<string?> GetMessageParentFolderIdAsync(string graphMessageId, CancellationToken ct)
    {
        var url = $"{GraphBase}/users/{Mailbox}/messages/{Uri.EscapeDataString(graphMessageId)}?$select=parentFolderId";
        using var response = await SendAsync(HttpMethod.Get, url, content: null, ct, allowNotFound: true);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return doc.RootElement.TryGetProperty("parentFolderId", out var p) ? p.GetString() : null;
    }

    public async Task<string?> GetFolderIdAsync(string wellKnownName, CancellationToken ct)
    {
        var url = $"{GraphBase}/users/{Mailbox}/mailFolders/{Uri.EscapeDataString(wellKnownName)}?$select=id";
        using var response = await SendAsync(HttpMethod.Get, url, content: null, ct, allowNotFound: true);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;
    }

    public async Task<string?> FindMessageIdByInternetMessageIdAsync(string internetMessageId, CancellationToken ct)
    {
        // internetMessageId is stable across folder moves; search the whole mailbox for it. The value
        // contains angle brackets and must be OData-escaped (double any single quote inside the literal).
        var escaped = internetMessageId.Replace("'", "''");
        var filter = Uri.EscapeDataString($"internetMessageId eq '{escaped}'");
        var url = $"{GraphBase}/users/{Mailbox}/messages?$filter={filter}&$select=id&$top=1";
        using var response = await SendAsync(HttpMethod.Get, url, content: null, ct, allowNotFound: true);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (doc.RootElement.TryGetProperty("value", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
            {
                if (item.TryGetProperty("id", out var idEl))
                {
                    var id = idEl.GetString();
                    if (!string.IsNullOrEmpty(id))
                        return id;
                }
            }
        }
        return null;
    }

    public async Task<string> EnsureFolderAsync(string displayName, string? parentFolderId, CancellationToken ct)
    {
        // Root-level folders live under /mailFolders; nested ones under the parent's /childFolders.
        var collectionUrl = string.IsNullOrEmpty(parentFolderId)
            ? $"{GraphBase}/users/{Mailbox}/mailFolders"
            : $"{GraphBase}/users/{Mailbox}/mailFolders/{Uri.EscapeDataString(parentFolderId)}/childFolders";

        // Find an existing folder with this exact name first so repeated triage never duplicates it.
        var escapedName = displayName.Replace("'", "''");
        var filter = Uri.EscapeDataString($"displayName eq '{escapedName}'");
        var findUrl = $"{collectionUrl}?$select=id,displayName&$filter={filter}";
        using (var found = await SendAsync(HttpMethod.Get, findUrl, content: null, ct))
        {
            await using var stream = await found.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            if (doc.RootElement.TryGetProperty("value", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var f in arr.EnumerateArray())
                {
                    var existing = f.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                    if (!string.IsNullOrEmpty(existing))
                        return existing;
                }
            }
        }

        // Not present: create it.
        var createBody = JsonContent.Create(new { displayName });
        using (var created = await SendAsync(HttpMethod.Post, collectionUrl, createBody, ct))
        {
            await using var stream = await created.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var id = doc.RootElement.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            if (string.IsNullOrEmpty(id))
                throw new InvalidOperationException("Graph create folder did not return an id.");
            return id;
        }
    }

    public async Task<GraphDraft?> CreateDraftAsync(GraphOutboundMessage message, CancellationToken ct)
    {
        // POST /users/{mailbox}/messages creates the message in the Drafts folder and CANNOT send.
        // A human reviews the draft in Outlook and presses Send — code never calls /send or /sendMail.
        var url = $"{GraphBase}/users/{Mailbox}/messages";

        static object Recipient(GraphRecipient r) =>
            new { emailAddress = new { address = r.Email, name = r.Name ?? r.Email } };

        var toRecipients = message.To.Select(Recipient).ToArray();

        // Graph attachments need the "@odata.type" property name, which an anonymous type cannot
        // express, so each attachment is built as a dictionary that serialises to the right shape.
        var attachments = (message.Attachments ?? Array.Empty<GraphAttachment>())
            .Select(a => new Dictionary<string, object>
            {
                ["@odata.type"] = "#microsoft.graph.fileAttachment",
                ["name"] = a.FileName,
                ["contentType"] = a.ContentType,
                ["contentBytes"] = Convert.ToBase64String(a.Content)
            })
            .ToArray();

        var messageBody = new Dictionary<string, object?>
        {
            ["subject"] = message.Subject,
            ["body"] = new { contentType = "HTML", content = message.HtmlBody },
            ["toRecipients"] = toRecipients
        };
        if (attachments.Length > 0)
            messageBody["attachments"] = attachments;
        if (message.Cc is { Count: > 0 } cc)
            messageBody["ccRecipients"] = cc.Select(Recipient).ToArray();
        if (message.Bcc is { Count: > 0 } bcc)
            messageBody["bccRecipients"] = bcc.Select(Recipient).ToArray();
        // Workflow categories ride on the draft and survive the send, keeping the sent copy — and,
        // via the thread sweep, its replies — associated with the record that drafted it.
        if (message.Categories is { Count: > 0 } categories)
            messageBody["categories"] = categories.ToArray();

        using var response = await SendAsync(HttpMethod.Post, url, JsonContent.Create(messageBody), ct);
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;
        var id = root.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        if (string.IsNullOrEmpty(id))
            return null;
        var webLink = root.TryGetProperty("webLink", out var wl) ? wl.GetString() : null;
        return new GraphDraft(id, webLink);
    }

    public async Task<GraphSubscription> CreateSubscriptionAsync(
        string notificationUrl, string clientState, DateTimeOffset expiry, CancellationToken ct)
    {
        var url = $"{GraphBase}/subscriptions";
        var payload = new
        {
            changeType = "created",
            notificationUrl,
            resource = $"users/{_options.Mailbox}/mailFolders('inbox')/messages",
            expirationDateTime = expiry.UtcDateTime.ToString("o"),
            clientState
        };
        using var response = await SendAsync(HttpMethod.Post, url, JsonContent.Create(payload), ct);
        return await ReadSubscriptionAsync(response, ct);
    }

    public async Task<GraphSubscription> RenewSubscriptionAsync(
        string subscriptionId, DateTimeOffset expiry, CancellationToken ct)
    {
        var url = $"{GraphBase}/subscriptions/{Uri.EscapeDataString(subscriptionId)}";
        var payload = new { expirationDateTime = expiry.UtcDateTime.ToString("o") };
        using var response = await SendAsync(HttpMethod.Patch, url, JsonContent.Create(payload), ct);
        return await ReadSubscriptionAsync(response, ct);
    }

    public async Task DeleteSubscriptionAsync(string subscriptionId, CancellationToken ct)
    {
        var url = $"{GraphBase}/subscriptions/{Uri.EscapeDataString(subscriptionId)}";
        using var response = await SendAsync(HttpMethod.Delete, url, content: null, ct, allowNotFound: true);
        _ = response;
    }

    private static async Task<GraphSubscription> ReadSubscriptionAsync(HttpResponseMessage response, CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;
        var id = root.GetProperty("id").GetString() ?? "";
        var expires = root.TryGetProperty("expirationDateTime", out var e) && e.TryGetDateTimeOffset(out var dt)
            ? dt
            : DateTimeOffset.UtcNow;
        return new GraphSubscription(id, expires);
    }

    // Exchange Online throttles per-mailbox concurrency/rate with 429 (and occasionally 503). We
    // honour the server's Retry-After and retry a bounded number of times so a transient throttle
    // self-heals inside the invocation rather than failing the queue message.
    private const int MaxAttempts = 5;
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string url, HttpContent? content, CancellationToken ct,
        bool allowNotFound = false, bool preferImmutableIds = true)
    {
        // Buffer the body once: an HttpContent instance can only be sent a single time, but a
        // throttled request must be re-sent, so we rebuild the content from bytes on each attempt.
        byte[]? body = null;
        MediaTypeHeaderValue? contentType = null;
        if (content is not null)
        {
            body = await content.ReadAsByteArrayAsync(ct);
            contentType = content.Headers.ContentType;
            content.Dispose();
        }

        for (var attempt = 1; ; attempt++)
        {
            var token = await _tokens.GetTokenAsync(ct);
            using var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            // Ask Graph for immutable message/folder ids. By default Graph returns "default" ids that
            // change whenever a message moves or the mailbox changes, which makes a freshly-stored id
            // go stale within seconds and makes move/lookup operations land on the wrong copy. Immutable
            // ids never change across moves, so a stored id keeps resolving and moves stay deterministic.
            // NOT applied to the /messages/delta call: a delta cursor is tied to the id type it was
            // created with, and Graph rejects a mismatched id type mid-enumeration.
            if (preferImmutableIds)
                request.Headers.TryAddWithoutValidation("Prefer", "IdType=\"ImmutableId\"");
            if (body is not null)
            {
                request.Content = new ByteArrayContent(body);
                request.Content.Headers.ContentType = contentType;
            }

            var response = await _http.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
                return response;
            if (allowNotFound && response.StatusCode == HttpStatusCode.NotFound)
                return response;

            var throttled = response.StatusCode == HttpStatusCode.TooManyRequests
                || response.StatusCode == HttpStatusCode.ServiceUnavailable;
            if (throttled && attempt < MaxAttempts)
            {
                var delay = RetryDelay(response, attempt);
                _logger.LogWarning(
                    "Graph {Method} throttled ({Status}); attempt {Attempt}/{Max}, backing off {DelaySeconds:0.0}s.",
                    method, (int)response.StatusCode, attempt, MaxAttempts, delay.TotalSeconds);
                response.Dispose();
                await Task.Delay(delay, ct);
                continue;
            }

            var detail = await SafeReadAsync(response, ct);
            response.Dispose();
            throw new GraphRequestException(
                $"Graph {method} {Sanitise(url)} failed: {(int)response.StatusCode} {response.StatusCode}. {detail}");
        }
    }

    private static TimeSpan RetryDelay(HttpResponseMessage response, int attempt)
    {
        // Prefer the server's Retry-After hint (delta seconds or an absolute date); fall back to
        // exponential backoff with a little jitter to avoid every parallel worker waking together.
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is { } delta && delta > TimeSpan.Zero)
            return Cap(delta);
        if (retryAfter?.Date is { } date)
        {
            var until = date - DateTimeOffset.UtcNow;
            if (until > TimeSpan.Zero)
                return Cap(until);
        }

        var seconds = Math.Min(MaxRetryDelay.TotalSeconds, Math.Pow(2, attempt)); // 2, 4, 8, 16, 30
        return TimeSpan.FromSeconds(seconds + Random.Shared.NextDouble());
    }

    private static TimeSpan Cap(TimeSpan value) => value > MaxRetryDelay ? MaxRetryDelay : value;

    private static async Task<string> SafeReadAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try { return await response.Content.ReadAsStringAsync(ct); }
        catch { return "(no body)"; }
    }

    // Strip the mailbox address out of logged URLs.
    private string Sanitise(string url) => url.Replace(Mailbox, "{mailbox}").Replace(_options.Mailbox, "{mailbox}");

    private static GraphMessage? ParseMessage(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object)
            return null;

        var id = item.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        if (string.IsNullOrEmpty(id))
            return null;

        // Deleted/removed entries in a delta feed carry an @removed annotation and little else.
        if (item.TryGetProperty("@removed", out _))
            return new GraphMessage(id, "", null, null, null, "", "", "", "", false, default, IsRemoved: true);

        string internetMessageId = item.TryGetProperty("internetMessageId", out var imid) ? imid.GetString() ?? "" : "";
        string? conversationId = item.TryGetProperty("conversationId", out var conv) ? conv.GetString() : null;
        string subject = item.TryGetProperty("subject", out var subj) ? subj.GetString() ?? "" : "";
        string bodyPreview = item.TryGetProperty("bodyPreview", out var bp) ? bp.GetString() ?? "" : "";
        bool hasAttachments = item.TryGetProperty("hasAttachments", out var ha) && ha.ValueKind == JsonValueKind.True;

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

        string? inReplyTo = null, references = null;
        if (item.TryGetProperty("internetMessageHeaders", out var headers) && headers.ValueKind == JsonValueKind.Array)
        {
            foreach (var h in headers.EnumerateArray())
            {
                var name = h.TryGetProperty("name", out var hn) ? hn.GetString() : null;
                var hv = h.TryGetProperty("value", out var hvEl) ? hvEl.GetString() : null;
                if (string.Equals(name, "In-Reply-To", StringComparison.OrdinalIgnoreCase)) inReplyTo = hv;
                else if (string.Equals(name, "References", StringComparison.OrdinalIgnoreCase)) references = hv;
            }
        }

        return new GraphMessage(
            id, internetMessageId, conversationId, inReplyTo, references,
            fromEmail, fromName, subject, bodyPreview, hasAttachments, receivedAt, IsRemoved: false);
    }
}

/// <summary>Raised when a Graph REST call returns a non-success status. Carries a sanitised message.</summary>
public sealed class GraphRequestException : Exception
{
    public GraphRequestException(string message) : base(message) { }
}
