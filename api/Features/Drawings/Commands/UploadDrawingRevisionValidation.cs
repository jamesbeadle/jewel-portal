using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

// The revision label and the issuer are optional; the file and its stored reference are not.
public sealed class UploadDrawingRevisionValidation
{
    public ValidationOutcome Check(UploadDrawingRevision command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.DrawingId)) errors.Add("DrawingId is required.");
        if (string.IsNullOrWhiteSpace(command.DrawingRevisionId)) errors.Add("DrawingRevisionId is required.");
        if (command.RevisionLabel?.Trim().Length > DrawingFieldLimits.RevisionLabelMaxLength)
            errors.Add($"Revision label must be {DrawingFieldLimits.RevisionLabelMaxLength} characters or fewer.");
        if (string.IsNullOrWhiteSpace(command.FileName)) errors.Add("File name is required.");
        if (command.IssuedByEmail?.Trim().Length > DrawingFieldLimits.EmailMaxLength)
            errors.Add($"Issuing email must be {DrawingFieldLimits.EmailMaxLength} characters or fewer.");
        if (string.IsNullOrWhiteSpace(command.BlobRef)) errors.Add("Uploaded file reference is required.");
        if (command.FileSizeBytes <= 0) errors.Add("Uploaded file is empty.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
