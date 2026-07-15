using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class AddBidPackageLineItemsValidation
{
    public ValidationOutcome Check(AddBidPackageLineItems command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.BidPackageId)) errors.Add("BidPackageId is required.");
        if (command.LineItems is null || command.LineItems.Count == 0)
        {
            errors.Add("At least one line item is required.");
        }
        else
        {
            foreach (var item in command.LineItems)
            {
                if (string.IsNullOrWhiteSpace(item.Description)) { errors.Add("Each line item needs a description."); break; }
            }
            foreach (var item in command.LineItems)
            {
                if (string.IsNullOrWhiteSpace(item.CostCode)) { errors.Add("Each line item needs a cost code."); break; }
            }
        }
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
