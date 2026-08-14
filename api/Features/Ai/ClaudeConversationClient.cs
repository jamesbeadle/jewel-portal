using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Ai;

/// <summary>A tool as Anthropic expects it. <see cref="InputSchema"/> is a JSON Schema object.</summary>
public sealed record ClaudeToolSpec(string Name, string Description, object InputSchema);

public sealed record ClaudeToolCall(string Id, string Name, string ArgumentsJson);

public sealed record ClaudeReply(
    bool Ok,
    string? Text,
    IReadOnlyList<ClaudeToolCall> ToolCalls,
    string? StopReason,
    string? Error,
    /// <summary>Reported by Anthropic per call. Summed across a turn's steps for the activity log.</summary>
    int InputTokens = 0,
    int OutputTokens = 0);

/// <summary>
/// Multi-turn client with tool support. Separate from <see cref="IClaudeClient"/>, which keeps its
/// single-shot shape for callers that need one prompt → one answer with no tools.
/// </summary>
public interface IClaudeConversationClient
{
    bool IsConfigured { get; }

    /// <summary>
    /// One round trip. <paramref name="messages"/> is the Anthropic messages array, already shaped
    /// by the caller (see <c>SendAiMessageHandler</c>).
    /// </summary>
    Task<ClaudeReply> ContinueAsync(
        string systemPrompt,
        IReadOnlyList<object> messages,
        IReadOnlyList<ClaudeToolSpec> tools,
        CancellationToken ct);
}

public sealed class NullClaudeConversationClient : IClaudeConversationClient
{
    public bool IsConfigured => false;

    public Task<ClaudeReply> ContinueAsync(
        string systemPrompt, IReadOnlyList<object> messages,
        IReadOnlyList<ClaudeToolSpec> tools, CancellationToken ct) =>
        Task.FromResult(new ClaudeReply(false, null, Array.Empty<ClaudeToolCall>(), null, "not_configured"));
}

public sealed class ClaudeConversationClient : IClaudeConversationClient
{
    private const string MessagesUrl = "https://api.anthropic.com/v1/messages";

    // Deliberately larger than AnthropicOptions.MaxTokens (1024, sized for field extraction). A
    // conversational reply that also has to carry a drafted email needs room.
    private const int ConversationMaxTokens = 4096;

    private readonly HttpClient http;
    private readonly AnthropicOptions options;
    private readonly ILogger<ClaudeConversationClient> logger;

    public ClaudeConversationClient(
        HttpClient http, AnthropicOptions options, ILogger<ClaudeConversationClient> logger)
    {
        this.http = http;
        this.options = options;
        this.logger = logger;
    }

    public bool IsConfigured => options.IsConfigured;

    public async Task<ClaudeReply> ContinueAsync(
        string systemPrompt,
        IReadOnlyList<object> messages,
        IReadOnlyList<ClaudeToolSpec> tools,
        CancellationToken ct)
    {
        if (!options.IsConfigured)
            return new ClaudeReply(false, null, Array.Empty<ClaudeToolCall>(), null, "not_configured");

        try
        {
            // Prompt caching (docs/ai — turn feel): the tool catalogue and the system prompt are
            // stable across the hops of a turn and the turns of a conversation, so each carries a
            // cache_control breakpoint. Cached prefix tokens cost ~10% and, more importantly here,
            // skip re-processing — which is seconds off every hop. The transcript's own breakpoint
            // is set by AiTurnRunner on the newest persisted block, so the prefix grows
            // incrementally instead of being re-paid whole. Order matters to Anthropic's prefix
            // match: tools, then system, then messages — volatile content therefore rides on the
            // END of the messages array (the turn-context block), never in system.
            var payload = new Dictionary<string, object?>
            {
                ["model"] = options.Model,
                ["max_tokens"] = ConversationMaxTokens,
                ["system"] = new object[]
                {
                    new Dictionary<string, object?>
                    {
                        ["type"] = "text",
                        ["text"] = systemPrompt,
                        ["cache_control"] = new { type = "ephemeral" }
                    }
                },
                ["messages"] = messages
            };

            if (tools.Count > 0)
            {
                payload["tools"] = tools
                    .Select((tool, index) =>
                    {
                        var spec = new Dictionary<string, object?>
                        {
                            ["name"] = tool.Name,
                            ["description"] = tool.Description,
                            ["input_schema"] = tool.InputSchema
                        };
                        // The breakpoint goes on the LAST tool: it caches the whole catalogue.
                        if (index == tools.Count - 1)
                            spec["cache_control"] = new { type = "ephemeral" };
                        return spec;
                    })
                    .ToArray();
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, MessagesUrl)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("x-api-key", options.ApiKey);
            request.Headers.Add("anthropic-version", options.ApiVersion);

            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Anthropic conversation call failed: {Status} {Body}.", (int)response.StatusCode, body);
                return new ClaudeReply(false, null, Array.Empty<ClaudeToolCall>(), null,
                    $"upstream_{(int)response.StatusCode}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = document.RootElement;

            var stopReason = root.TryGetProperty("stop_reason", out var stopElement)
                ? stopElement.GetString()
                : null;

            // { usage: { input_tokens, output_tokens } } — the only honest source of spend.
            var inputTokens = 0;
            var outputTokens = 0;
            if (root.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object)
            {
                if (usage.TryGetProperty("input_tokens", out var inElement) && inElement.TryGetInt32(out var parsedIn))
                    inputTokens = parsedIn;
                if (usage.TryGetProperty("output_tokens", out var outElement) && outElement.TryGetInt32(out var parsedOut))
                    outputTokens = parsedOut;
            }

            string? text = null;
            var toolCalls = new List<ClaudeToolCall>();

            if (root.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
            {
                foreach (var block in content.EnumerateArray())
                {
                    var type = block.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;

                    if (type == "text" && block.TryGetProperty("text", out var textElement))
                    {
                        // Several text blocks can come back; join rather than taking the first.
                        var piece = textElement.GetString();
                        if (!string.IsNullOrWhiteSpace(piece))
                            text = string.IsNullOrEmpty(text) ? piece : $"{text}\n\n{piece}";
                    }
                    else if (type == "tool_use")
                    {
                        var id = block.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
                        var name = block.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;
                        var input = block.TryGetProperty("input", out var inputElement)
                            ? inputElement.GetRawText()
                            : "{}";
                        if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(name))
                            toolCalls.Add(new ClaudeToolCall(id, name, input));
                    }
                }
            }

            return new ClaudeReply(true, text, toolCalls, stopReason, null, inputTokens, outputTokens);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Anthropic conversation call errored.");
            return new ClaudeReply(false, null, Array.Empty<ClaudeToolCall>(), null, "exception");
        }
    }
}
