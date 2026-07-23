using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class SetVoqStatusValidation
{
    public ValidationOutcome Check(SetVoqStatus command)
    {
        if (string.IsNullOrWhiteSpace(command.VariationOrderQuoteId))
            return new ValidationOutcome(new[] { "VariationOrderQuoteId is required." });
        if (!Enum.IsDefined(command.Status))
            return new ValidationOutcome(new[] { "Status is not a recognised VOQ status." });
        return ValidationOutcome.Passed;
    }
}
