using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Progress.Storage;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Progress;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

/// <summary>
/// Deletes a progress update, its photo rows and their stored files, and removes it from any
/// report selections. Rows are removed first; blob deletion follows and is best-effort per file
/// (a missing blob is a no-op).
/// </summary>
public sealed class DeleteProgressUpdateHandler
    : ICommandHandler<DeleteProgressUpdate, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly IProgressPhotoStore photoStore;

    public DeleteProgressUpdateHandler(JpmsContext context, IProgressPhotoStore photoStore)
    {
        this.context = context;
        this.photoStore = photoStore;
    }

    public async Task<Acknowledgement> HandleAsync(DeleteProgressUpdate command, CancellationToken cancellationToken)
    {
        var update = await context.ProgressUpdates.FindAsync(new object[] { command.ProgressUpdateId }, cancellationToken);
        if (update is null) throw new InvalidOperationException($"Progress update {command.ProgressUpdateId} not found.");

        var photos = await context.ProgressPhotos
            .Where(row => row.ProgressUpdateId == command.ProgressUpdateId)
            .ToListAsync(cancellationToken);
        var selections = await context.ProgressReportSelections
            .Where(row => row.ProgressUpdateId == command.ProgressUpdateId)
            .ToListAsync(cancellationToken);

        context.ProgressPhotos.RemoveRange(photos);
        context.ProgressReportSelections.RemoveRange(selections);
        context.ProgressUpdates.Remove(update);
        await context.SaveChangesAsync(cancellationToken);

        foreach (var photo in photos.Where(photo => !string.IsNullOrWhiteSpace(photo.BlobRef)))
            await photoStore.DeleteAsync(photo.BlobRef, cancellationToken);

        return new Acknowledgement(command.ProgressUpdateId);
    }
}
