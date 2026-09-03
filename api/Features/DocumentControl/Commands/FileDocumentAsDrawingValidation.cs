using Jewel.JPMS.Api.Features.Drawings;
using Jewel.JPMS.Contracts.DocumentControl;

namespace Jewel.JPMS.Api.Features.DocumentControl.Commands;

public sealed class FileDocumentAsDrawingValidation
{
    public ValidationOutcome Check(FileDocumentAsDrawing command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.DocumentControlItemId)) errors.Add("DocumentControlItemId is required.");
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        // Code, title and revision are all optional — a blank code registers a new drawing named
        // by its file. Only the column widths are checked.
        if (command.DrawingCode?.Trim().Length > DrawingFieldLimits.DrawingCodeMaxLength)
            errors.Add($"Document code must be {DrawingFieldLimits.DrawingCodeMaxLength} characters or fewer.");
        if (command.Title?.Trim().Length > DrawingFieldLimits.TitleMaxLength)
            errors.Add($"Title must be {DrawingFieldLimits.TitleMaxLength} characters or fewer.");
        if (command.RevisionLabel?.Trim().Length > DrawingFieldLimits.RevisionLabelMaxLength)
            errors.Add($"Revision label must be {DrawingFieldLimits.RevisionLabelMaxLength} characters or fewer.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
