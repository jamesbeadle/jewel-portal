using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.CommercialInputs;

internal static class CommercialInputsEntityMapping
{
    public static Daywork ToModel(this DayworkEntity entity) => new(
        DayworkId: entity.DayworkId,
        ProjectId: entity.ProjectId,
        WorkedOn: entity.WorkedOn,
        SubcontractorReference: entity.SubcontractorReference,
        Description: entity.Description,
        InstructedBy: entity.InstructedBy,
        Hours: entity.Hours,
        HourlyRate: entity.HourlyRate,
        LabourCost: entity.LabourCost,
        PlantCost: entity.PlantCost,
        MaterialsCost: entity.MaterialsCost,
        UpliftPercent: entity.UpliftPercent,
        ChargeableAmount: entity.ChargeableAmount);

    public static ContraCharge ToModel(this ContraChargeEntity entity) => new(
        ContraChargeId: entity.ContraChargeId,
        ProjectId: entity.ProjectId,
        SubcontractorReference: entity.SubcontractorReference,
        RaisedOn: entity.RaisedOn,
        Description: entity.Description,
        Category: entity.Category,
        Amount: entity.Amount,
        Status: entity.Status,
        RecoveredAmount: entity.RecoveredAmount);

    public static SubcontractorRetention ToModel(this SubcontractorRetentionEntity entity) => new(
        SubcontractorRetentionId: entity.SubcontractorRetentionId,
        ProjectId: entity.ProjectId,
        SubcontractorReference: entity.SubcontractorReference,
        CertifiedAmount: entity.CertifiedAmount,
        RetentionPercent: entity.RetentionPercent,
        FirstReleasedAmount: entity.FirstReleasedAmount,
        FinalReleasedAmount: entity.FinalReleasedAmount);
}
