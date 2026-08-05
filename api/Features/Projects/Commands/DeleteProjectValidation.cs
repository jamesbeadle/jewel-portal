using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Projects;

namespace Jewel.JPMS.Api.Features.Projects.Commands;

public sealed class DeleteProjectValidation
{
    public ValidationOutcome Check(DeleteProject command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.ConfirmName)) errors.Add("Type the project name to confirm deletion.");

        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
