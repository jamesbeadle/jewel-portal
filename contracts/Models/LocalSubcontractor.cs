namespace Jewel.JPMS.Models;

/// <summary>
/// A company found near a project's site by the Google Places search — a candidate to invite to
/// tender. Places supplies name/address/phone/website/rating but never an email address, so an
/// invited company lands in the directory without one until someone fills it in.
/// </summary>
public sealed record LocalSubcontractor(
    string PlaceId,
    string Name,
    string Address,
    string? Phone,
    string? Website,
    double? Rating,
    int RatingCount,
    // Set when a directory entry with the same company name already exists, so the UI can mark it
    // and invite the existing entry rather than create a duplicate.
    string? ExistingSubcontractorId = null);

/// <summary>
/// One page of local-subcontractor search results. NextPageToken drives "load more" (Google Places
/// pagination); Error carries a human-readable reason when the search couldn't run at all
/// (unconfigured API key, project missing its address) so the UI can explain rather than fail.
/// </summary>
public sealed record LocalSubcontractorSearchResult(
    IReadOnlyList<LocalSubcontractor> Results,
    string? NextPageToken = null,
    string? Error = null);
