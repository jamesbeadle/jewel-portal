using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SetBidPackageLineItemsValidation
{
    public ValidationOutcome Check(SetBidPackageLineItems command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.BidPackageId)) errors.Add("BidPackageId is required.");
        if (command.LineItems is null)
        {
            errors.Add("Line items are required.");
        }
        else
        {
            foreach (var item in command.LineItems)
            {
                if (string.IsNullOrWhiteSpace(item.Description)) { errors.Add("Each line item needs a description."); break; }
            }
        }
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
