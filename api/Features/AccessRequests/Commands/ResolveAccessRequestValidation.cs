using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.AccessRequests;

namespace Jewel.JPMS.Api.Features.AccessRequests.Commands;

public sealed class ResolveAccessRequestValidation
{
    public ValidationOutcome Check(ResolveAccessRequest command)
    {
        if (string.IsNullOrWhiteSpace(command.Email)) return ValidationOutcome.Failed("Email is required.");
        return ValidationOutcome.Passed;
    }
}
