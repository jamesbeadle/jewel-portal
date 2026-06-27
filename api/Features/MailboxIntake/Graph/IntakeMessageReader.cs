using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Graph;

/// <summary>Full on-demand content of one mailbox message: the (raw, unsanitised) HTML or text
/// body plus its real, non-inline attachments. Sanitisation happens in the handler, not here.</summary>
public sealed record IntakeMessageContent(string Body, bool IsHtml, IReadOnlyList<IntakeMessageAttachment> Attachments);

public sealed record IntakeMessageAttachment(string Name, long Size, string? ContentType);

/// <summary>
/// Reads a single mailbox message's full body and attachment metadata from Microsoft Graph, on
/// demand, when a triager opens an email. Deliberately read-only and narrow — the producer/webhook
/// API has no other Graph reach; the background sweep/move/send paths live in the worker.
/// </summary>
public interface IIntakeMessageReader
{
    /// <summary>Fetch body + attachments for a Graph message id, or null if it can't be retrieved.</summary>
    Task<IntakeMessageContent?> GetAsync(string graphMessageId, CancellationToken ct);
}

/// <summary>No-op reader used when Graph credentials aren't configured for the API. Returns null so
/// callers fall back to the stored preview rather than failing.</summary>
public sealed class NullIntakeMessageReader : IIntakeMessageReader
{
    public Task<IntakeMessageContent?> GetAsync(string graphMessageId, CancellationToken ct) =>
        Task.FromResult<IntakeMessageContent?>(null);
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
                    attachments.Add(new IntakeMessageAttachment(name, size, type));
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
}
