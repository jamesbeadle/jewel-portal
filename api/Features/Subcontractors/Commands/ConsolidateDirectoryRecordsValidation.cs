using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class ConsolidateDirectoryRecordsValidation
{
    public ValidationOutcome Check(ConsolidateDirectoryRecords command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.MasterSubcontractorId))
            errors.Add("A master record is required.");
        var mergedIds = (command.MergedSubcontractorIds ?? Array.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToList();
        if (mergedIds.Count == 0)
            errors.Add("At least one record to consolidate into the master is required.");
        if (mergedIds.Any(id => string.Equals(id, command.MasterSubcontractorId, StringComparison.OrdinalIgnoreCase)))
            errors.Add("The master record can't also be one of the records being merged away.");
        if (string.IsNullOrWhiteSpace(command.CompanyName))
            errors.Add("Company name is required.");
        if (command.PaymentTermsDays is < 0 or > 365)
            errors.Add("Payment terms must be between 0 and 365 days.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
