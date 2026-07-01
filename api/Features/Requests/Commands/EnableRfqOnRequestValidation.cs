using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class EnableRfqOnRequestValidation
{
    public ValidationOutcome Check(EnableRfqOnRequest command)
    {
        if (string.IsNullOrWhiteSpace(command.RequestId))
            return new ValidationOutcome(new[] { "RequestId is required." });
        return ValidationOutcome.Passed;
    }
}
