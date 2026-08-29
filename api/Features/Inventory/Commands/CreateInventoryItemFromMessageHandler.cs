using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Inventory;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Inventory.Commands;

// Adds the inventory item from a tagged email and links the email to it. The item is created by
// the SAME handler as a manually added one (numbering — one set of rules, whichever door the item
// came in through), then the originating email is tagged to it through the shared record-link
// path, exactly like CreateDefectFromMessage / CreateWorkOrderFromMessage. The item is persisted
// first because the link path resolves the record from the database; a link failure therefore
// throws with the item already saved — same trade-off as the other from-message commands, and the
// email stays in the queue to retry against the existing item.
public sealed class CreateInventoryItemFromMessageHandler
    : ICommandHandler<CreateInventoryItemFromMessage, InventoryItem>
{
    private readonly ICommandHandler<AddInventoryItem, InventoryItem> addItem;
    private readonly ICommandHandler<LinkMessageToRecord, Acknowledgement> link;

    public CreateInventoryItemFromMessageHandler(
        ICommandHandler<AddInventoryItem, InventoryItem> addItem,
        ICommandHandler<LinkMessageToRecord, Acknowledgement> link)
    { this.addItem = addItem; this.link = link; }

    public async Task<InventoryItem> HandleAsync(CreateInventoryItemFromMessage command, CancellationToken cancellationToken)
    {
        var item = await addItem.HandleAsync(
            new AddInventoryItem(
                command.ProjectId,
                command.ProductName,
                command.ProductDetails,
                command.Location,
                command.LocationDetails),
            cancellationToken);

        // Tag the originating email to the new item through the shared record-link path (verified
        // by read-back inside the handler). Throws if the email can't be read/tagged.
        await link.HandleAsync(
            new LinkMessageToRecord(
                command.MessageId, RecordType.Inventory, item.InventoryItemId, command.InternetMessageId,
                AllowCrossPathway: command.AllowCrossPathway,
                Scope: command.Scope),
            cancellationToken);

        return item;
    }
}
