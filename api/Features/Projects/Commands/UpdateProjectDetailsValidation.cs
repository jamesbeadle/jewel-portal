using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Projects.Commands;

public sealed class UpdateProjectDetailsValidation
{
    public ValidationOutcome Check(UpdateProjectDetails command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.Reference)) errors.Add("Reference is required.");
        if (string.IsNullOrWhiteSpace(command.Name)) errors.Add("Name is required.");
        if (string.IsNullOrWhiteSpace(command.ClientName)) errors.Add("Client name is required.");
        if (string.IsNullOrWhiteSpace(command.ProjectManagerEmail)) errors.Add("Project manager email is required.");
        if (command.PartyKind is not (PartyKind.Client or PartyKind.Architect))
            errors.Add("PartyKind must be Client or Architect.");
        if (!string.IsNullOrWhiteSpace(command.OnBehalfOfClientId) && command.PartyKind != PartyKind.Architect)
            errors.Add("OnBehalfOfClientId only applies when the party is an architect.");

        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
