using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Progress.Photos;
using Jewel.JPMS.Api.Features.Progress.Storage;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Hs.Thread;

/// <summary>
/// Takes the photographs sent with a comment: each prepared exactly as a progress photo is (HEIC
/// to JPEG, turned the right way up, shrunk, stripped), skipped when the record already holds the
/// same file, stored through the progress photo store under the record's own key, and written as
/// a row on the comment. A failed image is its own outcome, never the batch's.
/// </summary>
public sealed class HsRecordPhotoIntake
{
    private const string ProjectKeyPrefix = "hs-records";
    private readonly JpmsContext context;
    private readonly IProgressPhotoStore photoStore;

    public HsRecordPhotoIntake(JpmsContext context, IProgressPhotoStore photoStore)
    {
        this.context = context;
        this.photoStore = photoStore;
    }

    public async Task<IReadOnlyList<ProgressPhotoIntakeOutcome>> TakeAsync(
        HsRecordCommentEntity comment, IReadOnlyList<IncomingProgressPhoto> images, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var alreadyHeld = await HashesHeldByAsync(comment.HsRecordId, cancellationToken);
        var plan = ProgressPhotoBatchPlan.Build(images, alreadyHeld);
        var outcomes = new List<ProgressPhotoIntakeOutcome>();
        foreach (var step in plan)
        {
            outcomes.Add(step.Prepared is null ? step.Outcome : await StoreAsync(comment, step.Prepared, now, cancellationToken));
        }
        return outcomes;
    }

    private async Task<Dictionary<string, string>> HashesHeldByAsync(string hsRecordId, CancellationToken cancellationToken)
    {
        var rows = await context.HsRecordPhotos.AsNoTracking()
            .Where(row => row.HsRecordId == hsRecordId)
            .Select(row => new { row.ContentHash, row.FileName })
            .ToListAsync(cancellationToken);
        var held = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var row in rows) held.TryAdd(row.ContentHash, row.FileName);
        return held;
    }

    private async Task<ProgressPhotoIntakeOutcome> StoreAsync(
        HsRecordCommentEntity comment, PreparedProgressPhoto prepared, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var photoId = HsIdentifierFactory.NextHsRecordPhotoId();
        try
        {
            using var content = new MemoryStream(prepared.Bytes);
            var blobRef = await photoStore.UploadAsync(
                ProjectKeyPrefix, comment.HsRecordId, photoId, prepared.FileName, prepared.ContentType, content, cancellationToken);
            context.HsRecordPhotos.Add(PhotoRow(comment, photoId, prepared, blobRef, now));
            return new ProgressPhotoIntakeOutcome(prepared.FileName, ProgressPhotoIntakeResult.Stored, photoId, "Stored.");
        }
        catch (Exception failure) when (failure is not OperationCanceledException)
        {
            return new ProgressPhotoIntakeOutcome(prepared.FileName, ProgressPhotoIntakeResult.Failed, null,
                $"Could not be stored — check the progress photo storage configuration. ({failure.Message})");
        }
    }

    private static HsRecordPhotoEntity PhotoRow(
        HsRecordCommentEntity comment, string photoId, PreparedProgressPhoto prepared, string blobRef, DateTimeOffset now) => new()
    {
        HsRecordPhotoId = photoId,
        HsRecordId = comment.HsRecordId,
        HsRecordCommentId = comment.HsRecordCommentId,
        FileName = prepared.FileName,
        BlobRef = blobRef,
        ContentType = prepared.ContentType,
        FileSizeBytes = prepared.Bytes.Length,
        ContentHash = prepared.ContentHash,
        UploadedAt = now
    };
}
