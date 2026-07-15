using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Retention;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Retention.Commands;

public sealed class ConfirmRetentionReleaseValidation
{
    public ValidationOutcome Check(ConfirmRetentionRelease command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (!Enum.IsDefined(typeof(RetentionMilestone), command.Milestone)) errors.Add("Unknown retention milestone.");
        if (command.Amount <= 0) errors.Add("Release amount must be greater than zero.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
