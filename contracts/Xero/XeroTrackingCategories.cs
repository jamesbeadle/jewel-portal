using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Xero;

/// <summary>
/// Asks the API for the organisation's Xero tracking categories with their options, exactly as
/// Xero holds them — the Cost codes page's "Xero sites" and "Xero cost codes" tabs, which exist so
/// the exact phrasing of each option can be read off when linking projects (XeroSiteName) and cost
/// codes to Xero. Archived options are included and flagged, because a retired option's name still
/// explains historical tracking. The API caches the Xero read briefly to respect Xero's rate
/// limits (this endpoint is the one that answers 429 when hammered); <paramref name="Force"/>
/// bypasses that cache for an explicit user refresh.
/// </summary>
public sealed record ListXeroTrackingCategories(bool Force = false) : IQuery<XeroTrackingCategoriesSnapshot>;

/// <summary>
/// What the API saw when it asked Xero for tracking categories. Mirrors
/// <see cref="XeroSuppliersSnapshot"/>: <see cref="IsConfigured"/> false = no Xero credentials
/// (the UI explains rather than erroring); <see cref="Error"/> carries a human-readable failure
/// when Xero itself said no (a missing accounting.settings scope or a 429 land here);
/// <see cref="FetchedAtUtc"/> is when Xero was actually read (older than 'now' when cached).
/// ALL categories come back, not just the two the portal uses — seeing what exists is the point —
/// with <see cref="XeroTrackingCategory.IsSiteCategory"/> /
/// <see cref="XeroTrackingCategory.IsCostCodeCategory"/> stamped on the ones that match the
/// configured Sites / Cost Code names (spacing/case tolerant, same matching as the write-back).
/// </summary>
public sealed record XeroTrackingCategoriesSnapshot(
    bool IsConfigured,
    string? Error,
    DateTimeOffset? FetchedAtUtc,
    IReadOnlyList<XeroTrackingCategory> Categories)
{
    public static XeroTrackingCategoriesSnapshot NotConfigured() =>
        new(false, null, null, Array.Empty<XeroTrackingCategory>());

    public static XeroTrackingCategoriesSnapshot Failed(string error) =>
        new(true, error, null, Array.Empty<XeroTrackingCategory>());
}

/// <summary>One tracking category as Xero holds it, options in Xero's own order.</summary>
public sealed record XeroTrackingCategory(
    string TrackingCategoryId,
    string Name,
    string Status,
    IReadOnlyList<XeroTrackingOption> Options,
    bool IsSiteCategory = false,
    bool IsCostCodeCategory = false);

/// <summary>One option within a tracking category, name exactly as Xero spells it.</summary>
public sealed record XeroTrackingOption(
    string TrackingOptionId,
    string Name,
    string Status)
{
    public bool IsArchived => string.Equals(Status, "ARCHIVED", StringComparison.OrdinalIgnoreCase);
}
