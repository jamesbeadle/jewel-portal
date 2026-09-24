using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Features.Progress.SitePhotos;

/// <summary>The Site photos page's door to the pool: the list, the drop, the restore, the delete.
/// Nothing here files or archives a photo — the assistant does both over the connector.</summary>
public interface ISitePhotoStore
{
    Task<IReadOnlyList<SitePhoto>> ListAsync(CancellationToken cancellationToken);

    Task<SitePhotoUploadResult> UploadAsync(IReadOnlyList<IBrowserFile> files, CancellationToken cancellationToken);

    Task RestoreAsync(string sitePhotoId, CancellationToken cancellationToken);

    Task DeleteAsync(string sitePhotoId, CancellationToken cancellationToken);
}
