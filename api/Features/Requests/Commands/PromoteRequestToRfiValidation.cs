using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class PromoteRequestToRfiValidation
{
    public ValidationOutcome Check(PromoteRequestToRfi command)
    {
        if (string.IsNullOrWhiteSpace(command.RequestId))
            return new ValidationOutcome(new[] { "RequestId is required." });
        return ValidationOutcome.Passed;
    }
}
