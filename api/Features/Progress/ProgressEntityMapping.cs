using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Progress;

internal static class ProgressEntityMapping
{
    public static ProgressUpdate ToModel(this ProgressUpdateEntity entity, IReadOnlyList<ProgressPhoto> photos) =>
        new(entity.ProgressUpdateId, entity.ProjectId, entity.Title, entity.Description,
            entity.WorkDate, entity.CreatedByEmail, entity.CreatedAt, photos);

    public static ProgressPhoto ToModel(this ProgressPhotoEntity entity) =>
        new(entity.ProgressPhotoId, entity.ProgressUpdateId, entity.FileName, entity.ContentType,
            entity.FileSizeBytes, entity.SortOrder, entity.UploadedByEmail, entity.UploadedAt);

    public static ProgressReport ToModel(this ProgressReportEntity entity, IReadOnlyList<string> selectedUpdateIds) =>
        new(entity.ProgressReportId, entity.ProjectId, entity.Title, entity.PeriodStart, entity.PeriodEnd,
            entity.Introduction, entity.WorkCompleted, entity.UpcomingWorks,
            entity.CreatedByEmail, entity.CreatedAt, selectedUpdateIds);
}
