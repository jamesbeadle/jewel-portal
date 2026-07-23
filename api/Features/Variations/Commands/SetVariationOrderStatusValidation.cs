using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class SetVariationOrderStatusValidation
{
    public ValidationOutcome Check(SetVariationOrderStatus command)
    {
        if (string.IsNullOrWhiteSpace(command.VariationOrderId))
            return new ValidationOutcome(new[] { "VariationOrderId is required." });
        if (!Enum.IsDefined(command.Status))
            return new ValidationOutcome(new[] { "Status is not a recognised variation order status." });
        return ValidationOutcome.Passed;
    }
}
