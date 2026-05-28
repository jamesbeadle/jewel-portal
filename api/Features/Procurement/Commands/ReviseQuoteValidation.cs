using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class ReviseQuoteValidation
{
    public ValidationOutcome Check(ReviseQuote command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.QuoteId)) errors.Add("QuoteId is required.");
        if (command.Value <= 0) errors.Add("Revised value must be positive.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
