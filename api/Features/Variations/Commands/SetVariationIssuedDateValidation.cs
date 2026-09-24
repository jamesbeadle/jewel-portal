using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class SetVariationIssuedDateValidation
{
    public ValidationOutcome Check(SetVariationIssuedDate command)
    {
        var errors = new List<string>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (string.IsNullOrWhiteSpace(command.VariationOrderId)) errors.Add("VariationOrderId is required.");
        if (command.IssuedOn > today) errors.Add("The date issued cannot be in the future.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
