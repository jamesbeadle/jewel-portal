using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Boq;

namespace Jewel.JPMS.Api.Features.Boq.Commands;

public sealed class UpdateBoqLineValidation
{
    public ValidationOutcome Check(UpdateBoqLine command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.BoqLineItemId)) errors.Add("BoqLineItemId is required.");
        if (string.IsNullOrWhiteSpace(command.Description)) errors.Add("Description is required.");
        if (string.IsNullOrWhiteSpace(command.Unit)) errors.Add("Unit is required.");
        if (command.Quantity <= 0) errors.Add("Quantity must be positive.");
        if (command.RateValue < 0) errors.Add("Rate value cannot be negative.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
