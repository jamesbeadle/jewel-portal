using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class RecordClaimEntryValidation
{
    public ValidationOutcome Check(RecordClaimEntry command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ValuationClaimId)) errors.Add("ValuationClaimId is required.");
        if (string.IsNullOrWhiteSpace(command.ValuationLineItemId)) errors.Add("ValuationLineItemId is required.");
        // Any number, any decimals, positive or negative — a % is whatever reproduces the
        // claimed value on the line. +/-100000 is only a typo rail.
        if (command.PercentComplete < -100000 || command.PercentComplete > 100000) errors.Add("Percent complete must be between -100000% and 100000%.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
