using Jewel.JPMS.Api.Features.Ai;

namespace Jewel.JPMS.Api.Features.Sales.Research;

/// <summary>
/// What the research produced: the definition fields Claude proposes (each used only where the
/// team left the field blank), and the findings — markdown with sources — that back them.
/// </summary>
public sealed record StrategyResearchResult(
    string TargetArea,
    string Hypothesis,
    string Evidence,
    string Proposition,
    string Findings);

/// <summary>
/// The research call: one Anthropic Messages request with the web_search server tool, a generous
/// search budget (this runs in the worker, so the SWA gateway's ceiling does not apply), and a
/// strict-JSON answer. Reads the brief plus whatever the team has already written; finds the
/// concrete areas, the data and the angle; never invents figures — every number carries its
/// source URL in the findings. Throws on failure so the runner can stamp the row Failed with the
/// reason.
/// </summary>
public sealed class StrategyResearcher
{
    private const string MessagesUrl = "https://api.anthropic.com/v1/messages";
    private const int MaxTokens = 8000;
    private const int MaxSearches = 12;

    private readonly HttpClient http;
    private readonly AnthropicOptions options;
    private readonly ILogger<StrategyResearcher> logger;

    public StrategyResearcher(HttpClient http, AnthropicOptions options, ILogger<StrategyResearcher> logger)
    {
        this.http = http;
        this.options = options;
        this.logger = logger;
    }

    public bool IsConfigured => options.IsConfigured;

    public async Task<StrategyResearchResult> ResearchAsync(SalesStrategy strategy, CancellationToken ct)
    {
        if (!options.IsConfigured)
            throw new InvalidOperationException("No Anthropic API key is configured on the worker (Anthropic__ApiKey).");

        using var request = new HttpRequestMessage(HttpMethod.Post, MessagesUrl)
        {
            Content = JsonContent.Create(new
            {
                model = options.ModelForTier("sonnet"),
                max_tokens = MaxTokens,
                system = SystemPrompt,
                messages = new[] { new { role = "user", content = UserPrompt(strategy) } },
                tools = new object[]
                {
                    new
                    {
                        type = "web_search_20250305",
                        name = "web_search",
                        max_uses = MaxSearches,
                        user_location = new { type = "approximate", country = "GB", region = "Surrey" }
                    }
                }
            })
        };
        request.Headers.Add("x-api-key", options.ApiKey);
        request.Headers.Add("anthropic-version", options.ApiVersion);

        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await SafeBodyAsync(response, ct);
            logger.LogWarning("Strategy research call failed: {Status}. {Detail}", (int)response.StatusCode, detail);
            throw new InvalidOperationException($"Claude answered {(int)response.StatusCode} to the research call. {Trim(detail, 300)}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var answer = FinalTextBlock(doc.RootElement)
            ?? throw new InvalidOperationException("Claude returned no written answer from the research call.");
        return ParseAnswer(answer);
    }

    private const string SystemPrompt =
        "You are the prospecting researcher for Jewel Bespoke Build, Surrey's super-prime residential "
        + "builder: bespoke new homes and substantial upgrades (extensions, refurbishments, whole-house "
        + "remodels) for private clients, usually commissioned through an architect. The team writes a "
        + "sales strategy as a brief — an idea for finding leads, in their own words — and you do the "
        + "research that turns it into a concrete, evidenced strategy. Use web search, several times, "
        + "from different angles: for an area-based idea, find the specific towns and postcode districts "
        + "(Surrey and its borders unless the brief says otherwise), house-price movement (Land Registry "
        + "/ ONS / Rightmove / Zoopla figures), planning applications and local plans, infrastructure "
        + "and transport announcements, schools and anything else that moves prices; for an architect-"
        + "or partner-based idea, find the actual practices, what they build, and what they complain "
        + "about; for a developer or landowner idea, the sites and the people. NEVER invent a figure — "
        + "every number, date and claim in your findings carries its source URL in brackets right after "
        + "it, and anything you could not verify is labelled as unverified. British English. Answer "
        + "with STRICT JSON only — no prose before or after, no markdown fences:\n"
        + "{\"targetArea\":\"one line: the specific towns / postcode districts (or practices / areas) the "
        + "strategy should target\","
        + "\"hypothesis\":\"2-5 sentences: why these people, why now — the argument, resting on the "
        + "evidence\","
        + "\"evidence\":\"the data and findings behind it, one item per line, each with its source URL\","
        + "\"proposition\":\"1-2 sentences: what Jewel says to them\","
        + "\"findings\":\"markdown, under 1200 words: ## headed sections — the areas / people found and "
        + "why, the price and market evidence, planning and infrastructure signals, who to reach and "
        + "how to find them (data sources, lists), risks and what would show the idea is wrong — every "
        + "fact with its source URL; end with a ## Sources list\"}\n"
        + "Where the team has already written a field, respect it: build on it rather than contradict "
        + "it, but still propose your version of it in the JSON (they choose).";

    private static string UserPrompt(SalesStrategy strategy)
    {
        string Or(string value, string blank) => string.IsNullOrWhiteSpace(value) ? blank : value;
        return string.Join("\n", new[]
        {
            $"Strategy name: {strategy.Name}",
            $"Audience: {strategy.Audience.DisplayName()}",
            $"Channel: {strategy.Channel.DisplayName()}",
            "",
            "THE BRIEF (the team's idea, in their words):",
            Or(strategy.Brief, "(no brief written — work from the name, audience and channel)"),
            "",
            $"Where (as written so far): {Or(strategy.TargetArea, "(blank — find the specific areas)")}",
            $"Hypothesis (as written so far): {Or(strategy.Hypothesis, "(blank — write it from the evidence)")}",
            $"Evidence (as written so far): {Or(strategy.Evidence, "(blank — go and find it)")}",
            $"Proposition (as written so far): {Or(strategy.Proposition, "(blank — draft it)")}",
            "",
            "Research this strategy now and answer with the JSON."
        });
    }

    private static string? FinalTextBlock(JsonElement root)
    {
        if (!root.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            return null;
        string? last = null;
        foreach (var block in content.EnumerateArray())
        {
            var type = block.TryGetProperty("type", out var t) ? t.GetString() : null;
            if (type == "text" && block.TryGetProperty("text", out var textEl))
                last = textEl.GetString();
        }
        return last;
    }

    private static StrategyResearchResult ParseAnswer(string answer)
    {
        var json = answer.Trim();
        var start = json.IndexOf('{');
        var end = json.LastIndexOf('}');
        if (start < 0 || end <= start)
            throw new InvalidOperationException("Claude's research answer was not the JSON asked for. It began: " + Trim(answer, 200));
        using var doc = JsonDocument.Parse(json[start..(end + 1)]);
        var root = doc.RootElement;
        string Field(string name) =>
            root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString()!.Trim() : "";
        var findings = Field("findings");
        if (string.IsNullOrWhiteSpace(findings))
            throw new InvalidOperationException("Claude's research answer had no findings.");
        return new StrategyResearchResult(
            Cap(Field("targetArea"), 512),
            Cap(Field("hypothesis"), 4000),
            Cap(Field("evidence"), 4000),
            Cap(Field("proposition"), 1024),
            findings);
    }

    private static string Cap(string value, int max) => value.Length <= max ? value : value[..max];
    private static string Trim(string value, int max) => value.Length <= max ? value : value[..max] + "…";

    private static async Task<string> SafeBodyAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try { return await response.Content.ReadAsStringAsync(ct); }
        catch { return ""; }
    }
}
