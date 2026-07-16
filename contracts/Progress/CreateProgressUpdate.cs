using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Progress;

/// <summary>
/// Creates a progress update (a group of photos with a description). The photo files have already
/// been streamed to blob storage by the endpoint; this command carries the resulting blob refs and
/// file metadata. The endpoint owns identifier generation so the blob paths and the persisted rows
/// share ids. Sent as multipart/form-data by the front-end store, not via the JSON command sender.
/// </summary>
public sealed record CreateProgressUpdate(
    string ProgressUpdateId,
    string ProjectId,
    string Title,
    string Description,
    DateTimeOffset? WorkDate,
    ProgressWeather? Weather,
    string CreatedByEmail,
    IReadOnlyList<NewProgressPhoto> Photos) : ICommand<ProgressUpdate>;

public sealed record NewProgressPhoto(
    string ProgressPhotoId,
    string FileName,
    string BlobRef,
    string ContentType,
    long FileSizeBytes,
    int SortOrder);
