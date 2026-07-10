using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

public sealed class CreateValuationInvoiceValidation
{
    public ValidationOutcome Check(CreateValuationInvoice command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (command.Amount <= 0) errors.Add("Amount requested must be greater than zero.");
        if (command.AmountPaid is < 0) errors.Add("Amount paid cannot be negative.");
        if (!command.IsManual && (command.AmountPaid is not null || command.IssuedAt is not null || command.PaidAt is not null))
            errors.Add("Paid amounts and backdated dates are only valid on manual (historic) invoices.");
        if (command.IsManual && command.AmountPaid is not null && command.AmountPaid > command.Amount)
            errors.Add("Amount paid cannot exceed the invoice amount.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
