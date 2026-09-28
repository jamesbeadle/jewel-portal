namespace Jewel.JPMS.Api.Features.SiteAccess.Documents;

/// <summary>What the A4 poster says: the project, the poster's label, when it was issued and
/// expires, and the QR code that carries the link.</summary>
public sealed record SiteDrawingLinkPoster(
    string ProjectName,
    string ProjectReference,
    string Label,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    byte[] QrPng);
