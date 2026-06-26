using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class LinkIntakeToRequestValidation
{
    public ValidationOutcome Check(LinkIntakeToRequest command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.IntakeId)) errors.Add("IntakeId is required.");
        if (string.IsNullOrWhiteSpace(command.RequestId)) errors.Add("RequestId is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
