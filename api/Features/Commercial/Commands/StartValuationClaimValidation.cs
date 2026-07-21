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
        // Null percents mean "stamp from the project's retention terms" — only explicit
        // overrides need their range checked.
        if (command.RetentionPercent is { } retention && (retention < 0 || retention > 100))
            errors.Add("Retention must be 0-100%.");
        if (command.RetentionReleasePercent is { } release && (release < 0 || release > 100))
            errors.Add("Retention release must be 0-100%.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
