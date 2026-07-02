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
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
