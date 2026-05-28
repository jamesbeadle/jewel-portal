using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Site;

internal static class SiteEntityMapping
{
    public static SiteReport ToModel(this SiteReportEntity entity) =>
        new(entity.SiteReportId, entity.ProjectId, entity.PeriodEnd, entity.Narrative, entity.AttendanceDays, entity.OpenSnags, entity.ProgressPercent, entity.IsIssued);

    public static ProgrammeTask ToModel(this ProgrammeTaskEntity entity) =>
        new(entity.ProgrammeTaskId, entity.ProjectId, entity.Title, entity.PlannedStart, entity.PlannedEnd, entity.ProgressPercent, entity.BoqLineItemId);
}
