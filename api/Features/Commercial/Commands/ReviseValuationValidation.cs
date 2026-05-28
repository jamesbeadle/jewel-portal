using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class ReviseValuationValidation
{
    public ValidationOutcome Check(ReviseValuation command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ValuationId)) errors.Add("ValuationId is required.");
        if (command.GrossValue < 0) errors.Add("Gross value cannot be negative.");
        if (command.RetentionPercent < 0 || command.RetentionPercent > 100) errors.Add("Retention must be 0-100%.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
