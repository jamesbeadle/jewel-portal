using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Places;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

// Finds companies of a trade near the project's site via Google Places text search. Failures are
// returned as a readable Error on the result (not thrown) so the UI can explain: key not
// configured, project missing its address, or Places refusing the call. Hits are matched against
// the directory by company name so the UI can invite an existing entry instead of duplicating it.
// Places never returns email addresses, so each hit's email is taken from its directory entry or
// discovered on the company's website — hits with no findable email are excluded (an invite that
// can't be emailed is no invite).
public sealed class SearchLocalSubcontractorsHandler
    : IQueryHandler<SearchLocalSubcontractors, LocalSubcontractorSearchResult>
{
    private readonly JpmsContext context;
    private readonly IGooglePlacesClient places;
    private readonly GooglePlacesOptions options;
    private readonly IWebsiteEmailFinder emailFinder;

    public SearchLocalSubcontractorsHandler(
        JpmsContext context, IGooglePlacesClient places, GooglePlacesOptions options, IWebsiteEmailFinder emailFinder)
    {
        this.context = context; this.places = places; this.options = options; this.emailFinder = emailFinder;
    }

    public async Task<LocalSubcontractorSearchResult> HandleAsync(SearchLocalSubcontractors query, CancellationToken cancellationToken)
    {
        static LocalSubcontractorSearchResult Fail(string message) =>
            new(Array.Empty<LocalSubcontractor>(), Error: message);

        if (!options.IsConfigured)
            return Fail("The Google Places search isn't configured yet — add the GooglePlaces__ApiKey application setting.");

        if (string.IsNullOrWhiteSpace(query.Trade))
            return Fail("Choose a trade to search for.");

        var project = await context.Projects.FindAsync(new object[] { query.ProjectId }, cancellationToken);
        if (project is null)
            return Fail("Project not found.");

        var location = string.Join(", ", new[] { project.Town, project.Postcode }
            .Where(part => !string.IsNullOrWhiteSpace(part)));
        if (location.Length == 0)
            return Fail("This project has no town or postcode yet. Add the site address under Edit details on the project, then search again.");

        var page = await places.SearchTextAsync($"{query.Trade.Trim()} contractors near {location}", query.PageToken, cancellationToken);
        if (page is null)
            return Fail("The Places search failed. Check the API key is valid and the Places API (New) is enabled for it, then try again.");

        // Flag hits that already exist in the directory (by company name) so they aren't duplicated,
        // and reuse the directory's email when it has one.
        var existingByName = await context.Subcontractors
            .Select(sub => new { sub.SubcontractorId, sub.CompanyName, sub.ContactEmail })
            .ToListAsync(cancellationToken);
        var lookup = existingByName
            .GroupBy(sub => sub.CompanyName.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // Email discovery runs against every hit's website in parallel — each page fetch carries its
        // own short timeout, so one slow site can't stall the search.
        var mapped = await Task.WhenAll(page.Places.Select(async hit =>
        {
            string? subcontractorId = null;
            string? email = null;
            if (lookup.TryGetValue(hit.Name.Trim(), out var known))
            {
                subcontractorId = known.SubcontractorId;
                email = string.IsNullOrWhiteSpace(known.ContactEmail) ? null : known.ContactEmail;
            }
            if (email is null && !string.IsNullOrWhiteSpace(hit.Website))
                email = await emailFinder.FindAsync(hit.Website!, cancellationToken);

            return new LocalSubcontractor(
                hit.PlaceId, hit.Name, hit.Address, hit.Phone, hit.Website, hit.Rating, hit.RatingCount,
                Email: email, ExistingSubcontractorId: subcontractorId);
        }));

        // Only companies we can actually email make the list.
        var results = mapped.Where(hit => !string.IsNullOrWhiteSpace(hit.Email)).ToList();

        return new LocalSubcontractorSearchResult(results, page.NextPageToken);
    }
}
