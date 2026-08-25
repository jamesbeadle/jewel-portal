using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Drawings;

internal static class DrawingEntityMapping
{
    public static Drawing ToModel(this DrawingEntity entity) => entity.ToModel(DrawingRevisionRollup.None);

    public static Drawing ToModel(this DrawingEntity entity, DrawingRevisionRollup rollup) =>
        new(entity.DrawingId, entity.ProjectId, entity.DrawingCode, entity.Title,
            string.IsNullOrEmpty(entity.CurrentApprovedRevisionLabel) ? null : entity.CurrentApprovedRevisionLabel,
            entity.CreatedAt, rollup.UnapprovedCount, rollup.ArchivedCount,
            rollup.LatestMetadataExtractedAt, rollup.LatestAnalysedAt,
            string.IsNullOrEmpty(entity.DrawingFolderId) ? null : entity.DrawingFolderId,
            rollup.LatestFileName, rollup.HasApprovedRevision);

    public static DrawingFolder ToModel(this DrawingFolderEntity entity) =>
        new(entity.DrawingFolderId, entity.ProjectId, entity.Name, entity.CreatedAt,
            string.IsNullOrEmpty(entity.ParentDrawingFolderId) ? null : entity.ParentDrawingFolderId);

    public static DrawingRevision ToModel(this DrawingRevisionEntity entity) =>
        new(entity.DrawingRevisionId, entity.DrawingId, entity.RevisionLabel, entity.FileName, entity.IssuedByEmail,
            entity.ReceivedAt, entity.SupersededAt, entity.IsAmbiguous, entity.ViewCount,
            (DrawingApprovalStatus)entity.ApprovalStatus, entity.BlobRef, entity.ContentType, entity.FileSizeBytes,
            entity.ApprovedByEmail, entity.ApprovedAt, entity.MetadataExtractedAt, entity.AnalysedAt);
}
