using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Inventory;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Inventory.Commands;

public sealed class AddInventoryItemHandler : ICommandHandler<AddInventoryItem, InventoryItem>
{
    private readonly JpmsContext context;
    public AddInventoryItemHandler(JpmsContext context) { this.context = context; }

    public async Task<InventoryItem> HandleAsync(AddInventoryItem command, CancellationToken cancellationToken)
    {
        // Global sequence (like defect numbers): max + 1, never a row count — deleted rows must
        // not re-issue a number, because the number is the mailbox tag stem ("JPMS/INV-0001").
        var nextNumber = (await context.InventoryItems.MaxAsync(item => (int?)item.Number, cancellationToken) ?? 0) + 1;

        var entity = new InventoryItemEntity
        {
            InventoryItemId = Guid.NewGuid().ToString("N"),
            ProjectId = command.ProjectId,
            Number = nextNumber,
            ProductName = command.ProductName,
            ProductDetails = command.ProductDetails,
            Location = command.Location,
            LocationDetails = command.LocationDetails,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.InventoryItems.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
