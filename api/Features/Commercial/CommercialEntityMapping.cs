using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial;

internal static class CommercialEntityMapping
{
    public static Valuation ToModel(this ValuationEntity entity) =>
        new(entity.ValuationId, entity.ClaimPeriodId, entity.ProjectId, entity.GrossValue, entity.RetentionPercent, entity.NetValue, entity.IsIssued, entity.IssuedAt);

    public static CostCodeBudget ToModel(this CostCodeBudgetEntity entity) =>
        new(entity.CostCodeBudgetId, entity.ProjectId, entity.CostCode, entity.AllocatedAmount, entity.SpentAmount, entity.CommittedAmount);

    public static CostCentreCostProgress ToModel(this CostCentreCostProgressEntity entity) =>
        new(entity.CostCentreCostProgressId, entity.ProjectId, entity.CostCode, entity.CostCompletionPercent);

    public static Timesheet ToModel(this TimesheetEntity entity) =>
        new(entity.TimesheetId, entity.ProjectId, entity.PersonEmail, entity.WorkedOn, entity.Hours, entity.CostCode, entity.IsApproved);

    public static ClaimPeriod ToModel(this ClaimPeriodEntity entity) =>
        new(entity.ClaimPeriodId, entity.ProjectId, entity.PeriodNumber, entity.StartDate, entity.EndDate);
}
