using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class SetCostCentreCostCompletionValidation
{
    public ValidationOutcome Check(SetCostCentreCostCompletion command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.CostCode)) errors.Add("Cost code is required.");
        if (command.CostCompletionPercent < 0m || command.CostCompletionPercent > 100m) errors.Add("Cost completion must be between 0 and 100.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
