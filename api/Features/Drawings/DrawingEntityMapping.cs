using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Drawings;

internal static class DrawingEntityMapping
{
    public static Drawing ToModel(this DrawingEntity entity) =>
        new(entity.DrawingId, entity.ProjectId, entity.DrawingCode, entity.Title, entity.CurrentRevision, entity.CreatedAt);

    public static DrawingRevision ToModel(this DrawingRevisionEntity entity) =>
        new(entity.DrawingRevisionId, entity.DrawingId, entity.RevisionLabel, entity.FileName, entity.IssuedByEmail, entity.ReceivedAt, entity.SupersededAt, entity.IsAmbiguous, entity.ViewCount);
}
