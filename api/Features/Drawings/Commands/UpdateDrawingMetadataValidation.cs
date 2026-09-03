using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

// Code and title may be cleared as well as set — both are optional on a drawing.
public sealed class UpdateDrawingMetadataValidation
{
    public ValidationOutcome Check(UpdateDrawingMetadata command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.DrawingId)) errors.Add("DrawingId is required.");
        if (command.DrawingCode?.Trim().Length > DrawingFieldLimits.DrawingCodeMaxLength)
            errors.Add($"Document code must be {DrawingFieldLimits.DrawingCodeMaxLength} characters or fewer.");
        if (command.Title?.Trim().Length > DrawingFieldLimits.TitleMaxLength)
            errors.Add($"Title must be {DrawingFieldLimits.TitleMaxLength} characters or fewer.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
