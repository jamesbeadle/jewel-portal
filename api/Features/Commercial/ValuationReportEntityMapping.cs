using Jewel.JPMS.Api.Data.Entities;

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
            entity.Comments, entity.DisplayOrder, entity.ClientReference);

    public static ValuationClaim ToModel(this ValuationClaimEntity entity) =>
        new(entity.ValuationClaimId, entity.ProjectId, entity.ClaimNumber, entity.ClaimDate,
            (ValuationClaimStatus)entity.Status,
            entity.RetentionPercent, entity.RetentionReleasePercent,
            entity.PreapprovedAt, entity.ConfirmedAt,
            entity.ContractSum, entity.NetVariations, entity.RevisedContractSum,
            entity.TotalWorksComplete, entity.RetentionHeld, entity.RetentionReleased,
            entity.CertifiedToDate, entity.PaymentDueExVat, entity.Name,
            entity.DepositPercent, entity.DepositReleased, entity.DepositReleasedOpening,
            entity.LockedAt);

    public static ClaimLine ToModel(this ClaimLineEntity entity) =>
        new(entity.ClaimLineId, entity.ValuationClaimId, entity.ValuationLineItemId,
            entity.PercentComplete, entity.CumulativeClaimed, entity.PeriodIncrement);

    // A locked claim's own row as one line of its statement — the frozen copy of the bill line
    // beside the money claimed. Only meaningful once the row was frozen (DisplayOrder >= 0).
    public static ValuationStatementLine ToStatementLine(this ClaimLineEntity entity) =>
        new(entity.ValuationClaimId, entity.ValuationLineItemId,
            (ValuationElementType)entity.ElementType,
            entity.SectionCode, entity.SectionName,
            entity.VariationRef, entity.VariationTitle,
            (ValuationLineType)entity.LineType,
            entity.CostCode, entity.Description, entity.Unit,
            entity.Quantity, entity.Rate, entity.LineAmount,
            entity.PercentComplete, entity.CumulativeClaimed, entity.PeriodIncrement,
            entity.Comments, entity.DisplayOrder, entity.ClientReference);
}
