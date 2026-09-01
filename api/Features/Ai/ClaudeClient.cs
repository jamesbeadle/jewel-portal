
namespace Jewel.JPMS.Api.Features.Ai;

/// <summary>
/// Minimal client for the Anthropic Messages API. Sends a system + single user turn and returns the
/// assistant's text content (which callers prompt to be JSON). Returns null on any failure so callers
/// degrade gracefully rather than surfacing an error to the triager.
/// </summary>
/// <summary>One bounded chunk of a long completion. <see cref="IsComplete"/> is true when the
/// model reached its natural end (stop_reason "end_turn"); false means it hit the chunk's token
/// budget and a follow-up call with the accumulated text as assistant prefill will continue
/// from exactly where it stopped.</summary>
public sealed record ClaudeChunk(string Text, bool IsComplete);

public interface IClaudeClient
{
    bool IsConfigured { get; }

    /// <summary>Run one completion; returns the assistant text, or null if unconfigured/failed.
    /// <paramref name="modelOverride"/> / <paramref name="maxTokensOverride"/> let a caller run
    /// this call on a specific model id (e.g. one resolved from the chat picker's tier key via
    /// <see cref="AnthropicOptions.ModelForTier"/>) with a bigger response ceiling than the
    /// default extraction budget; null keeps the configured defaults, so existing callers are
    /// untouched.</summary>
    Task<string?> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct,
        string? modelOverride = null, int? maxTokensOverride = null);

    /// <summary>One bounded chunk of a long answer, for callers whose HTTP request must stay
    /// under the Static Web Apps gateway's ~45s ceiling: a small <paramref name="maxTokens"/>
    /// per call, a hard 35s per-call timeout (a slow call fails fast into the caller's degrade
    /// path instead of taking the gateway 500), and <paramref name="assistantPrefill"/> carrying
    /// everything produced so far so the model continues mid-answer rather than starting over.
    /// Returns null if unconfigured/failed/timed out.</summary>
    Task<ClaudeChunk?> CompleteChunkAsync(string systemPrompt, string userPrompt,
        string assistantPrefill, string model, int maxTokens, CancellationToken ct);
}

/// <summary>No-op used when no Anthropic key is configured; always returns null.</summary>
public sealed class NullClaudeClient : IClaudeClient
{
    public bool IsConfigured => false;
    public Task<string?> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct,
        string? modelOverride = null, int? maxTokensOverride = null) =>
        Task.FromResult<string?>(null);
    public Task<ClaudeChunk?> CompleteChunkAsync(string systemPrompt, string userPrompt,
        string assistantPrefill, string model, int maxTokens, CancellationToken ct) =>
        Task.FromResult<ClaudeChunk?>(null);
}

/// <summary>REST implementation (HttpClient + x-api-key header), matching the app's hand-rolled style.</summary>
public sealed class ClaudeClient : IClaudeClient
{
    private const string MessagesUrl = "https://api.anthropic.com/v1/messages";

    private readonly HttpClient _http;
    private readonly AnthropicOptions _options;
    private readonly ILogger<ClaudeClient> _logger;

    public ClaudeClient(HttpClient http, AnthropicOptions options, ILogger<ClaudeClient> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    public bool IsConfigured => _options.IsConfigured;

    public async Task<string?> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct,
        string? modelOverride = null, int? maxTokensOverride = null)
    {
        if (!_options.IsConfigured)
            return null;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, MessagesUrl)
            {
                Content = JsonContent.Create(new
                {
                    model = string.IsNullOrWhiteSpace(modelOverride) ? _options.Model : modelOverride,
                    max_tokens = maxTokensOverride is > 0 ? maxTokensOverride.Value : _options.MaxTokens,
                    system = systemPrompt,
                    messages = new[]
                    {
                        new { role = "user", content = userPrompt }
                    }
                })
            };
            // Anthropic auth + version headers. The key is a secret supplied via app settings only.
            request.Headers.Add("x-api-key", _options.ApiKey);
            request.Headers.Add("anthropic-version", _options.ApiVersion);

            // A hard 35s deadline, exactly like CompleteChunkAsync: this call runs under the
            // Static Web Apps ~45s gateway, and the HttpClient's own default is 100s, so a slow
            // Anthropic response would take the whole request past the gateway and hand the user a
            // raw 500 — defeating the "degrade to manual, never error" contract every caller
            // documents. Expiring here returns null into that degrade path instead.
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(35));
            ct = timeout.Token;

            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Anthropic call failed: {Status}.", (int)response.StatusCode);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            // Response shape: { content: [ { type: "text", text: "..." }, ... ], ... }
            if (!doc.RootElement.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
                return null;

            foreach (var block in content.EnumerateArray())
            {
                var type = block.TryGetProperty("type", out var t) ? t.GetString() : null;
                if (type == "text" && block.TryGetProperty("text", out var textEl))
                    return textEl.GetString();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Anthropic call errored.");
            return null;
        }
    }

    public async Task<ClaudeChunk?> CompleteChunkAsync(string systemPrompt, string userPrompt,
        string assistantPrefill, string model, int maxTokens, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            return null;

        try
        {
            // Hard per-call ceiling well under the SWA gateway's ~45s: better to hand the caller
            // a null (their degrade path) than to let the gateway kill the whole request with a
            // raw 500 after 45 silent seconds.
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(35));

            // Continuation via assistant prefill: the last message being an assistant turn makes
            // the model carry on mid-answer instead of starting again. The API rejects prefill
            // with trailing whitespace, so it is trimmed — callers accumulate the trimmed form.
            var messages = new List<object> { new { role = "user", content = userPrompt } };
            var prefill = assistantPrefill.TrimEnd();
            if (prefill.Length > 0)
                messages.Add(new { role = "assistant", content = prefill });

            using var request = new HttpRequestMessage(HttpMethod.Post, MessagesUrl)
            {
                Content = JsonContent.Create(new
                {
                    model,
                    max_tokens = maxTokens,
                    system = systemPrompt,
                    messages
                })
            };
            request.Headers.Add("x-api-key", _options.ApiKey);
            request.Headers.Add("anthropic-version", _options.ApiVersion);

            using var response = await _http.SendAsync(request, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Anthropic chunk call failed: {Status}.", (int)response.StatusCode);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token);

            var stopReason = doc.RootElement.TryGetProperty("stop_reason", out var stop)
                ? stop.GetString() : null;

            if (!doc.RootElement.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
                return null;

            foreach (var block in content.EnumerateArray())
            {
                var type = block.TryGetProperty("type", out var t) ? t.GetString() : null;
                if (type == "text" && block.TryGetProperty("text", out var textEl))
                    return new ClaudeChunk(textEl.GetString() ?? "", stopReason == "end_turn");
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Anthropic chunk call errored.");
            return null;
        }
    }
}
