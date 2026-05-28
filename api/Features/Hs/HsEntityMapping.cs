using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Hs;

internal static class HsEntityMapping
{
    public static HsRecord ToModel(this HsRecordEntity entity) => new(
        entity.HsRecordId, entity.ProjectId, (HsRecordKind)entity.Kind, entity.Summary,
        (HsSeverity)entity.Severity, (HsStatus)entity.Status, entity.AssignedToEmail,
        entity.RaisedAt, entity.DueAt, entity.ClosedAt);

    public static HsRecordAttendance ToModel(this HsRecordAttendanceEntity entity) =>
        new(entity.HsRecordAttendanceId, entity.HsRecordId, entity.AttendeeName, entity.SignatureBlobRef, entity.SignedAt);
}
