using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Inventory;

internal static class InventoryEntityMapping
{
    public static InventoryItem ToModel(this InventoryItemEntity entity) =>
        new(entity.InventoryItemId, entity.ProjectId, entity.ProductName, entity.ProductDetails,
            entity.Location, entity.LocationDetails, entity.CreatedAt, entity.Reference);
}
