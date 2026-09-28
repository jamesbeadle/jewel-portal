namespace Jewel.JPMS.Api.Features.SiteAccess.Site;

/// <summary>One drawing as the scan's page lists it: where it lives, what the register calls it,
/// the revision shown and whether that revision is approved.</summary>
public sealed record SiteDrawingRow(
    string FolderPath,
    string Label,
    string RevisionText,
    bool IsApproved,
    string ReceivedText,
    string DrawingRevisionId);
