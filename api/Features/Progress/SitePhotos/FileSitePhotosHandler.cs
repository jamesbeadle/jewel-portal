using Jewel.JPMS.Api.Features.Progress.Storage;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.SitePhotos;

/// <summary>
/// Files pool photos onto one existing update: each photo's prepared file is copied under the
/// update's own key, a progress photo row is written after the update's current photos with the
/// pool's content hash (so the ordinary photo dedupe recognises it from now on), and the pool row
/// is stamped with where it went. One save for the batch; a photo that cannot be filed is its own
/// outcome and never stops the others.
/// </summary>
public sealed class FileSitePhotosHandler : ICommandHandler<FileSitePhotos, SitePhotoFilingResult>
{
    private readonly JpmsContext context;
    private readonly IProgressPhotoStore photoStore;

    public FileSitePhotosHandler(JpmsContext context, IProgressPhotoStore photoStore)
    {
        this.context = context;
        this.photoStore = photoStore;
    }

    public async Task<SitePhotoFilingResult> HandleAsync(FileSitePhotos command, CancellationToken cancellationToken)
    {
        var update = await context.ProgressUpdates.FindAsync(new object[] { command.ProgressUpdateId }, cancellationToken);
        if (update is null) throw new InvalidOperationException($"Progress update {command.ProgressUpdateId} not found.");

        var held = await context.ProgressPhotos
            .Where(row => row.ProgressUpdateId == command.ProgressUpdateId)
            .OrderBy(row => row.SortOrder)
            .ToListAsync(cancellationToken);

        var filer = new SitePhotoFiler(context, photoStore, update, held, command.FiledByEmail, DateTimeOffset.UtcNow);
        var outcomes = new List<SitePhotoFilingOutcome>();
        foreach (var sitePhotoId in command.SitePhotoIds.Distinct(StringComparer.Ordinal))
        {
            outcomes.Add(await filer.FileAsync(sitePhotoId, cancellationToken));
        }

        await context.SaveChangesAsync(cancellationToken);
        return new SitePhotoFilingResult(update.ToModel(filer.PhotosOnTheUpdate), outcomes);
    }
}
