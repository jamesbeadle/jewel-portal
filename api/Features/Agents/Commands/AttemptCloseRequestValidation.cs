using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Agents;

namespace Jewel.JPMS.Api.Features.Agents.Commands;

public sealed class AttemptCloseRequestValidation
{
    public ValidationOutcome Check(AttemptCloseRequest command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.RequestId)) errors.Add("RequestId is required.");
        // Backdating is the point of a user-supplied close date; forward-dating is not allowed.
        if (command.ClosedAt is { } closedAt && closedAt.UtcDateTime.Date > DateTimeOffset.UtcNow.Date)
            errors.Add("The closed date cannot be in the future.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
