using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Progress.SitePhotos;

internal static class SitePhotoEntityMapping
{
    public static SitePhoto ToModel(this SitePhotoEntity entity) =>
        new(entity.SitePhotoId, entity.FileName, entity.ContentType, entity.FileSizeBytes, entity.ContentHash,
            entity.UploadedByEmail, entity.UploadedAt,
            entity.FiledToProjectId, entity.FiledToProgressUpdateId, entity.FiledAt);
}
