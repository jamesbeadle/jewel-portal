using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class SendVariationOrderEmailValidation
{
    public ValidationOutcome Check(SendVariationOrderEmail command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.VariationOrderId)) errors.Add("VariationOrderId is required.");
        // Which statuses may be emailed is the handler's, which has the record; an override that
        // is not an address is the mailbox's to refuse, with its own words.
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
