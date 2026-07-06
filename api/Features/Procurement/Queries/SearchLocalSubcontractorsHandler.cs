using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Places;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

// Finds companies of a trade near the project's site: a web search (Brave) locates company
// websites, then each site is visited to discover a contact email and phone. Hits with no findable
// email are excluded — an invite that can't be emailed is no invite. Failures are returned as a
// readable Error on the result (not thrown) so the UI can explain: key not configured, project
// missing its address, or the search refusing the call. Hits are matched against the directory by
// company name or website so the UI can invite an existing entry instead of duplicating it.
public sealed class SearchLocalSubcontractorsHandler
    : IQueryHandler<SearchLocalSubcontractors, LocalSubcontractorSearchResult>
{
    private static readonly char[] TitleSeparators = { '|', '-', '–', '—', '·' };

    private readonly JpmsContext context;
    private readonly ILocalBusinessSearch search;
    private readonly BraveSearchOptions options;
    private readonly IWebsiteContactFinder contactFinder;

    public SearchLocalSubcontractorsHandler(
        JpmsContext context, ILocalBusinessSearch search, BraveSearchOptions options, IWebsiteContactFinder contactFinder)
    {
        this.context = context; this.search = search; this.options = options; this.contactFinder = contactFinder;
    }

    public async Task<LocalSubcontractorSearchResult> HandleAsync(SearchLocalSubcontractors query, CancellationToken cancellationToken)
    {
        static LocalSubcontractorSearchResult Fail(string message) =>
            new(Array.Empty<LocalSubcontractor>(), Error: message);

        if (!options.IsConfigured)
            return Fail("The local search isn't configured yet — add the BraveSearch__ApiKey application setting.");

        if (string.IsNullOrWhiteSpace(query.Trade))
            return Fail("Choose a trade to search for.");

        var project = await context.Projects.FindAsync(new object[] { query.ProjectId }, cancellationToken);
        if (project is null)
            return Fail("Project not found.");

        var location = string.Join(", ", new[] { project.Town, project.Postcode }
            .Where(part => !string.IsNullOrWhiteSpace(part)));
        if (location.Length == 0)
            return Fail("This project has no town or postcode yet. Add the site address under Edit details on the project, then search again.");

        // PageToken is simply the page number of the underlying web search.
        var page = int.TryParse(query.PageToken, out var parsed) && parsed > 0 ? parsed : 0;

        var found = await search.SearchAsync(query.Trade.Trim(), location, page, cancellationToken);
        if (found is null)
            return Fail("The local search failed. Check the Brave Search API key is valid, then try again.");

        // Directory matching (by company name or website domain) so known companies aren't duplicated,
        // and their directory email is reused when present.
        var directory = await context.Subcontractors
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
            var name = CompanyNameFrom(hit.Title, hit.Domain);

            var known = byDomain.TryGetValue(hit.Domain, out var matched) ? matched
                : byName.TryGetValue(name, out matched) ? matched
                : null;

            var contact = await contactFinder.FindAsync(hit.Url, cancellationToken);
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

        return new LocalSubcontractorSearchResult(
            results,
            found.HasMore ? (page + 1).ToString() : null);
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
