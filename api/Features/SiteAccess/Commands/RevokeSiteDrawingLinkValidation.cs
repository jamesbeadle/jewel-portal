using Jewel.JPMS.Contracts.SiteAccess;

namespace Jewel.JPMS.Api.Features.SiteAccess.Commands;

public sealed class RevokeSiteDrawingLinkValidation
{
    public ValidationOutcome Check(RevokeSiteDrawingLink command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.SiteDrawingLinkId)) errors.Add("SiteDrawingLinkId is required.");
        if (string.IsNullOrWhiteSpace(command.RevokedByEmail)) errors.Add("Revoking email is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
