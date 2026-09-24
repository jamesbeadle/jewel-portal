using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Progress.SitePhotos;

internal static class SitePhotoEntityMapping
{
    public static SitePhoto ToModel(this SitePhotoEntity entity) =>
        new(entity.SitePhotoId, entity.FileName, entity.ContentType, entity.FileSizeBytes, entity.ContentHash,
            entity.UploadedByEmail, entity.UploadedAt,
            entity.FiledToProjectId, entity.FiledToProgressUpdateId, entity.FiledAt,
            ArchiveOf(entity));

    private static SitePhotoArchive? ArchiveOf(SitePhotoEntity entity)
    {
        if (entity.ArchivedAt is not { } archivedAt) return null;
        var reason = entity.ArchiveReason ?? SitePhotoArchiveReason.Other;
        return new SitePhotoArchive(reason, entity.ArchiveNote, entity.ArchivedForProjectId,
            entity.ArchivedForPeriodEnd, entity.ArchivedByEmail, archivedAt);
    }
}
