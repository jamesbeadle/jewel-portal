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
    ProgressWeather? Weather,
    string CreatedByEmail,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ProgressPhoto> Photos);

/// <summary>
/// Weather conditions on site for a progress update, entered manually by the site manager.
/// Units follow the client-facing report convention: temperatures in °C, wind in mph,
/// precipitation in inches. Every field is optional; null on the update means nothing recorded.
/// </summary>
public sealed record ProgressWeather(
    string Summary,
    DateTimeOffset? ObservedAt,
    int? TempHighC,
    int? TempLowC,
    int? WindMph,
    int? HumidityPercent,
    decimal? PrecipInches);

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
