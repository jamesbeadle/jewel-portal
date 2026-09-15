using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Hs.Audits;

internal static class HsAuditEntityMapping
{
    public static HsAudit ToModel(this HsAuditEntity entity) => new(
        entity.HsAuditId,
        entity.ProjectId,
        entity.Number,
        entity.Reference,
        (HsAuditStatus)entity.Status,
        (HsAuditType)entity.Type,
        entity.InspectionDate,
        entity.SiteManagerName,
        entity.SafetyOfficerName,
        entity.SummaryOfWorkActivities,
        entity.SiteOperativeCount,
        entity.FurtherComments,
        entity.Score,
        entity.PreviousScore,
        entity.TemplateVersion,
        entity.ManagerName,
        entity.IssuedAt,
        entity.ClosedAt,
        entity.CreatedByEmail,
        entity.CreatedAt);

    public static HsAuditItem ToModel(this HsAuditItemEntity entity) => new(
        entity.HsAuditItemId,
        entity.HsAuditId,
        entity.Code,
        entity.Section,
        entity.Name,
        entity.Comment is { } comment ? (HsAuditComment)comment : null,
        entity.Rate is { } rate ? (HsAuditRate)rate : null,
        entity.Class is { } hsAuditClass ? (HsAuditClass)hsAuditClass : null,
        entity.Minus,
        entity.TimeScale is { } timeScale ? (HsAuditTimeScale)timeScale : null,
        entity.Findings,
        entity.OwnerName,
        entity.DateRectified,
        entity.HsRecordId);
}
