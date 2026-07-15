namespace Jewel.JPMS.Models;

/// <summary>
/// A site manager's record of progress on the works: a group of photos with a description.
/// Updates are the raw material from which client-facing progress reports are assembled.
/// </summary>
public sealed record ProgressUpdate(
    string ProgressUpdateId,
    string ProjectId,
    string Title,
    string Description,
    DateTimeOffset? WorkDate,
    string CreatedByEmail,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ProgressPhoto> Photos);

public sealed record ProgressPhoto(
    string ProgressPhotoId,
    string ProgressUpdateId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    int SortOrder,
    string UploadedByEmail,
    DateTimeOffset UploadedAt);

/// <summary>
/// A client-facing progress report: narrative sections plus an ordered selection of progress
/// updates whose photos illustrate the completed works. The PDF is rendered from the register
/// on every download, so it always reflects the report as it stands.
/// </summary>
public sealed record ProgressReport(
    string ProgressReportId,
    string ProjectId,
    string Title,
    DateTimeOffset? PeriodStart,
    DateTimeOffset? PeriodEnd,
    string Introduction,
    string WorkCompleted,
    string UpcomingWorks,
    string CreatedByEmail,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> SelectedUpdateIds);
