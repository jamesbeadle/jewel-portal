using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Variations;

internal static class VariationsEntityMapping
{
    public static VariationOrderQuote ToModel(this VariationOrderQuoteEntity entity) => new(
        VariationOrderQuoteId: entity.VariationOrderQuoteId,
        ProjectId: entity.ProjectId,
        RequestId: entity.RequestId,
        Number: entity.Number,
        Reference: entity.Reference,
        Title: entity.Title,
        Description: entity.Description,
        Status: (VariationOrderQuoteStatus)entity.Status,
        SelectedBidPackageId: entity.SelectedBidPackageId,
        SelectedSubcontractorId: entity.SelectedSubcontractorId,
        EstimatedValue: entity.EstimatedValue,
        CreatedAt: entity.CreatedAt,
        CreatedByEmail: entity.CreatedByEmail,
        ApprovedAt: entity.ApprovedAt,
        ApprovedByEmail: entity.ApprovedByEmail);

    public static BidPackage ToModel(this BidPackageEntity entity) => new(
        entity.BidPackageId, entity.ProjectId, entity.Title, entity.Trade,
        (BidPackageStatus)entity.Status, entity.CreatedAt, entity.OwnerEmail, entity.VariationOrderQuoteId, entity.Number);

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
        entity.VariationOrderQuoteId,
        projectName,
        workOrderNumber,
        subcontractorName);

    public static VariationOrder ToModel(this VariationOrderEntity entity) => new(
        VariationOrderId: entity.VariationOrderId,
        ProjectId: entity.ProjectId,
        VariationOrderQuoteId: entity.VariationOrderQuoteId,
        RequestId: entity.RequestId,
        Number: entity.Number,
        VariationRef: entity.VariationRef,
        Title: entity.Title,
        Description: entity.Description,
        Status: (VariationOrderStatus)entity.Status,
        Value: entity.Value,
        SubcontractorId: entity.SubcontractorId,
        CostCode: entity.CostCode,
        ApprovedAt: entity.ApprovedAt,
        ApprovedByEmail: entity.ApprovedByEmail,
        IssuedAt: entity.IssuedAt,
        CancelledAt: entity.CancelledAt);
}
