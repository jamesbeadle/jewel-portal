using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

/// <summary>
/// Persists a progress update and its photo rows. The photo files have already been streamed to
/// blob storage by the endpoint; this handler only records the resulting refs.
/// </summary>
public sealed class CreateProgressUpdateHandler
    : ICommandHandler<CreateProgressUpdate, ProgressUpdate>
{
    private readonly JpmsContext context;

    public CreateProgressUpdateHandler(JpmsContext context) { this.context = context; }

    public async Task<ProgressUpdate> HandleAsync(CreateProgressUpdate command, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var update = new ProgressUpdateEntity
        {
            ProgressUpdateId = command.ProgressUpdateId,
            ProjectId = command.ProjectId,
            Title = command.Title.Trim(),
            Description = command.Description.Trim(),
            WorkDate = command.WorkDate,
            CreatedByEmail = command.CreatedByEmail,
            CreatedAt = now
        };
        context.ProgressUpdates.Add(update);

        var photos = new List<ProgressPhotoEntity>();
        foreach (var photo in command.Photos)
        {
            var entity = new ProgressPhotoEntity
            {
                ProgressPhotoId = photo.ProgressPhotoId,
                ProgressUpdateId = command.ProgressUpdateId,
                ProjectId = command.ProjectId,
                FileName = photo.FileName,
                BlobRef = photo.BlobRef,
                ContentType = photo.ContentType,
                FileSizeBytes = photo.FileSizeBytes,
                SortOrder = photo.SortOrder,
                UploadedByEmail = command.CreatedByEmail,
                UploadedAt = now
            };
            photos.Add(entity);
            context.ProgressPhotos.Add(entity);
        }

        await context.SaveChangesAsync(cancellationToken);
        return update.ToModel(photos.Select(photo => photo.ToModel()).ToList());
    }
}
