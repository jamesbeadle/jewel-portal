using Jewel.JPMS.Api.Features.Ai;

namespace Jewel.JPMS.Api.Features.Places;

/// <summary>
/// Finds local companies' websites for a trade + location via web search. Returns null on failure —
/// callers surface a friendly message rather than an exception. Aggregator/directory sites
/// (Checkatrade, Yell, Facebook…) are filtered out and hits are deduplicated by domain, so what
/// remains are the companies' own websites — which is where contact details are then discovered.
/// </summary>
public interface ILocalBusinessSearch
{
    bool IsConfigured { get; }

    /// <summary>Search for company websites, skipping any domain in <paramref name="excludeDomains"/>
    /// (domains already shown on earlier pages — this is how "Load more" avoids repeats).</summary>
    Task<BusinessSearchPage?> SearchAsync(
        string trade, string location, IReadOnlyCollection<string> excludeDomains, CancellationToken ct);
}

/// <summary>One page of company-website hits. HasMore drives the UI's "Load more".</summary>
public sealed record BusinessSearchPage(IReadOnlyList<BusinessHit> Hits, bool HasMore);

/// <summary>A company website found by the search.</summary>
public sealed record BusinessHit(string Url, string Domain, string Title, string Description);

/// <summary>No-op fallback when no API key is configured — the app runs, the search explains itself.</summary>
public sealed class NullLocalBusinessSearch : ILocalBusinessSearch
{
    public bool IsConfigured => false;

    public Task<BusinessSearchPage?> SearchAsync(
        string trade, string location, IReadOnlyCollection<string> excludeDomains, CancellationToken ct) =>
        Task.FromResult<BusinessSearchPage?>(null);
}

/// <summary>
/// Claude-backed implementation: one Messages API call with the web_search server tool. Claude runs
/// the searches, discards directories/aggregators, and answers with a strict JSON list of the
/// companies' own websites. The aggregator filter and domain dedupe are re-applied here as a
/// belt-and-braces pass — a model's promise is not a guarantee.
/// </summary>
public sealed class ClaudeLocalBusinessSearch : ILocalBusinessSearch
{
    private const string MessagesUrl = "https://api.anthropic.com/v1/messages";

    /// <summary>Companies asked for per page. More than typically survive contact discovery —
    /// email-less companies are filtered out after the search.</summary>
    public const int PageSize = 12;

    // Ceiling for the search turn. Deliberately independent of AnthropicOptions.MaxTokens, which is
    // sized for small field-extraction answers; a page of companies as JSON needs more room.
    private const int MaxTokens = 4096;

    // Web searches Claude may run inside the one call — enough to cover a trade + location from a
    // couple of angles without letting latency run away (the SWA gateway allows ~45s in total).
    private const int MaxSearches = 3;

    // Directories, marketplaces and socials — never the company's own site, so never a hit.
    private static readonly string[] AggregatorDomains =
    {
        "checkatrade.com", "yell.com", "yelp.com", "yelp.co.uk", "trustpilot.com", "bark.com",
        "mybuilder.com", "ratedpeople.com", "trustatrader.com", "houzz.com", "houzz.co.uk",
        "threebestrated.co.uk", "facebook.com", "instagram.com", "linkedin.com", "x.com",
        "twitter.com", "youtube.com", "tiktok.com", "pinterest.com", "wikipedia.org",
        "google.com", "gumtree.com", "nextdoor.co.uk", "which.co.uk", "reddit.com",
        "indeed.com", "glassdoor.co.uk", "companieshouse.gov.uk", "find-and-update.company-information.service.gov.uk"
    };

    private readonly HttpClient _http;
    private readonly AnthropicOptions _options;
    private readonly ILogger<ClaudeLocalBusinessSearch> _logger;

    public ClaudeLocalBusinessSearch(HttpClient http, AnthropicOptions options, ILogger<ClaudeLocalBusinessSearch> logger)
    {
        _http = http; _options = options; _logger = logger;
    }

    public bool IsConfigured => _options.IsConfigured;

