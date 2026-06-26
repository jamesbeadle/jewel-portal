using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class DeleteRequestValidation
{
    public ValidationOutcome Check(DeleteRequest command)
    {
        if (string.IsNullOrWhiteSpace(command.RequestId)) return ValidationOutcome.Failed("RequestId is required.");
        return ValidationOutcome.Passed;
    }
}
