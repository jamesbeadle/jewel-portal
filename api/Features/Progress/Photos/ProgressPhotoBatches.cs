using Jewel.JPMS.Api.Features.Progress.Commands;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.Photos;

/// <summary>
/// One batch of photographs onto one existing update, the way every caller does it — the page's
/// form, the connector's add_progress_photos and the site photo pool: take the images through
/// the intake, then record the stored ones through the AddProgressPhotos command. When nothing at
/// all was stored the update is untouched and the outcomes say why.
/// </summary>
internal static class ProgressPhotoBatches
{
    public sealed record Added(ProgressPhotoBatchResult? Batch, object? Failure);

    public static async Task<Added> AddAsync(
        ProgressPhotoIntake intake,
        AddProgressPhotosValidation validation,
        ICommandHandler<AddProgressPhotos, ProgressUpdate> handler,
        string projectId, string progressUpdateId, string uploadedByEmail,
        IReadOnlyList<IncomingProgressPhoto> images, CancellationToken cancellationToken)
    {
        var taken = await intake.TakeAsync(projectId, progressUpdateId, images, cancellationToken);
        if (taken.Stored.Count == 0)
            return new Added(null, new { error = "None of the images could be stored.", outcomes = taken.Outcomes });

        var command = new AddProgressPhotos(progressUpdateId, uploadedByEmail, taken.Stored);
        var outcome = validation.Check(command);
        if (outcome.HasFailed) return new Added(null, outcome.Errors);

        var update = await handler.HandleAsync(command, cancellationToken);
        return new Added(new ProgressPhotoBatchResult(update, taken.Outcomes), null);
    }
}
