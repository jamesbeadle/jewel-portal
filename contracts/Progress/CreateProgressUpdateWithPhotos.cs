using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Progress;

/// <summary>
/// Creates a progress update together with its photographs in one save. The image files have
/// already been prepared and streamed to blob storage by the endpoint; this command carries the
/// resulting blob refs and file metadata. The endpoint owns identifier generation so the blob
/// paths and the persisted rows share ids. Sent as multipart/form-data by the front-end store,
/// not via the JSON command sender.
/// </summary>
public sealed record CreateProgressUpdateWithPhotos(
    string ProgressUpdateId,
    string ProjectId,
    string Title,
    string Description,
    DateTimeOffset? WorkDate,
    ProgressWeather? Weather,
    string CreatedByEmail,
    IReadOnlyList<NewProgressPhoto> Photos) : ICommand<ProgressUpdate>;

/// <summary>A photograph already in blob storage, waiting for its row. <paramref name="ContentHash"/>
/// is the SHA-256 of the file as it was received, which is what "the same image posted twice"
/// is judged by.</summary>
public sealed record NewProgressPhoto(
    string ProgressPhotoId,
    string FileName,
    string BlobRef,
    string ContentType,
    long FileSizeBytes,
    int SortOrder,
    string ContentHash);
