using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

// A blank label is allowed — it clears the revision back to "no revision".
public sealed class SetDrawingRevisionLabelValidation
{
    public ValidationOutcome Check(SetDrawingRevisionLabel command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.DrawingId)) errors.Add("DrawingId is required.");
        if (string.IsNullOrWhiteSpace(command.DrawingRevisionId)) errors.Add("DrawingRevisionId is required.");
        if (command.RevisionLabel?.Trim().Length > DrawingFieldLimits.RevisionLabelMaxLength)
            errors.Add($"Revision label must be {DrawingFieldLimits.RevisionLabelMaxLength} characters or fewer.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
