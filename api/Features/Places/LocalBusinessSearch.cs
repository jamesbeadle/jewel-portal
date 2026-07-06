using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Places;

/// <summary>
/// Finds local companies' websites for a trade + location via web search. Returns null on failure —
/// callers surface a friendly message rather than an exception. Aggregator/directory sites
/// (Checkatrade, Yell, Facebook…) are filtered out and hits are deduplicated by domain, so what
/// remains are the companies' own websites — which is where contact details are then discovered.
/// </summary>
public interface ILocalBusinessSearch
{
    Task<BusinessSearchPage?> SearchAsync(string trade, string location, int page, CancellationToken ct);
}

/// <summary>One page of company-website hits. HasMore drives the UI's "Load more".</summary>
public sealed record BusinessSearchPage(IReadOnlyList<BusinessHit> Hits, bool HasMore);

/// <summary>A company website found by the search.</summary>
public sealed record BusinessHit(string Url, string Domain, string Title, string Description);

/// <summary>No-op fallback when no API key is configured — the app runs, the search explains itself.</summary>
public sealed class NullLocalBusinessSearch : ILocalBusinessSearch
{
    public Task<BusinessSearchPage?> SearchAsync(string trade, string location, int page, CancellationToken ct) =>
        Task.FromResult<BusinessSearchPage?>(null);
}

/// <summary>Brave Search API implementation (HttpClient + X-Subscription-Token header).</summary>
public sealed class BraveLocalBusinessSearch : ILocalBusinessSearch
{
    private const string SearchUrl = "https://api.search.brave.com/res/v1/web/search";

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
    private readonly BraveSearchOptions _options;
    private readonly ILogger<BraveLocalBusinessSearch> _logger;

    public BraveLocalBusinessSearch(HttpClient http, BraveSearchOptions options, ILogger<BraveLocalBusinessSearch> logger)
    {
        _http = http; _options = options; _logger = logger;
    }

    public async Task<BusinessSearchPage?> SearchAsync(string trade, string location, int page, CancellationToken ct)
    {
        var query = Uri.EscapeDataString($"{trade} companies in {location}");
        var url = $"{SearchUrl}?q={query}&count={_options.PageSize}&offset={page}&country={_options.Country}&safesearch=moderate";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("X-Subscription-Token", _options.ApiKey);

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Brave search failed: {Status}. {Detail}",
                (int)response.StatusCode, await SafeBodyAsync(response, ct));
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        var hits = new List<BusinessHit>();
        var seenDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var moreAvailable = root.TryGetProperty("query", out var q)
            && q.TryGetProperty("more_results_available", out var more)
            && more.ValueKind == JsonValueKind.True;

        if (root.TryGetProperty("web", out var web)
            && web.TryGetProperty("results", out var results)
            && results.ValueKind == JsonValueKind.Array)
        {
            foreach (var result in results.EnumerateArray())
            {
                var link = GetString(result, "url");
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
                    Title: StripHtml(GetString(result, "title") ?? domain),
                    Description: StripHtml(GetString(result, "description") ?? "")));
            }
        }

        return new BusinessSearchPage(hits, moreAvailable);
    }

    // Brave titles/descriptions carry emphasis markup (<strong>…) and entities — plain text only.
    private static string StripHtml(string value) =>
        System.Net.WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(value, "<[^>]+>", "")).Trim();

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
