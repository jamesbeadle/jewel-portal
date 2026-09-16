namespace Jewel.JPMS.Models;

/// <summary>
/// One photograph in the company-wide site photo pool (2026-09-16, James: "a big dumping ground
/// for photos and any project"). A site manager drops a week's photographs here from the same
/// files that end up in the WhatsApp export folder; the pool knows nothing about the project or
/// the day — the export does, and the assistant files each photo onto its day by fingerprint.
/// <see cref="ContentHash"/> is the SHA-256 (lower-case hex) of the file as received, which is
/// exactly what <c>shasum -a 256</c> answers for the same file on a laptop. Filed once, to one
/// progress update; the row stays so the pool can show what is still unfiled.
/// </summary>
public sealed record SitePhoto(
    string SitePhotoId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string ContentHash,
    string UploadedByEmail,
    DateTimeOffset UploadedAt,
    string? FiledToProjectId,
    string? FiledToProgressUpdateId,
    DateTimeOffset? FiledAt)
{
    public bool IsFiled => !string.IsNullOrEmpty(FiledToProgressUpdateId);
}
