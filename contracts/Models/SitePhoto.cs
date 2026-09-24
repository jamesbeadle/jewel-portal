namespace Jewel.JPMS.Models;

/// <summary>
/// One photograph in the company-wide site photo pool (2026-09-16, James: "a big dumping ground
/// for photos and any project"). A site manager drops a week's photographs here from the same
/// files that end up in the WhatsApp export folder; the pool knows nothing about the project or
/// the day — the export does, and the assistant files each photo onto its day by fingerprint.
/// <see cref="ContentHash"/> is the SHA-256 (lower-case hex) of the file as received, which is
/// exactly what <c>shasum -a 256</c> answers for the same file on a laptop. Filed once, to one
/// progress update; the row stays so the pool can show what is still unfiled. A photo the
/// weekly-report run judged not to be progress is archived instead (<see cref="Archive"/>) —
/// kept, never filed, out of the waiting view.
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
    DateTimeOffset? FiledAt,
    SitePhotoArchive? Archive = null,
    SitePhotoDestination? FiledTo = null)
{
    public bool IsFiled => !string.IsNullOrEmpty(FiledToProgressUpdateId);

    public bool IsArchived => Archive is not null;

    public bool IsWaiting => !IsFiled && !IsArchived;

    public bool IsFiledTo(string projectId) =>
        string.Equals(FiledToProjectId, projectId, StringComparison.OrdinalIgnoreCase);

    public bool IsArchivedFor(string projectId) =>
        string.Equals(Archive?.ProjectId, projectId, StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether the photo is one project's: filed onto its update, or archived from its
    /// week. An unfiled photo is nobody's until the run files it.</summary>
    public bool BelongsTo(string projectId) => IsFiledTo(projectId) || IsArchivedFor(projectId);
}

/// <summary>Where a filed photo went, read for a person (2026-09-24, the FD: "show which project
/// and day each photo is filed to"): the project's reference and name and the update's work day,
/// which is the day the photograph shows — not the day it was filed.</summary>
public sealed record SitePhotoDestination(
    string ProjectReference,
    string ProjectName,
    string ProgressUpdateTitle,
    DateTimeOffset? WorkDate);
