using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Cvr;

namespace Jewel.JPMS.Api.Features.Cvr.Commands;

public sealed class UpdateEotValidation
{
    public ValidationOutcome Check(UpdateEot command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.EotId)) errors.Add("EotId is required.");
        if (string.IsNullOrWhiteSpace(command.Reason)) errors.Add("Reason is required.");
        if (command.DaysGranted <= 0) errors.Add("Days granted must be positive.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
