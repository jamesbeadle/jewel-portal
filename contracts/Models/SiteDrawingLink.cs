namespace Jewel.JPMS.Models;

/// <summary>
/// A link printed on a QR poster for one folder of a project's document register: anyone on site
/// who scans it reads that folder's current drawings with no sign-in, until it expires or is
/// revoked. The secret never travels here — only its hash is stored — and the raw URL is answered
/// once, at creation, by <see cref="SiteDrawingLinkCreated"/>.
/// </summary>
public sealed record SiteDrawingLink(
    string SiteDrawingLinkId,
    string ProjectId,
    string DrawingFolderId,
    bool IncludeSubFolders,
    string Label,
    string CreatedByEmail,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt,
    int ScanCount,
    DateTimeOffset? LastScannedAt)
{
    public bool IsRevoked => RevokedAt is not null;
    public bool HasExpired => ExpiresAt <= DateTimeOffset.UtcNow;
    public bool IsActive => !IsRevoked && !HasExpired;
}

/// <summary>
/// The answer to creating a link: the link, and the ONE copy of its URL, its QR code and its A4
/// poster that will ever exist. Nothing stored can reproduce them — losing the poster means
/// revoking the link and minting another.
/// </summary>
public sealed record SiteDrawingLinkCreated(
    SiteDrawingLink Link,
    string Url,
    string QrPngBase64,
    string PosterPdfBase64);

/// <summary>The bounds a site link is created within: the one statement the API's validation and
/// the create dialog both read.</summary>
public static class SiteDrawingLinkLimits
{
    public const int LabelLength = 128;
    public const int DefaultExpiryDays = 90;
    public const int ShortestExpiryDays = 1;
    public const int LongestExpiryDays = 365;
    public const int SecretBytes = 16;
}

/// <summary>Who curates a project's site links: the roles that shape the document register's
/// folders. A poster is register curation, so the gate is the folders' own.</summary>
public static class SiteDrawingLinkRoles
{
    public static readonly RoleSet Curators =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);
}
