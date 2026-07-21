using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class RecordClaimEntriesValidation
{
    // Generous headroom over the largest real bill (Abbot Road runs ~250 lines) while
    // still bounding a runaway request.
    private const int MaxEntries = 2000;

    public ValidationOutcome Check(RecordClaimEntries command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ValuationClaimId)) errors.Add("ValuationClaimId is required.");
        if (command.Entries is null || command.Entries.Count == 0) errors.Add("At least one entry is required.");
        else
        {
            if (command.Entries.Count > MaxEntries) errors.Add($"At most {MaxEntries} entries per request.");
            if (command.Entries.Any(entry => string.IsNullOrWhiteSpace(entry.ValuationLineItemId)))
                errors.Add("Every entry needs a ValuationLineItemId.");
            if (command.Entries.Any(entry => entry.PercentComplete < 0 || entry.PercentComplete > 100))
                errors.Add("Percent complete must be 0-100% on every entry.");
            if (command.Entries.GroupBy(entry => entry.ValuationLineItemId).Any(group => group.Count() > 1))
                errors.Add("Each line may appear only once per request.");
        }
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
