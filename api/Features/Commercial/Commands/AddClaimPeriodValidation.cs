using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class AddClaimPeriodValidation
{
    public ValidationOutcome Check(AddClaimPeriod command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (command.PeriodNumber <= 0) errors.Add("Period number must be positive.");
        if (command.EndDate < command.StartDate) errors.Add("End date cannot be before start date.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
