using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class IssueVariationOrderValidation
{
    public ValidationOutcome Check(IssueVariationOrder command)
    {
        if (string.IsNullOrWhiteSpace(command.VariationOrderId))
            return new ValidationOutcome(new[] { "VariationOrderId is required." });
        return ValidationOutcome.Passed;
    }
}
