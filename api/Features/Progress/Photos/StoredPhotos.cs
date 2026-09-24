namespace Jewel.JPMS.Api.Features.Progress.Photos;

/// <summary>A stored photograph named by either of its ids — the site photo pool's or a progress
/// update's — and where its file is kept. Both live in the progress photo store.</summary>
internal sealed record StoredPhoto(string Id, string FileName, string BlobRef);

internal static class StoredPhotos
{
    /// <summary>The photographs the ids name, in the order asked; an id that is neither is left out.</summary>
    public static async Task<IReadOnlyList<StoredPhoto>> FindAsync(JpmsContext context, IReadOnlyList<string> ids, CancellationToken cancellationToken)
    {
        var progress = await context.ProgressPhotos.AsNoTracking()
            .Where(photo => ids.Contains(photo.ProgressPhotoId))
            .Select(photo => new StoredPhoto(photo.ProgressPhotoId, photo.FileName, photo.BlobRef))
            .ToListAsync(cancellationToken);
        var pool = await context.SitePhotos.AsNoTracking()
            .Where(photo => ids.Contains(photo.SitePhotoId))
            .Select(photo => new StoredPhoto(photo.SitePhotoId, photo.FileName, photo.BlobRef))
            .ToListAsync(cancellationToken);
        var byId = progress.Concat(pool).Where(photo => photo.BlobRef.Length > 0).ToDictionary(photo => photo.Id);
        return ids.Distinct().Where(byId.ContainsKey).Select(id => byId[id]).ToList();
    }
}
