using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Jewel.JPMS.Models;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Graph;

public sealed partial class MailboxGraphClient
{
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

}
