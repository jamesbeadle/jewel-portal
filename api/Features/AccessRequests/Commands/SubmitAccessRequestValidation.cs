using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.AccessRequests;

namespace Jewel.JPMS.Api.Features.AccessRequests.Commands;

public sealed class SubmitAccessRequestValidation
{
    public ValidationOutcome Check(SubmitAccessRequest command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.Email)) errors.Add("Email is required.");
        if (string.IsNullOrWhiteSpace(command.DisplayName)) errors.Add("Display name is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
