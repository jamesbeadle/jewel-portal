namespace Jewel.JPMS.Api.Features.Progress.Documents;

/// <summary>
/// Everything the progress report PDF needs, resolved up front so the renderer stays a pure
/// function of the model (no I/O, no database): project header details, narrative sections and
/// the selected updates with their photo bytes already loaded from blob storage.
/// </summary>
public sealed record ProgressReportDocumentModel(
    string Title,
    string ProjectName,
    string ProjectReference,
    string ClientName,
    DateTimeOffset? PeriodStart,
    DateTimeOffset? PeriodEnd,
    string Introduction,
    string WorkCompleted,
    string UpcomingWorks,
    string PreparedByEmail,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<ProgressReportDocumentUpdate> Updates);

public sealed record ProgressReportDocumentUpdate(
    string Title,
    string Description,
    DateTimeOffset? WorkDate,
    IReadOnlyList<ProgressReportDocumentPhoto> Photos);

public sealed record ProgressReportDocumentPhoto(byte[] Content);
