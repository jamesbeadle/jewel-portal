using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class CreateCostCentreGroupValidation
{
    public ValidationOutcome Check(CreateCostCentreGroup command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.Name)) errors.Add("A group name is required.");
        var distinctCodes = command.CostCodes
            .Select(costCode => costCode.Trim())
            .Where(costCode => costCode.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        if (distinctCodes < 2) errors.Add("A group needs at least two cost centres.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
