using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

public sealed class RecordValuationInvoicePaymentValidation
{
    public ValidationOutcome Check(RecordValuationInvoicePayment command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ValuationInvoiceId)) errors.Add("ValuationInvoiceId is required.");
        if (command.AmountPaid <= 0) errors.Add("Amount received must be greater than zero.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
