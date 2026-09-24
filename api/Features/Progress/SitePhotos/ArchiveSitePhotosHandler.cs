using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.SitePhotos;

/// <summary>
/// Sets pool photographs aside from the weekly report, each stamped with its reason, the sentence
/// saying why, and the project's week it was dumped in. The row and its file stay — archived is
/// kept, just in case. A photo already filed is on a progress update and is refused per photo;
/// one already archived keeps its first reason. One save for the batch.
/// </summary>
public sealed class ArchiveSitePhotosHandler : ICommandHandler<ArchiveSitePhotos, SitePhotoArchivingResult>
{
    private readonly JpmsContext context;

    public ArchiveSitePhotosHandler(JpmsContext context) { this.context = context; }

    public async Task<SitePhotoArchivingResult> HandleAsync(ArchiveSitePhotos command, CancellationToken cancellationToken)
    {
        var ids = command.Photos.Select(photo => photo.SitePhotoId).Distinct().ToList();
        var rows = await context.SitePhotos
            .Where(row => ids.Contains(row.SitePhotoId))
            .ToDictionaryAsync(row => row.SitePhotoId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var outcomes = command.Photos
            .DistinctBy(photo => photo.SitePhotoId)
            .Select(photo => Archive(photo, rows.GetValueOrDefault(photo.SitePhotoId), command, now))
            .ToList();

        await context.SaveChangesAsync(cancellationToken);
        return new SitePhotoArchivingResult(outcomes);
    }

    private static SitePhotoArchivingOutcome Archive(
        SitePhotoToArchive photo, SitePhotoEntity? row, ArchiveSitePhotos command, DateTimeOffset now)
    {
        if (row is null)
            return new SitePhotoArchivingOutcome(photo.SitePhotoId, SitePhotoArchiving.NotFound, "No pool photo has this id.");
        if (row.FiledToProgressUpdateId is not null)
            return new SitePhotoArchivingOutcome(photo.SitePhotoId, SitePhotoArchiving.AlreadyFiled,
                $"Already filed onto progress update {row.FiledToProgressUpdateId} — take it off the update instead.");
        if (row.ArchivedAt is not null)
            return new SitePhotoArchivingOutcome(photo.SitePhotoId, SitePhotoArchiving.AlreadyArchived, "Already archived.");

        row.ArchivedAt = now;
        row.ArchiveReason = photo.Reason;
        row.ArchiveNote = photo.Note.Trim();
        row.ArchivedForProjectId = command.ProjectId;
        row.ArchivedForPeriodEnd = command.PeriodEnd;
        row.ArchivedByEmail = command.ArchivedByEmail;
        return new SitePhotoArchivingOutcome(photo.SitePhotoId, SitePhotoArchiving.Archived, "Archived.");
    }
}
