using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.ProjectContracts;

namespace Jewel.JPMS.Api.Features.ProjectContracts.Commands;

public sealed class RemoveProjectContractAmendmentValidation
{
    public ValidationOutcome Check(RemoveProjectContractAmendment command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("A project is required.");
        if (string.IsNullOrWhiteSpace(command.ProjectContractAmendmentId)) errors.Add("The amendment identifier is missing.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
