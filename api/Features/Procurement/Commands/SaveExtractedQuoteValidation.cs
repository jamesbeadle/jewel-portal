using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SaveExtractedQuoteValidation
{
    public ValidationOutcome Check(SaveExtractedQuote command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.BidPackageId)) errors.Add("BidPackageId is required.");
        if (string.IsNullOrWhiteSpace(command.SubcontractorId)) errors.Add("SubcontractorId is required.");
        if (command.Lines is null || command.Lines.Count == 0)
            errors.Add("At least one quote line is required.");
        else
        {
            if (command.Lines.Any(line => string.IsNullOrWhiteSpace(line.Description)))
                errors.Add("Every quote line needs a description.");
            if (command.Lines.Any(line => line.Total < 0 || line.Rate < 0 || line.Quantity < 0))
                errors.Add("Quote line quantities, rates and totals must not be negative.");
        }
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
