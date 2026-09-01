using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Inventory;

namespace Jewel.JPMS.Api.Features.Inventory.Commands;

public sealed class UpdateInventoryItemHandler : ICommandHandler<UpdateInventoryItem, InventoryItem>
{
    private readonly JpmsContext context;
    public UpdateInventoryItemHandler(JpmsContext context) { this.context = context; }

    public async Task<InventoryItem> HandleAsync(UpdateInventoryItem command, CancellationToken cancellationToken)
    {
        var entity = await context.InventoryItems.FindAsync(new object[] { command.InventoryItemId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Inventory item {command.InventoryItemId} not found.");
        entity.ProductName = command.ProductName;
        entity.ProductDetails = command.ProductDetails;
        entity.Location = command.Location;
        entity.LocationDetails = command.LocationDetails;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
