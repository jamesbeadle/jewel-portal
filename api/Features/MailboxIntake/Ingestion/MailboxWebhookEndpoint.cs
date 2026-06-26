using System.Text.Json;
using Jewel.JPMS.Api.Features.MailboxIntake.Queue;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Ingestion;

/// <summary>
/// Receives Microsoft Graph change notifications for the projects@ Inbox.
///
/// Two jobs: (1) answer the subscription validation handshake by echoing the validationToken;
/// (2) for each genuine notification, verify the clientState then enqueue the message id for the
/// worker to fetch + ingest. We deliberately do NOT do DB work inline — Graph requires a fast
/// (~3s) response, and enqueuing gives us retries + dead-lettering. The delta sweep is the
/// completeness backstop if a notification is ever missed.
/// </summary>
public sealed class MailboxWebhookEndpoint
{
    private readonly MailboxIntakeOptions _options;
    private readonly IMailboxQueue _queue;
    private readonly ILogger<MailboxWebhookEndpoint> _logger;

    public MailboxWebhookEndpoint(MailboxIntakeOptions options, IMailboxQueue queue, ILogger<MailboxWebhookEndpoint> logger)
    {
        _options = options;
        _queue = queue;
        _logger = logger;
    }

    [Function(nameof(MailboxWebhookEndpoint))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mailbox/webhook")] HttpRequest request)
    {
        var ct = request.HttpContext.RequestAborted;

        // 1) Validation handshake: echo the token back as text/plain.
        if (request.Query.TryGetValue("validationToken", out var validationToken))
        {
            return new ContentResult
            {
                Content = validationToken.ToString(),
                ContentType = "text/plain",
                StatusCode = StatusCodes.Status200OK
            };
        }

        // 2) Real notification(s).
        string body;
        using (var reader = new StreamReader(request.Body))
            body = await reader.ReadToEndAsync(ct);

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("value", out var value) && value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var notification in value.EnumerateArray())
                    {
                        var clientState = notification.TryGetProperty("clientState", out var cs) ? cs.GetString() : null;
                        if (!string.Equals(clientState, _options.ClientState, StringComparison.Ordinal))
                        {
                            _logger.LogWarning("Rejected mailbox notification with mismatched clientState.");
                            continue;
                        }

                        var messageId = ExtractMessageId(notification);
                        if (string.IsNullOrEmpty(messageId))
                        {
                            _logger.LogWarning("Mailbox notification had no resource id; skipping.");
                            continue;
                        }

                        await _queue.EnqueueIntakeNotificationAsync(messageId, ct);
                    }
                }
            }
            catch (JsonException ex)
            {
                // Don't fail the webhook — the delta sweep will still catch the message. Log and move on.
                _logger.LogWarning(ex, "Could not parse mailbox notification payload.");
            }
        }

        // Always acknowledge fast so Graph keeps the subscription healthy.
        return new AcceptedResult();
    }

    private static string? ExtractMessageId(JsonElement notification)
    {
        if (notification.TryGetProperty("resourceData", out var data)
            && data.ValueKind == JsonValueKind.Object
            && data.TryGetProperty("id", out var id))
        {
            return id.GetString();
        }
        return null;
    }
}
