using Jewel.JPMS.Api.Features.Progress.Storage;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.Photos;

/// <summary>
/// Takes a batch of images for one progress update: prepares each, skips one whose content the
/// update already holds (or an earlier image in the batch does), stores the rest in order, and
/// answers with an outcome per image. A failed image is its own outcome, never the batch's.
/// </summary>
public sealed class ProgressPhotoIntake
{
    private readonly JpmsContext context;
    private readonly IProgressPhotoStore photoStore;

    public ProgressPhotoIntake(JpmsContext context, IProgressPhotoStore photoStore)
    {
        this.context = context;
        this.photoStore = photoStore;
    }

    public sealed record Taken(IReadOnlyList<NewProgressPhoto> Stored, IReadOnlyList<ProgressPhotoIntakeOutcome> Outcomes);

    /// <summary>Prepares and stores the batch against an update that may not yet have a row
    /// (a create) or already has one (an add). Nothing is written to the database here.</summary>
    public async Task<Taken> TakeAsync(
        string projectId, string progressUpdateId, IReadOnlyList<IncomingProgressPhoto> images, CancellationToken cancellationToken)
    {
        var alreadyHeld = await HashesHeldByAsync(progressUpdateId, cancellationToken);
        var plan = ProgressPhotoBatchPlan.Build(images, alreadyHeld);

        var stored = new List<NewProgressPhoto>();
        var outcomes = new List<ProgressPhotoIntakeOutcome>();
        foreach (var step in plan)
        {
            outcomes.Add(step.Prepared is null
                ? step.Outcome
                : await StoreAsync(projectId, progressUpdateId, step.Prepared, stored, cancellationToken));
        }
        return new Taken(stored, outcomes);
    }

    /// <summary>Content hash → file name of every photo the update already holds.</summary>
    private async Task<Dictionary<string, string>> HashesHeldByAsync(string progressUpdateId, CancellationToken cancellationToken)
    {
        var rows = await context.ProgressPhotos.AsNoTracking()
            .Where(row => row.ProgressUpdateId == progressUpdateId && row.ContentHash != "")
            .Select(row => new { row.ContentHash, row.FileName })
            .ToListAsync(cancellationToken);
        var held = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var row in rows) held.TryAdd(row.ContentHash, row.FileName);
        return held;
    }

    private async Task<ProgressPhotoIntakeOutcome> StoreAsync(
        string projectId, string progressUpdateId, PreparedProgressPhoto prepared,
        List<NewProgressPhoto> stored, CancellationToken cancellationToken)
    {
        var photoId = ProgressIdentifierFactory.NextProgressPhotoId();
        try
        {
            using var content = new MemoryStream(prepared.Bytes);
            var blobRef = await photoStore.UploadAsync(
                projectId, progressUpdateId, photoId, prepared.FileName, prepared.ContentType, content, cancellationToken);
            stored.Add(new NewProgressPhoto(photoId, prepared.FileName, blobRef, prepared.ContentType,
                prepared.Bytes.Length, stored.Count, prepared.ContentHash));
            return new ProgressPhotoIntakeOutcome(prepared.FileName, ProgressPhotoIntakeResult.Stored, photoId, "Stored.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ProgressPhotoIntakeOutcome(prepared.FileName, ProgressPhotoIntakeResult.Failed, null,
                $"Could not be stored — check the progress photo storage configuration. ({ex.Message})");
        }
    }
}
