using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

public sealed class SetLoginProjectsValidation
{
    public ValidationOutcome Check(SetLoginProjects command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.Email)) errors.Add("Email is required.");
        if (command.ProjectIds.Any(string.IsNullOrWhiteSpace)) errors.Add("A project id is blank.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
