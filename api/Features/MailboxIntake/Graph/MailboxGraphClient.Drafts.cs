using System.Net;
using System.Net.Http.Headers;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Graph;

public sealed partial class MailboxGraphClient
{
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

}
