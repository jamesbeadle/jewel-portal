using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Progress;

/// <summary>
/// Adds photos to an existing progress update. As with <see cref="CreateProgressUpdate"/>, the
/// files have already been streamed to blob storage by the endpoint; this command carries the
/// blob refs. Sent as multipart/form-data by the front-end store, not via the JSON command sender.
/// </summary>
public sealed record AddProgressPhotos(
    string ProgressUpdateId,
    string UploadedByEmail,
    IReadOnlyList<NewProgressPhoto> Photos) : ICommand<ProgressUpdate>;
