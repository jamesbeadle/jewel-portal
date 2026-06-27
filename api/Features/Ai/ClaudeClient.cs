using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Ai;

/// <summary>
/// Minimal client for the Anthropic Messages API. Sends a system + single user turn and returns the
/// assistant's text content (which callers prompt to be JSON). Returns null on any failure so callers
/// degrade gracefully rather than surfacing an error to the triager.
/// </summary>
public interface IClaudeClient
{
    bool IsConfigured { get; }

    /// <summary>Run one completion; returns the assistant text, or null if unconfigured/failed.</summary>
    Task<string?> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct);
}

/// <summary>No-op used when no Anthropic key is configured; always returns null.</summary>
public sealed class NullClaudeClient : IClaudeClient
{
    public bool IsConfigured => false;
    public Task<string?> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct) =>
        Task.FromResult<string?>(null);
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

    public async Task<string?> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            return null;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, MessagesUrl)
            {
                Content = JsonContent.Create(new
                {
                    model = _options.Model,
                    max_tokens = _options.MaxTokens,
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
}
