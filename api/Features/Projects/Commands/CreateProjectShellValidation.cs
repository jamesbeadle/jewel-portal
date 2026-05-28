using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Projects;

namespace Jewel.JPMS.Api.Features.Projects.Commands;

public sealed class CreateProjectShellValidation
{
    public ValidationOutcome Check(CreateProjectShell command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.Reference)) errors.Add("Reference is required.");
        if (string.IsNullOrWhiteSpace(command.Name)) errors.Add("Name is required.");
        if (string.IsNullOrWhiteSpace(command.ClientName)) errors.Add("Client name is required.");
        if (string.IsNullOrWhiteSpace(command.ProjectManagerEmail)) errors.Add("Project manager email is required.");

        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
