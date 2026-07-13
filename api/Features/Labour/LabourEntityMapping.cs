using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Labour;

internal static class LabourEntityMapping
{
    public static Worker ToModel(this WorkerEntity entity) =>
        new(entity.WorkerId, entity.Name, entity.SubcontractorId, entity.HourlyRate,
            entity.IsActive, entity.ContactEmail, entity.ContactPhone);

    public static ProjectWorkerAssignment ToModel(this ProjectWorkerAssignmentEntity entity, string workerName) =>
        new(entity.ProjectWorkerAssignmentId, entity.ProjectId, entity.WorkerId, workerName, entity.IsActive);

    public static SiteAttendance ToModel(this SiteAttendanceEntity entity, string workerName) =>
        new(entity.SiteAttendanceId, entity.ProjectId, entity.WorkerId, workerName,
            entity.WorkDate, entity.SignedInAt, entity.SignedOutAt);

    /// <summary>Full detail for commercial roles. Use <see cref="ToModelWithoutMoney"/> for
    /// callers who may not see rates.</summary>
    public static TimesheetDetail ToDetail(this TimesheetEntity entity, string workerName) =>
        new(entity.TimesheetId, entity.ProjectId, entity.WorkerId, workerName, entity.WorkedOn,
            entity.Hours, entity.CostCode, (TimesheetStatus)entity.Status, entity.RateApplied,
            entity.CostAmount, entity.ApprovedByEmail, entity.ApprovedAt, entity.RejectionReason);

    public static TimesheetDetail ToModelWithoutMoney(this TimesheetEntity entity, string workerName) =>
        entity.ToDetail(workerName) with { RateApplied = 0m, CostAmount = 0m };

    public static WorkerTimesheetView ToWorkerView(this TimesheetEntity entity) =>
        new(entity.TimesheetId, entity.WorkedOn, entity.Hours, entity.CostCode,
            (TimesheetStatus)entity.Status, entity.RejectionReason);

    public static LabourSettlementVariance ToModel(this LabourSettlementVarianceEntity entity) =>
        new(entity.LabourSettlementVarianceId, entity.ProjectId, entity.CostCode,
            entity.SubcontractorId, entity.Amount, entity.Reason, entity.XeroLedgerLineId,
            entity.CreatedByEmail, entity.CreatedAt);
}
