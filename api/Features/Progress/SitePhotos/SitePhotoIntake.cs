using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Progress.Photos;
using Jewel.JPMS.Api.Features.Progress.Storage;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.SitePhotos;

/// <summary>
/// Takes a batch of images into the pool: prepares each exactly as a progress photo is prepared,
/// skips one whose content the pool already holds (or an earlier image in the batch does), stores
/// the rest and writes their rows. A failed image is its own outcome, never the batch's.
/// </summary>
public sealed class SitePhotoIntake
{
    private readonly JpmsContext context;
    private readonly IProgressPhotoStore photoStore;

    public SitePhotoIntake(JpmsContext context, IProgressPhotoStore photoStore)
    {
        this.context = context;
        this.photoStore = photoStore;
    }

    public async Task<SitePhotoUploadResult> TakeAsync(
        IReadOnlyList<IncomingProgressPhoto> images, string uploadedByEmail, CancellationToken cancellationToken)
    {
        var alreadyHeld = await HashesHeldByThePoolAsync(images, cancellationToken);
        var plan = ProgressPhotoBatchPlan.Build(images, alreadyHeld);
        var now = DateTimeOffset.UtcNow;

        var outcomes = new List<SitePhotoUploadOutcome>();
        foreach (var step in plan)
        {
            outcomes.Add(step.Prepared is null
                ? SitePhotoUploadOutcomes.From(step.Outcome)
                : await StoreAsync(step.Prepared, uploadedByEmail, now, cancellationToken));
        }
        await context.SaveChangesAsync(cancellationToken);
        return new SitePhotoUploadResult(outcomes);
    }

    /// <summary>Content hash → file name of every pool photo whose content is in this batch.</summary>
    private async Task<Dictionary<string, string>> HashesHeldByThePoolAsync(
        IReadOnlyList<IncomingProgressPhoto> images, CancellationToken cancellationToken)
    {
        var hashes = images.Select(image => ProgressPhotoContentHash.Of(image.Bytes)).Distinct().ToList();
        var rows = await context.SitePhotos.AsNoTracking()
            .Where(row => hashes.Contains(row.ContentHash))
            .Select(row => new { row.ContentHash, row.FileName })
            .ToListAsync(cancellationToken);
        var held = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var row in rows) held.TryAdd(row.ContentHash, $"{row.FileName} already in the pool");
        return held;
    }

    private async Task<SitePhotoUploadOutcome> StoreAsync(
        PreparedProgressPhoto prepared, string uploadedByEmail, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var sitePhotoId = ProgressIdentifierFactory.NextSitePhotoId();
        try
        {
            using var content = new MemoryStream(prepared.Bytes);
            var blobRef = await photoStore.UploadAsync(
                SitePhotoPool.ProjectKey, SitePhotoPool.UpdateKey, sitePhotoId,
                prepared.FileName, prepared.ContentType, content, cancellationToken);
            context.SitePhotos.Add(SitePhotoRows.New(sitePhotoId, prepared, blobRef, uploadedByEmail, now));
            return new SitePhotoUploadOutcome(prepared.FileName, ProgressPhotoIntakeResult.Stored, sitePhotoId, "Stored in the pool.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new SitePhotoUploadOutcome(prepared.FileName, ProgressPhotoIntakeResult.Failed, null,
                $"Could not be stored — check the progress photo storage configuration. ({ex.Message})");
        }
    }
}

internal static class SitePhotoRows
{
    public static SitePhotoEntity New(
        string sitePhotoId, PreparedProgressPhoto prepared, string blobRef, string uploadedByEmail, DateTimeOffset uploadedAt) =>
        new()
        {
            SitePhotoId = sitePhotoId,
            FileName = prepared.FileName,
            BlobRef = blobRef,
            ContentType = prepared.ContentType,
            FileSizeBytes = prepared.Bytes.Length,
            ContentHash = prepared.ContentHash,
            UploadedByEmail = uploadedByEmail,
            UploadedAt = uploadedAt
        };
}

internal static class SitePhotoUploadOutcomes
{
    public static SitePhotoUploadOutcome From(ProgressPhotoIntakeOutcome outcome) =>
        new(outcome.FileName, outcome.Result, null, outcome.Detail);
}
