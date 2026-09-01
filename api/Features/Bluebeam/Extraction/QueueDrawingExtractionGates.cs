using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

public sealed class QueueDrawingExtractionAuthorisation
{
    public bool Allows(SignedInUser user, QueueDrawingExtraction command) =>
        DrawingExtractionRoles.AllowedToExtract.IncludesAny(user.Roles);
}

public sealed class QueueDrawingExtractionValidation
{
    public ValidationOutcome Check(QueueDrawingExtraction command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.DrawingId)) errors.Add("DrawingId is required.");
        if (string.IsNullOrWhiteSpace(command.DrawingRevisionId)) errors.Add("DrawingRevisionId is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}

public sealed class QueueProjectDrawingExtractionsAuthorisation
{
    public bool Allows(SignedInUser user, QueueProjectDrawingExtractions command) =>
        DrawingExtractionRoles.AllowedToExtract.IncludesAny(user.Roles);
}

public sealed class QueueProjectDrawingExtractionsValidation
{
    public ValidationOutcome Check(QueueProjectDrawingExtractions command)
    {
        if (string.IsNullOrWhiteSpace(command.ProjectId))
            return new ValidationOutcome(new List<string> { "ProjectId is required." });
        return ValidationOutcome.Passed;
    }
}
