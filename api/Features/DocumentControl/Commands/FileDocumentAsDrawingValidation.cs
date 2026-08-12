using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.DocumentControl;

namespace Jewel.JPMS.Api.Features.DocumentControl.Commands;

public sealed class FileDocumentAsDrawingValidation
{
    public ValidationOutcome Check(FileDocumentAsDrawing command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.DocumentControlItemId)) errors.Add("DocumentControlItemId is required.");
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.DrawingCode)) errors.Add("Drawing code is required.");
        else if (command.DrawingCode.Trim().Length > 64) errors.Add("Drawing code must be 64 characters or fewer.");
        if (command.Title is { Length: > 256 }) errors.Add("Title must be 256 characters or fewer.");
        if (command.RevisionLabel is { Length: > 16 }) errors.Add("Revision label must be 16 characters or fewer.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
