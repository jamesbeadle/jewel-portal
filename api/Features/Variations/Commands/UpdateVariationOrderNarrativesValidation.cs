using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class UpdateVariationOrderNarrativesValidation
{
    public ValidationOutcome Check(UpdateVariationOrderNarratives command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.VariationOrderId)) errors.Add("VariationOrderId is required.");
        // Blank sections are valid — submitting nothing clears a section. Overlong prose is
        // clamped by the handler through the shared narrative rule rather than refused here.
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
