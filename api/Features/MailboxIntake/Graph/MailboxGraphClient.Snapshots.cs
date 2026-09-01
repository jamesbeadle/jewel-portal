using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Jewel.JPMS.Models;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Graph;

public sealed partial class MailboxGraphClient
{
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

}
