using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Variations;

internal static class VariationsEntityMapping
{
    public static VariationOrder ToModel(this VariationOrderEntity entity) => new(
        VariationOrderId: entity.VariationOrderId,
        ProjectId: entity.ProjectId,
        RequestId: entity.RequestId,
        Number: entity.Number,
        Reference: entity.Reference,
        Title: entity.Title,
        Description: entity.Description,
        Status: (VariationOrderStatus)entity.Status,
        SelectedBidPackageId: entity.SelectedBidPackageId,
        SelectedSubcontractorId: entity.SelectedSubcontractorId,
        EstimatedValue: entity.EstimatedValue,
        VariationRef: entity.VariationRef,
        Value: entity.Value,
        CostCode: entity.CostCode,
        CreatedAt: entity.CreatedAt,
        CreatedByEmail: entity.CreatedByEmail,
        IssuedAt: entity.IssuedAt,
        ApprovedAt: entity.ApprovedAt,
        ApprovedByEmail: entity.ApprovedByEmail,
        RejectedAt: entity.RejectedAt);

    public static BidPackage ToModel(this BidPackageEntity entity) => new(
        entity.BidPackageId, entity.ProjectId, entity.Title, entity.Trade,
        (BidPackageStatus)entity.Status, entity.CreatedAt, entity.OwnerEmail, entity.VariationOrderId, entity.Number);

    public static SubcontractorVariationRequest ToModel(
        this SubcontractorVariationRequestEntity entity,
        string projectName = "", int workOrderNumber = 0, string subcontractorName = "") => new(
        entity.VariationRequestId,
        entity.ProjectId,
        entity.WorkOrderId,
        entity.SubcontractorId,
        entity.Title,
        entity.Description,
        entity.ProposedValue,
        (VariationRequestStatus)entity.Status,
        entity.SubmittedAt,
        entity.ReviewedAt,
        entity.ReviewedByEmail,
        entity.RejectionReason,
        entity.VariationOrderId,
        projectName,
        workOrderNumber,
        subcontractorName);
}
