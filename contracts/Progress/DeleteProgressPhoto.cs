using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Progress;

/// <summary>Permanently deletes a single photo and its stored file. Cannot be undone.</summary>
public sealed record DeleteProgressPhoto(
    string ProgressUpdateId,
    string ProgressPhotoId) : ICommand<Acknowledgement>;
