using System.Net;
using System.Net.Http.Headers;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Graph;

public sealed partial class MailboxGraphClient
{
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

}
