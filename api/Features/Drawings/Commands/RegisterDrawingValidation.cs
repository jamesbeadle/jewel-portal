using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

// Code and title are optional (a drawing can be named later); only their widths are checked.
public sealed class RegisterDrawingValidation
{
    public ValidationOutcome Check(RegisterDrawing command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (command.DrawingCode?.Trim().Length > DrawingFieldLimits.DrawingCodeMaxLength)
            errors.Add($"Drawing code must be {DrawingFieldLimits.DrawingCodeMaxLength} characters or fewer.");
        if (command.Title?.Trim().Length > DrawingFieldLimits.TitleMaxLength)
            errors.Add($"Title must be {DrawingFieldLimits.TitleMaxLength} characters or fewer.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
