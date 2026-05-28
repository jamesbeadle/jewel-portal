using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class ApproveTimesheetValidation
{
    public ValidationOutcome Check(ApproveTimesheet command)
    {
        if (string.IsNullOrWhiteSpace(command.TimesheetId)) return ValidationOutcome.Failed("TimesheetId is required.");
        return ValidationOutcome.Passed;
    }
}
