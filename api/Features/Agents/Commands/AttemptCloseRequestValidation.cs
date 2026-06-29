using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Agents;

namespace Jewel.JPMS.Api.Features.Agents.Commands;

public sealed class AttemptCloseRequestValidation
{
    public ValidationOutcome Check(AttemptCloseRequest command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.RequestId)) errors.Add("RequestId is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
