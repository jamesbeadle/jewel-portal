using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.ProjectContracts;

namespace Jewel.JPMS.Api.Features.ProjectContracts.Commands;

/// <summary>
/// Messages are written as full sentences — they reach the user verbatim through the command
/// sender's error unwrapping.
/// </summary>
public sealed class SetProjectContractAmendmentDetailsValidation
{
    public ValidationOutcome Check(SetProjectContractAmendmentDetails command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("A project is required.");
        if (string.IsNullOrWhiteSpace(command.ProjectContractAmendmentId)) errors.Add("The amendment identifier is missing.");
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("Give the amendment a title — it is how the list reads.");
        if (command.Title is { Length: > 256 }) errors.Add("The title is too long (256 characters max).");
        if (command.Notes is { Length: > 4000 }) errors.Add("The notes are too long (4,000 characters max).");

        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
