using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class ReturnVoqToTenderingValidation
{
    public ValidationOutcome Check(ReturnVoqToTendering command)
    {
        if (string.IsNullOrWhiteSpace(command.VariationOrderQuoteId))
            return new ValidationOutcome(new[] { "VariationOrderQuoteId is required." });
        return ValidationOutcome.Passed;
    }
}
