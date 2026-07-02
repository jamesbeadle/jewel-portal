using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

public sealed class IssueValuationInvoiceValidation
{
    public ValidationOutcome Check(IssueValuationInvoice command)
    {
        if (string.IsNullOrWhiteSpace(command.ValuationInvoiceId))
            return new ValidationOutcome(new[] { "ValuationInvoiceId is required." });
        return ValidationOutcome.Passed;
    }
}
