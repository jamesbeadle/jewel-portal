using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class SubmitTimesheetValidation
{
    public ValidationOutcome Check(SubmitTimesheet command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.PersonEmail)) errors.Add("Person email is required.");
        if (string.IsNullOrWhiteSpace(command.CostCode)) errors.Add("Cost code is required.");
        if (command.Hours <= 0) errors.Add("Hours must be positive.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
