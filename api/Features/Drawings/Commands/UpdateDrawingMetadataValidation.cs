using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

public sealed class UpdateDrawingMetadataValidation
{
    public ValidationOutcome Check(UpdateDrawingMetadata command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.DrawingId)) errors.Add("DrawingId is required.");
        if (string.IsNullOrWhiteSpace(command.DrawingCode)) errors.Add("Drawing code is required.");
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("Title is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
