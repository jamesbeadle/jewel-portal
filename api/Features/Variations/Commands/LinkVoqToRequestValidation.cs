using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class LinkVoqToRequestValidation
{
    public ValidationOutcome Check(LinkVoqToRequest command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.VariationOrderQuoteId)) errors.Add("VariationOrderQuoteId is required.");
        if (string.IsNullOrWhiteSpace(command.RequestId)) errors.Add("RequestId is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
