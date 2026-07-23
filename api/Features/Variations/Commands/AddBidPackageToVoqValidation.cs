using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class AddBidPackageToVoqValidation
{
    public ValidationOutcome Check(AddBidPackageToVoq command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.VariationOrderId)) errors.Add("VariationOrderId is required.");
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("A package title is required.");
        if (string.IsNullOrWhiteSpace(command.Trade)) errors.Add("A trade is required.");
        if (string.IsNullOrWhiteSpace(command.OwnerEmail)) errors.Add("Owner email is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
