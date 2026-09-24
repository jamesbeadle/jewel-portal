using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class ReinstateVariationOrderValidation
{
    public ValidationOutcome Check(ReinstateVariationOrder command)
    {
        if (string.IsNullOrWhiteSpace(command.VariationOrderId))
            return new ValidationOutcome(new[] { "VariationOrderId is required." });
        return ValidationOutcome.Passed;
    }
}
