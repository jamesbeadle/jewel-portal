using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Progress.Commands;
using Jewel.JPMS.Api.Features.Progress.Storage;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.SitePhotos;

/// <summary>One filing batch onto one update, photo by photo. Nothing here saves — the handler
/// saves once when the batch is done.</summary>
internal sealed class SitePhotoFiler
{
    private readonly JpmsContext context;
    private readonly IProgressPhotoStore photoStore;
    private readonly ProgressUpdateEntity update;
    private readonly List<ProgressPhotoEntity> photos;
    private readonly string filedByEmail;
    private readonly DateTimeOffset now;
    private int nextSortOrder;

    public SitePhotoFiler(
        JpmsContext context, IProgressPhotoStore photoStore, ProgressUpdateEntity update,
        List<ProgressPhotoEntity> photosAlreadyOnTheUpdate, string filedByEmail, DateTimeOffset now)
    {
        this.context = context;
        this.photoStore = photoStore;
        this.update = update;
        photos = photosAlreadyOnTheUpdate;
        this.filedByEmail = filedByEmail;
        this.now = now;
        nextSortOrder = photos.Count == 0 ? 0 : photos[^1].SortOrder + 1;
    }

    public IReadOnlyList<ProgressPhoto> PhotosOnTheUpdate => photos.Select(photo => photo.ToModel()).ToList();

    public async Task<SitePhotoFilingOutcome> FileAsync(string sitePhotoId, CancellationToken cancellationToken)
    {
        var poolPhoto = await context.SitePhotos.FirstOrDefaultAsync(row => row.SitePhotoId == sitePhotoId, cancellationToken);
        if (poolPhoto is null)
            return new SitePhotoFilingOutcome(sitePhotoId, SitePhotoFiling.NotFound, null, "No pool photo has this id.");
        var refusal = SitePhotoFilingRefusals.For(poolPhoto, update.ProgressUpdateId);
        if (refusal is not null) return refusal;

        var sameContent = photos.FirstOrDefault(photo => photo.ContentHash == poolPhoto.ContentHash);
        if (sameContent is not null)
        {
            Stamp(poolPhoto, sameContent.ProgressPhotoId);
            return new SitePhotoFilingOutcome(sitePhotoId, SitePhotoFiling.AlreadyOnUpdate, sameContent.ProgressPhotoId,
                $"The update already holds this photograph as {sameContent.FileName}.");
        }
        return await CopyOntoTheUpdateAsync(poolPhoto, cancellationToken);
    }

    private async Task<SitePhotoFilingOutcome> CopyOntoTheUpdateAsync(SitePhotoEntity poolPhoto, CancellationToken cancellationToken)
    {
        var progressPhotoId = ProgressIdentifierFactory.NextProgressPhotoId();
        try
        {
            var blob = await photoStore.OpenAsync(poolPhoto.BlobRef, cancellationToken);
            if (blob is null)
                return new SitePhotoFilingOutcome(poolPhoto.SitePhotoId, SitePhotoFiling.Failed, null, "The pool's stored file could not be found.");

            var blobRef = await UploadCopyAsync(blob, poolPhoto, progressPhotoId, cancellationToken);
            var row = ProgressPhotoRows.New(
                new NewProgressPhoto(progressPhotoId, poolPhoto.FileName, blobRef, poolPhoto.ContentType,
                    poolPhoto.FileSizeBytes, nextSortOrder, poolPhoto.ContentHash),
                update.ProgressUpdateId, update.ProjectId, nextSortOrder++, filedByEmail, now);
            context.ProgressPhotos.Add(row);
            photos.Add(row);
            Stamp(poolPhoto, progressPhotoId);
            return new SitePhotoFilingOutcome(poolPhoto.SitePhotoId, SitePhotoFiling.Filed, progressPhotoId, "Filed.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new SitePhotoFilingOutcome(poolPhoto.SitePhotoId, SitePhotoFiling.Failed, null,
                $"Could not be copied onto the update — check the progress photo storage configuration. ({ex.Message})");
        }
    }

    private async Task<string> UploadCopyAsync(
        ProgressPhotoBlob blob, SitePhotoEntity poolPhoto, string progressPhotoId, CancellationToken cancellationToken)
    {
        await using var source = blob.Content;
        using var copy = new MemoryStream();
        await source.CopyToAsync(copy, cancellationToken);
        copy.Position = 0;
        return await photoStore.UploadAsync(
            update.ProjectId, update.ProgressUpdateId, progressPhotoId,
            poolPhoto.FileName, poolPhoto.ContentType, copy, cancellationToken);
    }

    private void Stamp(SitePhotoEntity poolPhoto, string progressPhotoId)
    {
        poolPhoto.FiledToProjectId = update.ProjectId;
        poolPhoto.FiledToProgressUpdateId = update.ProgressUpdateId;
        poolPhoto.FiledToProgressPhotoId = progressPhotoId;
        poolPhoto.FiledByEmail = filedByEmail;
        poolPhoto.FiledAt = now;
    }
}
