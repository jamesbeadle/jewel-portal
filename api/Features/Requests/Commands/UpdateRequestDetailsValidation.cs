using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class UpdateRequestDetailsValidation
{
    public ValidationOutcome Check(UpdateRequestDetails command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.RequestId)) errors.Add("RequestId is required.");
        if (string.IsNullOrWhiteSpace(command.Reference)) errors.Add("Reference is required.");
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("Title is required.");
        // Backdating a close is allowed; forward-dating is not.
        if (command.ClosedAt is { } closedAt && closedAt.UtcDateTime.Date > DateTimeOffset.UtcNow.Date)
            errors.Add("The closed date cannot be in the future.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
