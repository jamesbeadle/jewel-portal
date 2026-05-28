using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class AssembleSiteReportValidation
{
    public ValidationOutcome Check(AssembleSiteReport command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.Narrative)) errors.Add("Narrative is required.");
        if (command.AttendanceDays < 0) errors.Add("Attendance days cannot be negative.");
        if (command.ProgressPercent < 0 || command.ProgressPercent > 100) errors.Add("Progress must be between 0 and 100.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
