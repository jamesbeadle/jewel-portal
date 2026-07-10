using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial;

internal static class ValuationReportEntityMapping
{
    public static ValuationLineItem ToModel(this ValuationLineItemEntity entity) =>
        new(entity.ValuationLineItemId, entity.ProjectId,
            (ValuationElementType)entity.ElementType,
            entity.SectionCode, entity.SectionName,
            entity.VariationRef, entity.VariationTitle,
            (ValuationLineType)entity.LineType,
            entity.CostCode, entity.Description, entity.Unit,
            entity.Quantity, entity.Rate, entity.LineAmount,
            entity.Comments, entity.DisplayOrder);

    public static ValuationClaim ToModel(this ValuationClaimEntity entity) =>
        new(entity.ValuationClaimId, entity.ProjectId, entity.ClaimNumber, entity.ClaimDate,
            (ValuationClaimStatus)entity.Status,
            entity.RetentionPercent, entity.RetentionReleasePercent,
            entity.PreapprovedAt, entity.ConfirmedAt,
            entity.ContractSum, entity.NetVariations, entity.RevisedContractSum,
            entity.TotalWorksComplete, entity.RetentionHeld, entity.RetentionReleased,
            entity.CertifiedToDate, entity.PaymentDueExVat);

    public static ClaimLine ToModel(this ClaimLineEntity entity) =>
        new(entity.ClaimLineId, entity.ValuationClaimId, entity.ValuationLineItemId,
            entity.PercentComplete, entity.CumulativeClaimed, entity.PeriodIncrement);

    public static ValuationReportSnapshot ToModel(this ValuationReportSnapshotEntity entity) =>
        new(entity.ValuationReportSnapshotId, entity.ProjectId,
            entity.ValuationInvoiceId, entity.ValuationClaimId,
            entity.Label, entity.TakenAt, entity.IsSuperseded,
            entity.ContractSum, entity.NetVariations, entity.RevisedContractSum,
            entity.TotalWorksComplete,
            entity.RetentionPercent, entity.RetentionHeld,
            entity.RetentionReleasePercent, entity.RetentionReleased,
            entity.CertifiedToDate, entity.PaymentDueExVat);

    public static ValuationReportSnapshotLine ToModel(this ValuationReportSnapshotLineEntity entity) =>
        new(entity.ValuationReportSnapshotLineId, entity.ValuationReportSnapshotId,
            entity.SourceValuationLineItemId,
            (ValuationElementType)entity.ElementType,
            entity.SectionCode, entity.SectionName,
            entity.VariationRef, entity.VariationTitle,
            (ValuationLineType)entity.LineType,
            entity.CostCode, entity.Description, entity.Unit,
            entity.Quantity, entity.Rate, entity.LineAmount,
            entity.PercentComplete, entity.CumulativeClaimed, entity.PeriodIncrement,
            entity.Comments, entity.DisplayOrder);
}
