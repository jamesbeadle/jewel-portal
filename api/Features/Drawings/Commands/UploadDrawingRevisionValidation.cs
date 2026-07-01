using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

public sealed class UploadDrawingRevisionValidation
{
    public ValidationOutcome Check(UploadDrawingRevision command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.DrawingId)) errors.Add("DrawingId is required.");
        if (string.IsNullOrWhiteSpace(command.DrawingRevisionId)) errors.Add("DrawingRevisionId is required.");
        if (string.IsNullOrWhiteSpace(command.RevisionLabel)) errors.Add("Revision label is required.");
        if (string.IsNullOrWhiteSpace(command.FileName)) errors.Add("File name is required.");
        if (string.IsNullOrWhiteSpace(command.IssuedByEmail)) errors.Add("Issuing email is required.");
        if (string.IsNullOrWhiteSpace(command.BlobRef)) errors.Add("Uploaded file reference is required.");
        if (command.FileSizeBytes <= 0) errors.Add("Uploaded file is empty.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
