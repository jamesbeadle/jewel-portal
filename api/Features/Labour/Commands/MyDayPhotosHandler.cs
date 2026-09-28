using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Progress.Commands;
using Jewel.JPMS.Api.Features.Progress.Photos;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

/// <summary>The day's photographs onto the worker's own note: the note is theirs only when their
/// sign-out wrote it, and the photographs go through the one intake every progress photograph
/// takes, appended in the order posted.</summary>
public sealed class MyDayPhotosHandler
{
    private readonly JpmsContext context;
    private readonly ProgressPhotoIntake intake;
    private readonly AddProgressPhotosValidation validation;
    private readonly ICommandHandler<AddProgressPhotos, ProgressUpdate> photos;

    public MyDayPhotosHandler(
        JpmsContext context,
        ProgressPhotoIntake intake,
        AddProgressPhotosValidation validation,
        ICommandHandler<AddProgressPhotos, ProgressUpdate> photos)
    {
        this.context = context;
        this.intake = intake;
        this.validation = validation;
        this.photos = photos;
    }

    public async Task<ProgressUpdateEntity?> OwnNoteAsync(string progressUpdateId, string email, CancellationToken cancellationToken)
    {
        var note = await context.ProgressUpdates.AsNoTracking()
            .FirstOrDefaultAsync(row => row.ProgressUpdateId == progressUpdateId, cancellationToken);
        var isOwnNote = note is not null && MyDayNotes.IsOwnedBy(note, email);
        return isOwnNote ? note : null;
    }

    internal Task<ProgressPhotoBatches.Added> AddAsync(
        ProgressUpdateEntity note, string email, IReadOnlyList<IncomingProgressPhoto> images, CancellationToken cancellationToken) =>
        ProgressPhotoBatches.AddAsync(intake, validation, photos, note.ProjectId, note.ProgressUpdateId, email, images, cancellationToken);
}
