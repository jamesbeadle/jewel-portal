using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Api.Features.Hs.Commands;

public sealed class LogHsRecordValidation
{
    public ValidationOutcome Check(LogHsRecord command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.Summary)) errors.Add("Summary is required.");
        if (string.IsNullOrWhiteSpace(command.AssignedToEmail)) errors.Add("Assignee email is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
