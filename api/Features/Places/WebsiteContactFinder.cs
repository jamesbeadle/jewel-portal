using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Places;

/// <summary>
/// Best-effort contact discovery for a company website: fetch the homepage (then /contact as a
/// fallback), prefer mailto:/tel: links, otherwise take the most plausible email and UK phone
/// number in the HTML. Web search results carry no contact details, so this is what turns a hit
/// into someone we can actually invite. Email null when nothing plausible is found — callers
/// exclude those companies.
/// </summary>
public interface IWebsiteContactFinder
{
    Task<WebsiteContact> FindAsync(string websiteUrl, CancellationToken ct);
}

/// <summary>What was found on the site — any field may be null. Name comes from the site's own
/// og:site_name or &lt;title&gt;, so persisted company details originate from the company itself
/// rather than from search-result content (which must stay transient).</summary>
public sealed record WebsiteContact(string? Email, string? Phone, string? Name = null)
{
    public static readonly WebsiteContact None = new(null, null);
}

public sealed class WebsiteContactFinder : IWebsiteContactFinder
{
    private static readonly Regex EmailPattern = new(
        @"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}",
        RegexOptions.Compiled);

    private static readonly Regex MailtoPattern = new(
        @"mailto:([A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex TelPattern = new(
        @"tel:\s*([+\d][\d\s().\-]{7,18}\d)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // UK-shaped numbers in page text: +44… or 0xxxx…, tolerant of spaces/dashes/brackets.
    private static readonly Regex UkPhonePattern = new(
        @"(?:\+44\s?\(?0?\)?\s?\d{2,4}|\(?0\d{2,4}\)?)[\s.\-]?\d{3,4}[\s.\-]?\d{3,4}",
        RegexOptions.Compiled);

    private static readonly Regex SiteNamePattern = new(
        @"property\s*=\s*[""']og:site_name[""']\s+content\s*=\s*[""']([^""']+)[""']|content\s*=\s*[""']([^""']+)[""']\s+property\s*=\s*[""']og:site_name[""']",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex TitleTagPattern = new(
        @"<title[^>]*>\s*([^<]+?)\s*</title>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Obvious non-contact matches: asset filenames, tracking/CMS domains, placeholder addresses.
    private static readonly string[] JunkFragments =
    {
        ".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp", ".css", ".js",
        "example.", "sentry", "wixpress", "@2x", "no-reply", "noreply", "your@", "email@", "name@",
        "domain.", "@email", "godaddy", "cloudflare"
    };

    private readonly HttpClient _http;
    private readonly ILogger<WebsiteContactFinder> _logger;

    public WebsiteContactFinder(HttpClient http, ILogger<WebsiteContactFinder> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<WebsiteContact> FindAsync(string websiteUrl, CancellationToken ct)
    {
        if (!Uri.TryCreate(websiteUrl, UriKind.Absolute, out var uri)) return WebsiteContact.None;

        var contact = await FindOnPageAsync(uri, ct);
        if (contact.Email is not null) return contact;

        // Many trade sites keep contact details on a contact page only.
        foreach (var path in new[] { "contact", "contact-us" })
        {
            if (!Uri.TryCreate(uri, path, out var contactUri)) continue;
            var fromContactPage = await FindOnPageAsync(contactUri, ct);
            if (fromContactPage.Email is not null)
                return fromContactPage with
                {
                    Phone = fromContactPage.Phone ?? contact.Phone,
                    Name = contact.Name ?? fromContactPage.Name // homepage names the business best
                };
        }
        return contact;
    }

    private async Task<WebsiteContact> FindOnPageAsync(Uri uri, CancellationToken ct)
    {
        try
        {
            // Per-page timeout so one slow site can't stall the whole search page.
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));

            using var response = await _http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode) return WebsiteContact.None;

            var html = await response.Content.ReadAsStringAsync(timeout.Token);
            if (html.Length == 0) return WebsiteContact.None;

            return new WebsiteContact(FindEmail(html), FindPhone(html), FindName(html));
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            _logger.LogDebug("Contact discovery failed for {Uri}: {Message}", uri, ex.Message);
            return WebsiteContact.None;
        }
    }

    private static string? FindEmail(string html)
    {
        // mailto: links are deliberate contact addresses — trust them first.
        foreach (Match match in MailtoPattern.Matches(html))
        {
            var candidate = CleanEmail(match.Groups[1].Value);
            if (candidate is not null) return candidate;
        }

        // Otherwise the most plausible raw address, preferring generic contact mailboxes.
        var candidates = EmailPattern.Matches(html)
            .Select(m => CleanEmail(m.Value))
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

    // og:site_name is the site naming itself; otherwise the first meaningful <title> segment
    // ("SilvaTree Landscaping | Garden Design Bromley" → "SilvaTree Landscaping").
    private static string? FindName(string html)
    {
        var og = SiteNamePattern.Match(html);
        if (og.Success)
        {
            var name = System.Net.WebUtility.HtmlDecode((og.Groups[1].Value + og.Groups[2].Value).Trim());
            if (name.Length >= 3) return name;
        }

        var title = TitleTagPattern.Match(html);
        if (!title.Success) return null;
        var segments = System.Net.WebUtility.HtmlDecode(title.Groups[1].Value)
            .Split(new[] { '|', '-', '–', '—', '·' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            if (segment.Length < 3) continue;
            if (segment.Equals("home", StringComparison.OrdinalIgnoreCase)) continue;
            if (segment.Equals("welcome", StringComparison.OrdinalIgnoreCase)) continue;
            return segment;
        }
        return null;
    }

    private static string? FindPhone(string html)
    {
        // tel: links are deliberate; fall back to a UK-shaped number in the text.
        var tel = TelPattern.Match(html);
        var raw = tel.Success ? tel.Groups[1].Value : UkPhonePattern.Match(html).Value;
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var cleaned = Regex.Replace(raw, @"[^\d+]", " ").Trim();
        cleaned = Regex.Replace(cleaned, @"\s+", " ");
        var digits = cleaned.Count(char.IsDigit);
        return digits is >= 10 and <= 13 ? cleaned : null;
    }

    private static string? CleanEmail(string raw)
    {
        var email = raw.Trim().TrimEnd('.').ToLowerInvariant();
        if (email.Length is < 6 or > 128) return null;
        if (JunkFragments.Any(junk => email.Contains(junk, StringComparison.OrdinalIgnoreCase))) return null;
        return email;
    }
}
