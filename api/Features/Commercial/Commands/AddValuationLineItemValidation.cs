using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class AddValuationLineItemValidation
{
    public ValidationOutcome Check(AddValuationLineItem command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.Description)) errors.Add("Description is required.");
        if (command.ElementType == ValuationElementType.Variation && string.IsNullOrWhiteSpace(command.VariationRef))
            errors.Add("Variation lines require a variation reference (e.g. V18).");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
