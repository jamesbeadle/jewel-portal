using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

public sealed class UpdateValuationInvoiceValidation
{
    public ValidationOutcome Check(UpdateValuationInvoice command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ValuationInvoiceId)) errors.Add("ValuationInvoiceId is required.");
        // Zero is a legal amendment — it voids a manual entry's value without deleting the row.
        // The handler still insists on > 0 for workflow (non-manual) invoices, which it can tell
        // apart; validation here only rules out the nonsensical.
        if (command.Amount < 0) errors.Add("Amount cannot be negative.");
        if (command.AmountPaid is < 0) errors.Add("Amount paid cannot be negative.");
        if (command.AmountPaid is not null && command.AmountPaid > command.Amount)
            errors.Add("Amount paid cannot exceed the invoice amount.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
