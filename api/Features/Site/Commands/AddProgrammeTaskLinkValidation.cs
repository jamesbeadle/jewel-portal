using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class AddProgrammeTaskLinkValidation
{
    public ValidationOutcome Check(AddProgrammeTaskLink command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.PredecessorTaskId)) errors.Add("PredecessorTaskId is required.");
        if (string.IsNullOrWhiteSpace(command.SuccessorTaskId)) errors.Add("SuccessorTaskId is required.");
        if (command.PredecessorTaskId == command.SuccessorTaskId) errors.Add("A task cannot depend on itself.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
