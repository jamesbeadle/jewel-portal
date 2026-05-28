using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class AddProgrammeTaskValidation
{
    public ValidationOutcome Check(AddProgrammeTask command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("Title is required.");
        if (command.PlannedEnd < command.PlannedStart) errors.Add("Planned end must be on or after planned start.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
