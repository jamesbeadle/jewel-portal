using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Progress;

/// <summary>
/// Adds photos to an existing progress update, after the update's current photos. As with
/// <see cref="CreateProgressUpdateWithPhotos"/>, the files have already been prepared and streamed
/// to blob storage by the endpoint; this command carries the blob refs. Sent as multipart/form-data
/// by the front-end store and by the connector's add_progress_photos, not via the JSON command sender.
/// </summary>
public sealed record AddProgressPhotos(
    string ProgressUpdateId,
    string UploadedByEmail,
    IReadOnlyList<NewProgressPhoto> Photos) : ICommand<ProgressUpdate>;
