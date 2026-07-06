namespace Jewel.JPMS.Models;

/// <summary>
/// A company found near a project's site by the Google Places search — a candidate to invite to
/// tender. Places supplies name/address/phone/website/rating but never an email address, so Email
/// is discovered from the company's own website (or the directory, for known companies); results
/// where no email could be found are excluded — an invite that can't be emailed is no invite.
/// </summary>
public sealed record LocalSubcontractor(
    string PlaceId,
    string Name,
    string Address,
    string? Phone,
    string? Website,
    double? Rating,
    int RatingCount,
    // Discovered from the company's website (best-effort) or taken from the directory entry.
    string? Email = null,
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
