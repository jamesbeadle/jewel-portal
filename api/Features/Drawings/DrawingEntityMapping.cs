using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Drawings;

internal static class DrawingEntityMapping
{
    public static Drawing ToModel(this DrawingEntity entity, int unapprovedCount = 0, int archivedCount = 0) =>
        new(entity.DrawingId, entity.ProjectId, entity.DrawingCode, entity.Title,
            string.IsNullOrEmpty(entity.CurrentApprovedRevisionLabel) ? null : entity.CurrentApprovedRevisionLabel,
            entity.CreatedAt, unapprovedCount, archivedCount);

    public static DrawingRevision ToModel(this DrawingRevisionEntity entity) =>
        new(entity.DrawingRevisionId, entity.DrawingId, entity.RevisionLabel, entity.FileName, entity.IssuedByEmail,
            entity.ReceivedAt, entity.SupersededAt, entity.IsAmbiguous, entity.ViewCount,
            (DrawingApprovalStatus)entity.ApprovalStatus, entity.BlobRef, entity.ContentType, entity.FileSizeBytes,
            entity.ApprovedByEmail, entity.ApprovedAt);
}
