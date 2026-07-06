using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Places;

/// <summary>
/// Best-effort email discovery for a company website: fetch the homepage (then /contact as a
/// fallback), prefer mailto: links, otherwise take the most plausible address in the HTML.
/// Google Places never returns emails, so this is what turns a Places hit into someone we can
/// actually invite. Null when nothing plausible is found — callers exclude those companies.
/// </summary>
public interface IWebsiteEmailFinder
{
    Task<string?> FindAsync(string websiteUrl, CancellationToken ct);
}

public sealed class WebsiteEmailFinder : IWebsiteEmailFinder
{
    private static readonly Regex EmailPattern = new(
        @"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}",
        RegexOptions.Compiled);

    private static readonly Regex MailtoPattern = new(
        @"mailto:([A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Obvious non-contact matches: asset filenames, tracking/CMS domains, placeholder addresses.
    private static readonly string[] JunkFragments =
    {
        ".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp", ".css", ".js",
        "example.", "sentry", "wixpress", "@2x", "no-reply", "noreply", "your@", "email@", "name@",
        "domain.", "@email", "godaddy", "cloudflare"
    };

    private readonly HttpClient _http;
    private readonly ILogger<WebsiteEmailFinder> _logger;

    public WebsiteEmailFinder(HttpClient http, ILogger<WebsiteEmailFinder> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<string?> FindAsync(string websiteUrl, CancellationToken ct)
    {
        if (!Uri.TryCreate(websiteUrl, UriKind.Absolute, out var uri)) return null;

        var email = await FindOnPageAsync(uri, ct);
        if (email is not null) return email;

        // Many trade sites keep the address on a contact page only.
        foreach (var path in new[] { "contact", "contact-us" })
        {
            if (Uri.TryCreate(uri, path, out var contactUri))
            {
                email = await FindOnPageAsync(contactUri, ct);
                if (email is not null) return email;
            }
        }
        return null;
    }

    private async Task<string?> FindOnPageAsync(Uri uri, CancellationToken ct)
    {
        try
        {
            // Per-page timeout so one slow site can't stall the whole search page.
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));

            using var response = await _http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode) return null;

            var html = await response.Content.ReadAsStringAsync(timeout.Token);
            if (html.Length == 0) return null;

            // mailto: links are deliberate contact addresses — trust them first.
            foreach (Match match in MailtoPattern.Matches(html))
            {
                var candidate = Clean(match.Groups[1].Value);
                if (candidate is not null) return candidate;
            }

            // Otherwise the most plausible raw address, preferring generic contact mailboxes.
            var candidates = EmailPattern.Matches(html)
                .Select(m => Clean(m.Value))
                .Where(c => c is not null)
                .Select(c => c!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (candidates.Count == 0) return null;

            return candidates
                .OrderByDescending(c => c.StartsWith("info@") || c.StartsWith("contact@")
                    || c.StartsWith("hello@") || c.StartsWith("enquiries@") || c.StartsWith("office@") || c.StartsWith("sales@"))
                .First();
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            _logger.LogDebug("Email discovery failed for {Uri}: {Message}", uri, ex.Message);
            return null;
        }
    }

    private static string? Clean(string raw)
    {
        var email = raw.Trim().TrimEnd('.').ToLowerInvariant();
        if (email.Length is < 6 or > 128) return null;
        if (JunkFragments.Any(junk => email.Contains(junk, StringComparison.OrdinalIgnoreCase))) return null;
        return email;
    }
}
