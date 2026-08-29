using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Inventory;

public sealed record UpdateInventoryItem(
    string InventoryItemId,
    string ProductName,
    string ProductDetails,
    string Location,
    string LocationDetails) : ICommand<InventoryItem>;
