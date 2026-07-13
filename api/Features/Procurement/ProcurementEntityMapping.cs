using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Procurement;

internal static class ProcurementEntityMapping
{
    public static BidPackage ToModel(this BidPackageEntity entity) =>
        new(entity.BidPackageId, entity.ProjectId, entity.Title, entity.Trade, (BidPackageStatus)entity.Status, entity.CreatedAt, entity.OwnerEmail, entity.VariationOrderQuoteId, entity.Number);

    public static BidPackageRecipient ToModel(this BidPackageRecipientEntity entity) =>
        new(entity.RecipientId, entity.BidPackageId, entity.SubcontractorId, (BidPackageRecipientStatus)entity.Status, entity.InvitedAt, entity.RespondedAt);

    public static BidPackageLineItem ToModel(this BidPackageLineItemEntity entity) =>
        new(entity.LineItemId, entity.BidPackageId, entity.Description, entity.Unit, entity.Quantity, entity.Trade, entity.SortOrder,
            (BidPackageLineCoverage)entity.Coverage, entity.BoqLineItemId, entity.VariationOrderQuoteId);

    public static Quote ToModel(this QuoteEntity entity) =>
        new(entity.QuoteId, entity.BidPackageId, entity.SubcontractorId, entity.Value, entity.Notes, entity.ReceivedAt, entity.IsDeclined);

    public static QuoteLineItem ToModel(this QuoteLineItemEntity entity) =>
        new(entity.QuoteLineItemId, entity.QuoteId, entity.BidPackageLineItemId, entity.Description,
            entity.Unit, entity.Quantity, entity.Rate, entity.Total);

    public static WorkOrder ToModel(this WorkOrderEntity entity) =>
        new(entity.WorkOrderId, entity.ProjectId, entity.BidPackageId, entity.SubcontractorId, entity.Value, entity.Scope, entity.AwardedAt, entity.AwardedByEmail,
            entity.Number, entity.Title, (WorkOrderStatus)entity.Status, entity.CreatedAt, entity.ScheduledCompletion, entity.VariationOrderId);

    public static WorkOrderLine ToModel(this WorkOrderLineEntity entity) =>
        new(entity.WorkOrderLineId, entity.WorkOrderId, entity.Title, entity.Description, entity.CostType, entity.CostCode,
            entity.Quantity, entity.Unit, entity.UnitCost, entity.LineTotal, entity.PaidToDate, entity.SortOrder);
}
