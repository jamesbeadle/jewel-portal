using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class ApproveVariationOrderQuoteValidation
{
    public ValidationOutcome Check(ApproveVariationOrderQuote command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.VariationOrderQuoteId)) errors.Add("VariationOrderQuoteId is required.");
        if (string.IsNullOrWhiteSpace(command.CostCode)) errors.Add("A cost code is required to commit the variation value.");
        if (string.IsNullOrWhiteSpace(command.ApprovedByEmail)) errors.Add("Approving email is required.");
        // Negative values are legitimate — an omit variation reduces the contract sum — but a
        // zero value approves nothing.
        if (command.Value is 0) errors.Add("Value cannot be zero. Enter the agreed value (negative for an omit).");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
