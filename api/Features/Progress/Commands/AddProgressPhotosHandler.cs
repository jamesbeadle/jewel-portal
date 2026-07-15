using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

/// <summary>Appends photo rows to an existing update (files already in blob storage), placing
/// them after the update's current photos.</summary>
public sealed class AddProgressPhotosHandler
    : ICommandHandler<AddProgressPhotos, ProgressUpdate>
{
    private readonly JpmsContext context;

    public AddProgressPhotosHandler(JpmsContext context) { this.context = context; }

    public async Task<ProgressUpdate> HandleAsync(AddProgressPhotos command, CancellationToken cancellationToken)
    {
        var update = await context.ProgressUpdates.FindAsync(new object[] { command.ProgressUpdateId }, cancellationToken);
        if (update is null) throw new InvalidOperationException($"Progress update {command.ProgressUpdateId} not found.");

        var existing = await context.ProgressPhotos
            .Where(row => row.ProgressUpdateId == command.ProgressUpdateId)
            .OrderBy(row => row.SortOrder)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var nextSortOrder = existing.Count == 0 ? 0 : existing[^1].SortOrder + 1;

        var added = new List<ProgressPhotoEntity>();
        foreach (var photo in command.Photos)
        {
            var entity = new ProgressPhotoEntity
            {
                ProgressPhotoId = photo.ProgressPhotoId,
                ProgressUpdateId = command.ProgressUpdateId,
                ProjectId = update.ProjectId,
                FileName = photo.FileName,
                BlobRef = photo.BlobRef,
                ContentType = photo.ContentType,
                FileSizeBytes = photo.FileSizeBytes,
                SortOrder = nextSortOrder++,
                UploadedByEmail = command.UploadedByEmail,
                UploadedAt = now
            };
            added.Add(entity);
            context.ProgressPhotos.Add(entity);
        }

        await context.SaveChangesAsync(cancellationToken);

        var all = existing.Concat(added).Select(photo => photo.ToModel()).ToList();
        return update.ToModel(all);
    }
}
