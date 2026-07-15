using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Progress.Storage;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Progress;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

public sealed class DeleteProgressPhotoHandler
    : ICommandHandler<DeleteProgressPhoto, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly IProgressPhotoStore photoStore;

    public DeleteProgressPhotoHandler(JpmsContext context, IProgressPhotoStore photoStore)
    {
        this.context = context;
        this.photoStore = photoStore;
    }

    public async Task<Acknowledgement> HandleAsync(DeleteProgressPhoto command, CancellationToken cancellationToken)
    {
        var photo = await context.ProgressPhotos
            .FirstOrDefaultAsync(row =>
                row.ProgressPhotoId == command.ProgressPhotoId
                && row.ProgressUpdateId == command.ProgressUpdateId, cancellationToken);
        if (photo is null) throw new InvalidOperationException($"Progress photo {command.ProgressPhotoId} not found.");

        context.ProgressPhotos.Remove(photo);
        await context.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(photo.BlobRef))
            await photoStore.DeleteAsync(photo.BlobRef, cancellationToken);

        return new Acknowledgement(command.ProgressPhotoId);
    }
}
