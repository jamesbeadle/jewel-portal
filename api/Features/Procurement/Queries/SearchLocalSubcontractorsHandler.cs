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
public sealed class SearchLocalSubcontractorsHandler
    : IQueryHandler<SearchLocalSubcontractors, LocalSubcontractorSearchResult>
{
    private readonly JpmsContext context;
    private readonly IGooglePlacesClient places;
    private readonly GooglePlacesOptions options;

    public SearchLocalSubcontractorsHandler(JpmsContext context, IGooglePlacesClient places, GooglePlacesOptions options)
    {
        this.context = context; this.places = places; this.options = options;
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

        // Flag hits that already exist in the directory (by company name) so they aren't duplicated.
        var existingByName = await context.Subcontractors
            .Select(sub => new { sub.SubcontractorId, sub.CompanyName })
            .ToListAsync(cancellationToken);
        var lookup = existingByName
            .GroupBy(sub => sub.CompanyName.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().SubcontractorId, StringComparer.OrdinalIgnoreCase);

        var results = page.Places
            .Select(hit => new LocalSubcontractor(
                hit.PlaceId, hit.Name, hit.Address, hit.Phone, hit.Website, hit.Rating, hit.RatingCount,
                ExistingSubcontractorId: lookup.TryGetValue(hit.Name.Trim(), out var id) ? id : null))
            .ToList();

        return new LocalSubcontractorSearchResult(results, page.NextPageToken);
    }
}
