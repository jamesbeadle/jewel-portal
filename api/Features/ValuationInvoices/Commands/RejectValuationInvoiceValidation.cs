using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

public sealed class RejectValuationInvoiceValidation
{
    public ValidationOutcome Check(RejectValuationInvoice command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ValuationInvoiceId)) errors.Add("ValuationInvoiceId is required.");
        if (string.IsNullOrWhiteSpace(command.Reason)) errors.Add("A rejection reason is required — it drives the amendment.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
