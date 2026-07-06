using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Graph;

/// <summary>Full on-demand content of one mailbox message: the (raw, unsanitised) HTML or text
/// body plus its real, non-inline attachments. Sanitisation happens in the handler, not here.</summary>
public sealed record IntakeMessageContent(string Body, bool IsHtml, IReadOnlyList<IntakeMessageAttachment> Attachments);

// Id is the Graph attachment id, used to download the attachment's bytes on demand (e.g. saving a
// drawing out of a triaged email). Optional so existing metadata-only callers are unchanged.
public sealed record IntakeMessageAttachment(string Name, long Size, string? ContentType, string Id = "");

/// <summary>One downloaded attachment: its bytes plus the metadata needed to store it elsewhere.</summary>
public sealed record IntakeAttachmentContent(string Name, string ContentType, byte[] Content);

/// <summary>
/// Reads a single mailbox message's full body and attachment metadata from Microsoft Graph, on
/// demand, when a triager opens an email. Deliberately read-only and narrow — the producer/webhook
/// API has no other Graph reach; the background sweep/move/send paths live in the worker.
/// </summary>
public interface IIntakeMessageReader
{
    /// <summary>Fetch body + attachments for a Graph message id, or null if it can't be retrieved.</summary>
    Task<IntakeMessageContent?> GetAsync(string graphMessageId, CancellationToken ct);

    /// <summary>Download one attachment's bytes (a Graph fileAttachment), or null if it can't be
    /// retrieved / isn't a file attachment (item and reference attachments have no bytes).</summary>
    Task<IntakeAttachmentContent?> GetAttachmentAsync(string graphMessageId, string attachmentId, CancellationToken ct);
}

/// <summary>No-op reader used when Graph credentials aren't configured for the API. Returns null so
/// callers fall back to the stored preview rather than failing.</summary>
public sealed class NullIntakeMessageReader : IIntakeMessageReader
{
    public Task<IntakeMessageContent?> GetAsync(string graphMessageId, CancellationToken ct) =>
        Task.FromResult<IntakeMessageContent?>(null);
    public Task<IntakeAttachmentContent?> GetAttachmentAsync(string graphMessageId, string attachmentId, CancellationToken ct) =>
        Task.FromResult<IntakeAttachmentContent?>(null);
}

/// <summary>Graph REST implementation (HttpClient + app-only token), matching the worker's style.</summary>
public sealed class GraphIntakeMessageReader : IIntakeMessageReader
{
    private const string GraphBase = "https://graph.microsoft.com/v1.0";

    private readonly HttpClient _http;
    private readonly GraphTokenProvider _tokens;
    private readonly MailboxIntakeOptions _options;
    private readonly ILogger<GraphIntakeMessageReader> _logger;

    public GraphIntakeMessageReader(
        HttpClient http, GraphTokenProvider tokens, MailboxIntakeOptions options, ILogger<GraphIntakeMessageReader> logger)
    {
        _http = http;
        _tokens = tokens;
        _options = options;
        _logger = logger;
    }

    private string Mailbox => Uri.EscapeDataString(_options.Mailbox);

    public async Task<IntakeMessageContent?> GetAsync(string graphMessageId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(graphMessageId))
            return null;

        // Pull the full body plus non-inline attachment metadata in a single round trip.
        var url = $"{GraphBase}/users/{Mailbox}/messages/{Uri.EscapeDataString(graphMessageId)}"
            + "?$select=body,hasAttachments"
            + "&$expand=attachments($select=id,name,size,contentType,isInline)";

        try
        {
            var token = await _tokens.GetTokenAsync(ct);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _http.SendAsync(request, ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Intake message {GraphId} not found in mailbox.", graphMessageId);
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Graph message read failed: {Status}.", (int)response.StatusCode);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;

            string body = "";
            bool isHtml = true;
            if (root.TryGetProperty("body", out var bodyEl) && bodyEl.ValueKind == JsonValueKind.Object)
            {
                body = bodyEl.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
                var contentType = bodyEl.TryGetProperty("contentType", out var ctEl) ? ctEl.GetString() : "html";
                isHtml = !string.Equals(contentType, "text", StringComparison.OrdinalIgnoreCase);
            }

            var attachments = new List<IntakeMessageAttachment>();
            if (root.TryGetProperty("attachments", out var atts) && atts.ValueKind == JsonValueKind.Array)
            {
                foreach (var att in atts.EnumerateArray())
                {
                    // Skip inline attachments (embedded images etc.) — they're part of the body, not files.
                    if (att.TryGetProperty("isInline", out var inline) && inline.ValueKind == JsonValueKind.True)
                        continue;

                    var name = att.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    if (string.IsNullOrWhiteSpace(name)) name = "(unnamed attachment)";
                    long size = att.TryGetProperty("size", out var s) && s.TryGetInt64(out var sz) ? sz : 0;
                    string? type = att.TryGetProperty("contentType", out var t) ? t.GetString() : null;
                    var attachmentId = att.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
                    attachments.Add(new IntakeMessageAttachment(name, size, type, attachmentId));
                }
            }

            return new IntakeMessageContent(body, isHtml, attachments);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Intake message read errored for {GraphId}.", graphMessageId);
            return null;
        }
    }

    public async Task<IntakeAttachmentContent?> GetAttachmentAsync(string graphMessageId, string attachmentId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(graphMessageId) || string.IsNullOrEmpty(attachmentId))
            return null;

        // A fileAttachment carries its bytes as base64 contentBytes in the attachment resource.
        var url = $"{GraphBase}/users/{Mailbox}/messages/{Uri.EscapeDataString(graphMessageId)}"
            + $"/attachments/{Uri.EscapeDataString(attachmentId)}";

        try
        {
            var token = await _tokens.GetTokenAsync(ct);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Graph attachment read failed: {Status}.", (int)response.StatusCode);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;

            // Only fileAttachment has contentBytes; item/reference attachments are not downloadable files.
            if (!root.TryGetProperty("contentBytes", out var bytesEl) || bytesEl.ValueKind != JsonValueKind.String)
            {
                _logger.LogWarning("Attachment {AttachmentId} is not a file attachment.", attachmentId);
                return null;
            }

            var name = root.TryGetProperty("name", out var n) ? n.GetString() ?? "attachment" : "attachment";
            var contentType = root.TryGetProperty("contentType", out var t) ? t.GetString() ?? "application/octet-stream" : "application/octet-stream";
            var content = Convert.FromBase64String(bytesEl.GetString() ?? "");
            return new IntakeAttachmentContent(name, contentType, content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Attachment read errored for {GraphId}/{AttachmentId}.", graphMessageId, attachmentId);
            return null;
        }
    }
}
