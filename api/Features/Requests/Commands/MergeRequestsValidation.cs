using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class MergeRequestsValidation
{
    public ValidationOutcome Check(MergeRequests command)
    {
        if (string.IsNullOrWhiteSpace(command.SurvivorRequestId)) return ValidationOutcome.Failed("SurvivorRequestId is required.");
        if (string.IsNullOrWhiteSpace(command.MergedRequestId)) return ValidationOutcome.Failed("MergedRequestId is required.");
        if (command.SurvivorRequestId == command.MergedRequestId) return ValidationOutcome.Failed("A request cannot be merged into itself.");
        return ValidationOutcome.Passed;
    }
}
