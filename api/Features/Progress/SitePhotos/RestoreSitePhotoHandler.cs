using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.SitePhotos;

/// <summary>Takes a photograph out of the archive and back into the waiting view, clearing why it
/// was set aside. Restoring one that is not archived changes nothing.</summary>
public sealed class RestoreSitePhotoHandler : ICommandHandler<RestoreSitePhoto, Acknowledgement>
{
    private readonly JpmsContext context;

    public RestoreSitePhotoHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(RestoreSitePhoto command, CancellationToken cancellationToken)
    {
        var photo = await context.SitePhotos.FirstOrDefaultAsync(row => row.SitePhotoId == command.SitePhotoId, cancellationToken);
        if (photo is null) throw new InvalidOperationException($"Site photo {command.SitePhotoId} not found.");

        photo.ArchivedAt = null;
        photo.ArchiveReason = null;
        photo.ArchiveNote = "";
        photo.ArchivedForProjectId = null;
        photo.ArchivedForPeriodEnd = null;
        photo.ArchivedByEmail = "";
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(command.SitePhotoId);
    }
}
