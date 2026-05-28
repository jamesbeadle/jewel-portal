using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class UploadComplianceDocumentValidation
{
    public ValidationOutcome Check(UploadComplianceDocument command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.SubcontractorId)) errors.Add("SubcontractorId is required.");
        if (string.IsNullOrWhiteSpace(command.Kind)) errors.Add("Kind is required.");
        if (string.IsNullOrWhiteSpace(command.FileName)) errors.Add("File name is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
