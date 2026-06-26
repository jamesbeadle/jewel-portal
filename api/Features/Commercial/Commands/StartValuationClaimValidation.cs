using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class StartValuationClaimValidation
{
    public ValidationOutcome Check(StartValuationClaim command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (command.ClaimNumber <= 0) errors.Add("Claim number must be a positive integer.");
        if (command.RetentionPercent < 0 || command.RetentionPercent > 100) errors.Add("Retention must be 0-100%.");
        if (command.RetentionReleasePercent < 0 || command.RetentionReleasePercent > 100) errors.Add("Retention release must be 0-100%.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
