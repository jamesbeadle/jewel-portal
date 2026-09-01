using Jewel.JPMS.Api.Features.Places;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

// Finds companies of a trade near the project's site: a Claude web search locates company
// websites, then each site is visited to discover a contact email and phone. Hits with no findable
// email are excluded — an invite that can't be emailed is no invite. Failures are returned as a
// readable Error on the result (not thrown) so the UI can explain: key not configured, project
// missing its address, or the search refusing the call. Hits are matched against the directory by
// company name or website so the UI can invite an existing entry instead of duplicating it.
public sealed class SearchLocalSubcontractorsHandler
    : IQueryHandler<SearchLocalSubcontractors, LocalSubcontractorSearchResult>
{
    private static readonly char[] TitleSeparators = { '|', '-', '–', '—', '·' };

    // "Load more" stops offering itself once this many domains have been shown — the token has to
    // travel back and forth as a query value, and past this point the well is dry anyway.
    private const int MaxExcludedDomains = 60;

    private readonly JpmsContext context;
    private readonly ILocalBusinessSearch search;
    private readonly IWebsiteContactFinder contactFinder;

    public SearchLocalSubcontractorsHandler(
        JpmsContext context, ILocalBusinessSearch search, IWebsiteContactFinder contactFinder)
    {
        this.context = context; this.search = search; this.contactFinder = contactFinder;
    }

    public async Task<LocalSubcontractorSearchResult> HandleAsync(SearchLocalSubcontractors query, CancellationToken cancellationToken)
    {
        static LocalSubcontractorSearchResult Fail(string message) =>
            new(Array.Empty<LocalSubcontractor>(), Error: message);

        if (!search.IsConfigured)
            return Fail("The local search isn't configured yet — add the Anthropic__ApiKey application setting.");

        if (string.IsNullOrWhiteSpace(query.Trade))
            return Fail("Choose a trade to search for.");

        var project = await context.Projects.FindAsync(new object[] { query.ProjectId }, cancellationToken);
        if (project is null)
            return Fail("Project not found.");

        var location = string.Join(", ", new[] { project.Town, project.Postcode }
            .Where(part => !string.IsNullOrWhiteSpace(part)));
        if (location.Length == 0)
            return Fail("This project has no town or postcode yet. Add the site address under Edit details on the project, then search again.");

        // PageToken carries the domains already shown, so "Load more" asks the search to find
        // different companies rather than the same page again.
        var excludeDomains = (query.PageToken ?? "")
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var found = await search.SearchAsync(query.Trade.Trim(), location, excludeDomains, cancellationToken);
        if (found is null)
            return Fail("The local search failed. Check the Anthropic API key is valid, then try again.");

        // Directory matching (by company name or website domain) so known companies aren't duplicated,
        // and their directory email is reused when present.
        var directory = await context.Subcontractors.AsNoTracking()
            .Select(sub => new { sub.SubcontractorId, sub.CompanyName, sub.ContactEmail, sub.Website })
            .ToListAsync(cancellationToken);
        var byName = directory
            .GroupBy(sub => sub.CompanyName.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var byDomain = directory
            .Where(sub => !string.IsNullOrWhiteSpace(sub.Website))
            .GroupBy(sub => DomainOf(sub.Website!), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Key.Length > 0)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // Contact discovery runs against every hit's website in parallel — each page fetch carries
        // its own short timeout, so one slow site can't stall the search.
        var mapped = await Task.WhenAll(found.Hits.Select(async hit =>
        {
            var contact = await contactFinder.FindAsync(hit.Url, cancellationToken);

            // The persisted name comes from the company's own site (og:site_name / <title>); the
            // search-result title is only a transient display fallback.
            var name = contact.Name ?? CompanyNameFrom(hit.Title, hit.Domain);

            var known = byDomain.TryGetValue(hit.Domain, out var matched) ? matched
                : byName.TryGetValue(name, out matched) ? matched
                : null;

            var email = known is not null && !string.IsNullOrWhiteSpace(known.ContactEmail)
                ? known.ContactEmail
                : contact.Email;

            return new LocalSubcontractor(
                PlaceId: hit.Domain,
                Name: known?.CompanyName ?? name,
                Address: hit.Description,
                Phone: contact.Phone,
                Website: hit.Url,
                Rating: null,
                RatingCount: 0,
                Email: email,
                ExistingSubcontractorId: known?.SubcontractorId);
        }));

        // Only companies we can actually email make the list.
        var results = mapped.Where(hit => !string.IsNullOrWhiteSpace(hit.Email)).ToList();

        // The next page's exclusion list is everything shown so far — including this page's
        // email-less rejects, which a re-search should not surface again either.
        var shown = excludeDomains
            .Concat(found.Hits.Select(hit => hit.Domain))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new LocalSubcontractorSearchResult(
            results,
            found.HasMore && shown.Count <= MaxExcludedDomains ? string.Join(",", shown) : null);
    }

    // "SilvaTree Landscaping | Garden Design Bromley" → "SilvaTree Landscaping". Falls back through
    // title segments to the bare domain when the title is generic.
    private static string CompanyNameFrom(string title, string domain)
    {
        var segments = title.Split(TitleSeparators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            if (segment.Length < 3) continue;
            if (segment.Equals("home", StringComparison.OrdinalIgnoreCase)) continue;
            if (segment.Equals("welcome", StringComparison.OrdinalIgnoreCase)) continue;
            return segment;
        }
        return domain;
    }

    private static string DomainOf(string website)
    {
        var candidate = website.Trim();
        if (!candidate.Contains("://")) candidate = "https://" + candidate;
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri)) return "";
        return uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.Host[4..] : uri.Host;
    }
}
