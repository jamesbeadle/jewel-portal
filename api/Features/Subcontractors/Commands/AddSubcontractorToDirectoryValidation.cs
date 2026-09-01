using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class AddSubcontractorToDirectoryValidation
{
    public ValidationOutcome Check(AddSubcontractorToDirectory command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.CompanyName)) errors.Add("Company name is required.");
        // Trade only matters for the companies we buy work from; clients/architects don't need one.
        var needsTrade = command.Category is DirectoryCategory.Subcontractor or DirectoryCategory.Supplier;
        if (needsTrade && (command.TradeIds is null || command.TradeIds.Count == 0)) errors.Add("At least one trade is required for subcontractors and suppliers.");
        if (command.PaymentTermsDays is < 0 or > 365) errors.Add("Payment terms must be between 0 and 365 days.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
