using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.CostCenters;

namespace Jewel.JPMS.Api.Features.CostCenters.Commands;

public sealed class AddCostCenterValidation
{
    public ValidationOutcome Check(AddCostCenter command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.Code)) errors.Add("A code is required.");
        else if (command.Code.Trim().Length > 32) errors.Add("The code must be 32 characters or fewer.");
        if (string.IsNullOrWhiteSpace(command.Name)) errors.Add("A name is required.");
        else if (command.Name.Trim().Length > 256) errors.Add("The name must be 256 characters or fewer.");
        if (command.SortOrder < 0) errors.Add("The sort order cannot be negative.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
