using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class StageVariationOrderBuildUpValidation
{
    public const int MaxLines = 200;

    public ValidationOutcome Check(StageVariationOrderBuildUp command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.VariationOrderId)) errors.Add("VariationOrderId is required.");
        if (command.Lines is null) errors.Add("Lines are required (an empty list clears the staged build-up).");
        else
        {
            if (command.Lines.Count > MaxLines) errors.Add($"At most {MaxLines} lines can be staged.");
            if (command.Lines.Any(line => string.IsNullOrWhiteSpace(line.CostCode))) errors.Add("Every staged line needs a cost centre.");
            if (command.Lines.Count > 0 && command.Lines.Sum(line => line.Quantity * line.Rate) == 0m)
                errors.Add("The staged total can't be zero — enter the agreed values (negative rate for an omit).");
        }
        foreach (var (name, value) in new[] { ("Commercial basis", command.CommercialBasis), ("Programme impact", command.ProgrammeImpact), ("Exclusions", command.Exclusions) })
            if (value is { Length: > VariationNarratives.MaxNarrativeChars })
                errors.Add($"{name} is over {VariationNarratives.MaxNarrativeChars:N0} characters.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
