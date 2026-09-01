using System.Net;
using System.Net.Http.Headers;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Graph;

public sealed partial class MailboxGraphClient
{
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

}
