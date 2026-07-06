using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Site;

internal static class SiteEntityMapping
{
    public static SiteReport ToModel(this SiteReportEntity entity) =>
        new(entity.SiteReportId, entity.ProjectId, entity.PeriodEnd, entity.Narrative, entity.AttendanceDays, entity.OpenSnags, entity.ProgressPercent, entity.IsIssued);

    public static ProgrammeTask ToModel(this ProgrammeTaskEntity entity) =>
        new(entity.ProgrammeTaskId, entity.ProjectId, entity.Title, entity.PlannedStart, entity.PlannedEnd, entity.ProgressPercent, entity.BoqLineItemId);

    public static ProgrammeTaskLink ToModel(this ProgrammeTaskLinkEntity entity) =>
        new(entity.ProgrammeTaskLinkId, entity.ProjectId, entity.PredecessorTaskId, entity.SuccessorTaskId, entity.LagDays);

    public static ProgrammeBaseline ToModel(this ProgrammeBaselineEntity entity) =>
        new(entity.ProgrammeBaselineId, entity.ProjectId, entity.Label, entity.TakenByEmail, entity.TakenAt);

    public static ProgrammeBaselineTask ToModel(this ProgrammeBaselineTaskEntity entity) =>
        new(entity.ProgrammeBaselineTaskId, entity.ProgrammeBaselineId, entity.ProgrammeTaskId, entity.Title, entity.PlannedStart, entity.PlannedEnd);
}
