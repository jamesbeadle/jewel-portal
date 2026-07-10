using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

public sealed class UpdateValuationInvoiceValidation
{
    public ValidationOutcome Check(UpdateValuationInvoice command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ValuationInvoiceId)) errors.Add("ValuationInvoiceId is required.");
        if (command.Amount <= 0) errors.Add("Amount must be greater than zero.");
        if (command.AmountPaid is < 0) errors.Add("Amount paid cannot be negative.");
        if (command.AmountPaid is not null && command.AmountPaid > command.Amount)
            errors.Add("Amount paid cannot exceed the invoice amount.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
