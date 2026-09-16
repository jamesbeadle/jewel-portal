using Jewel.JPMS.Api.Features.Progress.Storage;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.SitePhotos;

/// <summary>Removes a pool row and its pool file. A copy already filed onto an update is that
/// update's own photo and stays.</summary>
public sealed class DeleteSitePhotoHandler : ICommandHandler<DeleteSitePhoto, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly IProgressPhotoStore photoStore;

    public DeleteSitePhotoHandler(JpmsContext context, IProgressPhotoStore photoStore)
    {
        this.context = context;
        this.photoStore = photoStore;
    }

    public async Task<Acknowledgement> HandleAsync(DeleteSitePhoto command, CancellationToken cancellationToken)
    {
        var photo = await context.SitePhotos.FirstOrDefaultAsync(row => row.SitePhotoId == command.SitePhotoId, cancellationToken);
        if (photo is null) throw new InvalidOperationException($"Site photo {command.SitePhotoId} not found.");

        context.SitePhotos.Remove(photo);
        await context.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(photo.BlobRef))
            await photoStore.DeleteAsync(photo.BlobRef, cancellationToken);

        return new Acknowledgement(command.SitePhotoId);
    }
}