    public async Task<BusinessSearchPage?> SearchAsync(
        string trade, string location, IReadOnlyCollection<string> excludeDomains, CancellationToken ct)
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
                    max_tokens = MaxTokens,
                    system = SystemPrompt,
                    messages = new[]
                    {
                        new { role = "user", content = UserPrompt(trade, location, excludeDomains) }
                    },
                    tools = new object[]
                    {
                        new
                        {
                            type = "web_search_20250305",
                            name = "web_search",
                            max_uses = MaxSearches,
                            user_location = new { type = "approximate", country = "GB" }
                        }
                    }
                })
            };
            // Anthropic auth + version headers. The key is a secret supplied via app settings only.
            request.Headers.Add("x-api-key", _options.ApiKey);
            request.Headers.Add("anthropic-version", _options.ApiVersion);

            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Claude local search failed: {Status}. {Detail}",
                    (int)response.StatusCode, await SafeBodyAsync(response, ct));
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var answer = FinalTextBlock(doc.RootElement);
            if (answer is null)
            {
                _logger.LogWarning("Claude local search returned no text answer.");
                return null;
            }

            return ParseAnswer(answer, excludeDomains);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Claude local search errored.");
            return null;
        }
    }

    private const string SystemPrompt =
        "You find local trade companies for a UK construction firm inviting bids. Use web search, " +
        "then answer with STRICT JSON only — no prose, no markdown fences:\n" +
        "{\"companies\":[{\"url\":\"https://…\",\"name\":\"Company Name\",\"description\":\"one line on what they do and where\"}],\"moreAvailable\":true}\n" +
        "Rules: only companies' OWN websites — never directories, aggregators, marketplaces or social " +
        "media (Checkatrade, Yell, Trustpilot, MyBuilder, Rated People, Houzz, Facebook, LinkedIn and " +
        "the like); one entry per company; url is the site's homepage; genuinely local firms that " +
        "plausibly serve the given location; skip every excluded domain you are given. Set " +
        "moreAvailable to true only if you are confident further distinct local companies exist " +
        "beyond those listed.";

    private static string UserPrompt(string trade, string location, IReadOnlyCollection<string> excludeDomains)
    {
        var exclusions = excludeDomains.Count == 0
            ? ""
            : $"\nAlready shown — exclude these domains: {string.Join(", ", excludeDomains)}";
        return $"Find up to {PageSize} {trade} companies in or near {location}, UK.{exclusions}";
    }

    // The answer is the last text block — earlier blocks are search commentary interleaved with the
    // server tool calls.
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

    private BusinessSearchPage? ParseAnswer(string answer, IReadOnlyCollection<string> excludeDomains)
    {
        // Tolerate a model that fenced the JSON despite instructions.
        var json = answer.Trim();
        var start = json.IndexOf('{');
        var end = json.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            _logger.LogWarning("Claude local search answer was not JSON.");
            return null;
        }
        json = json[start..(end + 1)];

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var moreAvailable = root.TryGetProperty("moreAvailable", out var more)
            && more.ValueKind == JsonValueKind.True;

        var hits = new List<BusinessHit>();
        var seenDomains = new HashSet<string>(excludeDomains, StringComparer.OrdinalIgnoreCase);

        if (root.TryGetProperty("companies", out var companies) && companies.ValueKind == JsonValueKind.Array)
        {
            foreach (var company in companies.EnumerateArray())
            {
                var link = GetString(company, "url");
                if (string.IsNullOrEmpty(link) || !Uri.TryCreate(link, UriKind.Absolute, out var uri)) continue;

                var domain = uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.Host[4..] : uri.Host;
                if (!seenDomains.Add(domain)) continue;
                if (AggregatorDomains.Any(agg =>
                        domain.Equals(agg, StringComparison.OrdinalIgnoreCase)
                        || domain.EndsWith("." + agg, StringComparison.OrdinalIgnoreCase)))
                    continue;

                hits.Add(new BusinessHit(
                    Url: $"{uri.Scheme}://{uri.Host}",
                    Domain: domain,
                    Title: GetString(company, "name") ?? domain,
                    Description: GetString(company, "description") ?? ""));
            }
        }

        return new BusinessSearchPage(hits, moreAvailable);
    }

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static async Task<string> SafeBodyAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try { return await response.Content.ReadAsStringAsync(ct); }
        catch { return "(unreadable body)"; }
    }
}
