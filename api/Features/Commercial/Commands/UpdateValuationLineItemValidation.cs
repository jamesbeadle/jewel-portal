using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class UpdateValuationLineItemValidation
{
    public ValidationOutcome Check(UpdateValuationLineItem command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ValuationLineItemId)) errors.Add("ValuationLineItemId is required.");
        if (string.IsNullOrWhiteSpace(command.Description)) errors.Add("Description is required.");
        if (command.ElementType == ValuationElementType.Variation && string.IsNullOrWhiteSpace(command.VariationRef))
            errors.Add("Variation lines require a variation reference (e.g. V18).");
        if ((command.ClientReference ?? "").Trim().Length > SetClientCostReferencesValidation.MaximumReferenceLength)
            errors.Add($"The client reference is longer than {SetClientCostReferencesValidation.MaximumReferenceLength} characters.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
