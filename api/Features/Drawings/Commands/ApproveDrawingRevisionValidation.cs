using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

public sealed class ApproveDrawingRevisionValidation
{
    public ValidationOutcome Check(ApproveDrawingRevision command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.DrawingId)) errors.Add("DrawingId is required.");
        if (string.IsNullOrWhiteSpace(command.DrawingRevisionId)) errors.Add("DrawingRevisionId is required.");
        if (string.IsNullOrWhiteSpace(command.ApprovedByEmail)) errors.Add("Approving email is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
