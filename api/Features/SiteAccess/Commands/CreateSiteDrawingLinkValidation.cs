using Jewel.JPMS.Contracts.SiteAccess;
using static Jewel.JPMS.Models.SiteDrawingLinkLimits;

namespace Jewel.JPMS.Api.Features.SiteAccess.Commands;

public sealed class CreateSiteDrawingLinkValidation
{
    public ValidationOutcome Check(CreateSiteDrawingLink command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.DrawingFolderId)) errors.Add("The folder the poster opens is required.");
        if (string.IsNullOrWhiteSpace(command.Label)) errors.Add("A label for the poster is required.");
        var label = command.Label ?? "";
        var isLabelTooLong = label.Length > LabelLength;
        if (isLabelTooLong) errors.Add($"The label must be {LabelLength} characters or fewer.");
        var isExpiryOutOfRange = command.ExpiresInDays < ShortestExpiryDays || command.ExpiresInDays > LongestExpiryDays;
        if (isExpiryOutOfRange) errors.Add($"The link must expire between {ShortestExpiryDays} and {LongestExpiryDays} days from now.");
        if (string.IsNullOrWhiteSpace(command.CreatedByEmail)) errors.Add("Creating email is required.");
        if (string.IsNullOrWhiteSpace(command.SiteOrigin)) errors.Add("The public site the poster points at is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
