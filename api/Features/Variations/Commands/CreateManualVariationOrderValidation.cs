using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class CreateManualVariationOrderValidation
{
    public ValidationOutcome Check(CreateManualVariationOrder command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.CreatedByEmail)) errors.Add("Creating email is required.");
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("A title is required.");
        if (command.Number is <= 0) errors.Add("The variation number must be a positive whole number.");

        // The build-up is the variation: at least one priced line, every line coded to a cost centre,
        // and a total that is actually a figure (negative for an omit is fine; zero prices nothing).
        if (command.Lines is not { Count: > 0 })
            errors.Add("At least one priced line is required — the lines' total is the variation's value.");
        else
        {
            if (command.Lines.Any(line => string.IsNullOrWhiteSpace(line.CostCode)))
                errors.Add("Every line needs a cost centre.");
            if (command.Lines.Sum(line => line.Quantity * line.Rate) == 0m)
                errors.Add("The lines' total can't be zero — enter the agreed values (a negative rate for an omit).");
        }

        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
