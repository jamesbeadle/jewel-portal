using System.Net.Http.Json;
using System.Text.Json;
using Jewel.JPMS.Contracts.Ai;
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
    int OutputTokens = 0,
    /// <summary>Set when the call ran on a bigger tier than asked for because the request had
    /// outgrown the chosen model's context window. One human sentence, surfaced by the panel.</summary>
    string? EscalationNote = null);

/// <summary>
/// Multi-turn client with tool support. Separate from <see cref="IClaudeClient"/>, which keeps its
/// single-shot shape for callers that need one prompt → one answer with no tools.
/// </summary>
public interface IClaudeConversationClient
{
    bool IsConfigured { get; }

    /// <summary>
    /// One round trip. <paramref name="messages"/> is the Anthropic messages array, already shaped
    /// by the caller (see <c>SendAiMessageHandler</c>). <paramref name="modelTier"/> is an
    /// AiModelCatalogue key — resolved to a real model id here, against config, so the client of
    /// this client can never name a raw model.
    /// </summary>
    Task<ClaudeReply> ContinueAsync(
        string systemPrompt,
        IReadOnlyList<object> messages,
        IReadOnlyList<ClaudeToolSpec> tools,
        string? modelTier,
        CancellationToken ct);
}

public sealed class NullClaudeConversationClient : IClaudeConversationClient
{
    public bool IsConfigured => false;

    public Task<ClaudeReply> ContinueAsync(
        string systemPrompt, IReadOnlyList<object> messages,
        IReadOnlyList<ClaudeToolSpec> tools, string? modelTier, CancellationToken ct) =>
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

    /// <summary>Rough tokens-per-character for estimation. Deliberately conservative (real English
    /// prose runs nearer 4 chars/token) so the fit check errs toward stepping up a tier rather than
    /// hitting the wall.</summary>
    private const int CharsPerToken = 3;

    /// <summary>Headroom on top of the payload estimate: the reply itself plus estimation slack.</summary>
    private const int EstimateHeadroomTokens = ConversationMaxTokens + 4_000;

    /// <summary>
    /// The tier this request will actually run on. Starts from what the user chose and STEPS UP —
    /// never down — to the cheapest tier whose context window fits the estimated request, so a
    /// conversation that has outgrown Haiku's 200k window carries on seamlessly on a bigger model
    /// instead of failing mid-turn and asking the user to switch by hand. If nothing fits (past
    /// even the 1M windows), the biggest candidate is returned and the API's own error handles it —
    /// the transcript budget should make that unreachable.
    /// </summary>
    private (string TierKey, string? Note) FitTier(string? requestedTier, int estimatedTokens)
    {
        var requested = AiModelCatalogue.Normalise(requestedTier);
        if (estimatedTokens <= options.ContextTokensForTier(requested)) return (requested, null);

        var order = AiModelCatalogue.All; // cheapest first
        var requestedIndex = 0;
        for (var i = 0; i < order.Count; i++)
            if (string.Equals(order[i].Key, requested, StringComparison.OrdinalIgnoreCase)) requestedIndex = i;

        for (var i = requestedIndex + 1; i < order.Count; i++)
        {
            if (estimatedTokens > options.ContextTokensForTier(order[i].Key)) continue;
            var from = AiModelCatalogue.Find(requested)?.DisplayName ?? requested;
            return (order[i].Key,
                $"Stepped up to {order[i].DisplayName} for this reply — the conversation has grown past what {from} can read.");
        }

        return (order[^1].Key, null);
    }

    public async Task<ClaudeReply> ContinueAsync(
        string systemPrompt,
        IReadOnlyList<object> messages,
        IReadOnlyList<ClaudeToolSpec> tools,
        string? modelTier,
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

            // ---- fit the model to the size of the request --------------------------------------
            // Serialise once without the model to measure what is actually about to be sent, then
            // pick the tier: the user's choice when it fits, the cheapest bigger one when it does
            // not. This is the guard against "Haiku started the chat and now cannot finish it" —
            // the step-up happens here, mid-conversation, without the user doing anything. (The
            // prompt cache is per model, so a step-up re-pays the prefix once — correct, and cheap
            // next to a failed turn.)
            var payloadJson = JsonSerializer.Serialize(payload);
            var estimatedTokens = payloadJson.Length / CharsPerToken + EstimateHeadroomTokens;
            var (tier, escalationNote) = FitTier(modelTier, estimatedTokens);
            if (escalationNote is not null)
            {
                logger.LogInformation(
                    "Stepped the conversation model up from {Requested} to {Used}: ~{Tokens} tokens estimated.",
                    AiModelCatalogue.Normalise(modelTier), tier, estimatedTokens);
            }

            // At most two attempts: the estimated fit, then one retry a tier up if the API still
            // says the prompt is too long — the estimate is conservative, so this path should be
            // rare, but "rare" is not "never" and a hard failure mid-conversation is the one
            // outcome this method exists to prevent.
            for (var attempt = 0; ; attempt++)
            {
                payload["model"] = options.ModelForTier(tier);

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

                    if (attempt == 0
                        && (int)response.StatusCode == 400
                        && body.Contains("too long", StringComparison.OrdinalIgnoreCase)
                        && NextBiggerTier(tier) is { } bigger)
                    {
                        var fromName = AiModelCatalogue.Find(tier)?.DisplayName ?? tier;
                        tier = bigger;
                        escalationNote = $"Stepped up to {AiModelCatalogue.Find(bigger)?.DisplayName ?? bigger} for this "
                                         + $"reply — the conversation has grown past what {fromName} can read.";
                        continue;
                    }

                    return new ClaudeReply(false, null, Array.Empty<ClaudeToolCall>(), null,
                        $"upstream_{(int)response.StatusCode}");
                }

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                return Parse(document.RootElement, escalationNote);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Anthropic conversation call errored.");
            return new ClaudeReply(false, null, Array.Empty<ClaudeToolCall>(), null, "exception");
        }
    }

    /// <summary>The next tier up with a genuinely bigger window, or null from the top.</summary>
    private string? NextBiggerTier(string currentTier)
    {
        var order = AiModelCatalogue.All;
        var currentWindow = options.ContextTokensForTier(currentTier);
        var index = 0;
        for (var i = 0; i < order.Count; i++)
            if (string.Equals(order[i].Key, currentTier, StringComparison.OrdinalIgnoreCase)) index = i;
        for (var i = index + 1; i < order.Count; i++)
            if (options.ContextTokensForTier(order[i].Key) > currentWindow) return order[i].Key;
        return null;
    }

    /// <summary>One successful Messages-API response into a typed reply.</summary>
    private static ClaudeReply Parse(JsonElement root, string? escalationNote)
    {
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

            return new ClaudeReply(true, text, toolCalls, stopReason, null, inputTokens, outputTokens,
                escalationNote);
    }
}
