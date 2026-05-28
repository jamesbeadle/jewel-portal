using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Procurement;

internal static class ProcurementEntityMapping
{
    public static BidPackage ToModel(this BidPackageEntity entity) =>
        new(entity.BidPackageId, entity.ProjectId, entity.Title, entity.Trade, (BidPackageStatus)entity.Status, entity.CreatedAt, entity.OwnerEmail);

    public static Quote ToModel(this QuoteEntity entity) =>
        new(entity.QuoteId, entity.BidPackageId, entity.SubcontractorId, entity.Value, entity.Notes, entity.ReceivedAt, entity.IsDeclined);

    public static WorkOrder ToModel(this WorkOrderEntity entity) =>
        new(entity.WorkOrderId, entity.ProjectId, entity.BidPackageId, entity.SubcontractorId, entity.Value, entity.Scope, entity.AwardedAt, entity.AwardedByEmail);
}
