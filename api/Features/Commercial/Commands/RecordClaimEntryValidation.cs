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
        if (command.PercentComplete < 0 || command.PercentComplete > 100) errors.Add("Percent complete must be 0-100%.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
