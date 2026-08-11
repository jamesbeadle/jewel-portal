using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.ProjectContracts;

namespace Jewel.JPMS.Api.Features.ProjectContracts.Commands;

public sealed class AttachProjectContractAmendmentValidation
{
    public ValidationOutcome Check(AttachProjectContractAmendment command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("A project is required.");
        if (string.IsNullOrWhiteSpace(command.ProjectContractAmendmentId)) errors.Add("The amendment identifier is missing.");
        if (string.IsNullOrWhiteSpace(command.BlobRef)) errors.Add("The stored file reference is missing.");
        if (string.IsNullOrWhiteSpace(command.FileName)) errors.Add("A file name is required.");
        if (command.FileSizeBytes <= 0) errors.Add("The uploaded file is empty.");
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("Give the amendment a title — it is how the list reads.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
