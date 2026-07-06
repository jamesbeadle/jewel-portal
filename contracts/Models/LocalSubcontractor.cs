namespace Jewel.JPMS.Models;

/// <summary>
/// A company found near a project's site by the local web search — a candidate to invite to
/// tender. The search finds company websites; email and phone are discovered from each company's
/// own site (or the directory, for known companies). Results where no email could be found are
/// excluded — an invite that can't be emailed is no invite. PlaceId is the website domain (the
/// stable identity of a hit); Address carries the search snippet describing the company;
/// Rating/RatingCount are unused by the current provider and kept for shape compatibility.
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
    // Set when a directory entry with the same company name or website already exists, so the UI
    // can mark it and invite the existing entry rather than create a duplicate.
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
