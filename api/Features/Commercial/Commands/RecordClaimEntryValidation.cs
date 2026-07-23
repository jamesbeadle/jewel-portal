using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class RecordClaimEntryValidation
{
    public ValidationOutcome Check(RecordClaimEntry command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ValuationClaimId)) errors.Add("ValuationClaimId is required.");
        if (string.IsNullOrWhiteSpace(command.ValuationLineItemId)) errors.Add("ValuationLineItemId is required.");
        // Variation lines may legitimately claim outside 0-100 (weighted % of a net VO);
        // the handler enforces 0-100 for physical-completion lines. +/-100000 is a typo rail.
        if (command.PercentComplete < -100000 || command.PercentComplete > 100000) errors.Add("Percent complete must be between -100000% and 100000%.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
