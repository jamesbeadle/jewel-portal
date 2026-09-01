using Jewel.JPMS.Contracts.Inventory;

namespace Jewel.JPMS.Api.Features.Inventory.Commands;

public sealed class UpdateInventoryItemValidation
{
    public ValidationOutcome Check(UpdateInventoryItem command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.InventoryItemId)) errors.Add("InventoryItemId is required.");
        if (string.IsNullOrWhiteSpace(command.ProductName)) errors.Add("ProductName is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
