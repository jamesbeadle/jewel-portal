using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class UpdateProgrammeTaskValidation
{
    public ValidationOutcome Check(UpdateProgrammeTask command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProgrammeTaskId)) errors.Add("ProgrammeTaskId is required.");
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("Title is required.");
        if (command.PlannedEnd < command.PlannedStart) errors.Add("Planned end must be on or after planned start.");
        if (command.ProgressPercent < 0 || command.ProgressPercent > 100) errors.Add("Progress must be between 0 and 100.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
