using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

/// <summary>
/// Persists a progress update and its photo rows in one save. The photo files have already been
/// streamed to blob storage by the endpoint; this handler only records the resulting refs.
/// </summary>
public sealed class CreateProgressUpdateWithPhotosHandler
    : ICommandHandler<CreateProgressUpdateWithPhotos, ProgressUpdate>
{
    private readonly JpmsContext context;

    public CreateProgressUpdateWithPhotosHandler(JpmsContext context) { this.context = context; }

    public async Task<ProgressUpdate> HandleAsync(CreateProgressUpdateWithPhotos command, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var update = ProgressUpdateRows.New(
            command.ProgressUpdateId, command.ProjectId, command.Title, command.Description,
            command.WorkDate, command.Weather, command.CreatedByEmail, now);
        context.ProgressUpdates.Add(update);

        var photos = new List<ProgressPhotoEntity>();
        foreach (var photo in command.Photos)
        {
            var entity = ProgressPhotoRows.New(photo, command.ProgressUpdateId, command.ProjectId, photo.SortOrder, command.CreatedByEmail, now);
            photos.Add(entity);
            context.ProgressPhotos.Add(entity);
        }

        await context.SaveChangesAsync(cancellationToken);
        return update.ToModel(photos.Select(photo => photo.ToModel()).ToList());
    }
}
