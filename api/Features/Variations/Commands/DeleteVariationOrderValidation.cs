using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class DeleteVariationOrderValidation
{
    public ValidationOutcome Check(DeleteVariationOrder command)
    {
        if (string.IsNullOrWhiteSpace(command.VariationOrderId))
            return new ValidationOutcome(new List<string> { "VariationOrderId is required." });
        return ValidationOutcome.Passed;
    }
}
